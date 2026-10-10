using System;
using RoR2;
using UnityEngine;
using UnityEngine.Rendering;

namespace LaserCutterImprovements
{
    /// <summary>
    /// A part of the Laser Cutter's range sphere ahead of the player, drawn with the game's Cloud Remap shader as plain
    /// colours: a faint, evenly translucent orange band around the aim direction from a little above it down to
    /// straight below, and a line on the ground where the sphere meets it. Added to the player's body the first time it is shown; <see cref="LaserHooks"/> shows it every
    /// frame the beam reaches its maximum range, and it fades out in the frames it isn't shown.
    /// </summary>
    internal sealed class RangeIndicator : MonoBehaviour
    {
        private const float HalfWidth = 25f; // degrees to each side of the aim direction
        private const float AboveAim = 15f; // degrees above the aim direction, over which the top fades out
        private const int Columns = 24;
        private const int Rows = 24;
        private const float FadeTime = 0.15f;
        private const float BandAlpha = 0.12f; // at the band's centre
        private const float LineAlpha = 0.9f; // along the line, fading out at both ends
        private const float LineWidth = 0.4f;
        private const float LineStep = 2.5f; // degrees between the points of the line
        private const int LineSide = (int)(HalfWidth / LineStep); // points to each side of the aim direction
        private const float WalkStep = 10f; // degrees between the points tested for ground, going down a vertical circle
        private const float WalkBack = 20f; // degrees above where the last circle found ground that the next one starts
        private const float LowestElevation = -89f;
        private const float SoftFade = 20f; // the shader's soft-particle fade: only the last ~5 cm in front of geometry fades
        private const string TintColor = "_TintColor";
        private const float GroundOffset = 0.3f; // metres above the ground, so the line clears its depth

        private static readonly Color IndicatorColor = new Color(1f, 0.6f, 0.2f);
        private static Material template;

        private GameObject sphere;
        private GameObject lineObject;
        private LineRenderer line;
        private Material material;
        private Mesh mesh;
        private readonly Vector3[] vertices = new Vector3[(Rows + 1) * (Columns + 1)];
        private readonly Color[] colors = new Color[(Rows + 1) * (Columns + 1)];
        private readonly Vector3[] linePositions = new Vector3[2 * LineSide + 1];
        private bool hasLine;
        private float alpha;
        private int shownFrame = -1;

        public static void Show(CharacterBody body, Vector3 origin, Vector3 direction, float range)
        {
            if (!body.TryGetComponent(out RangeIndicator indicator))
            {
                indicator = body.gameObject.AddComponent<RangeIndicator>();
            }
            indicator.Place(origin, direction, range);
        }

        private void Awake()
        {
            if (!template)
            {
                // The game's particle shader, alpha-blended and drawn on both sides: white texture, no colour ramp and
                // no clouds, so the colour is the tint times the vertex colours.
                template = new Material(LegacyShaderAPI.Find("Hopoo Games/FX/Cloud Remap"));
                template.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                template.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                template.SetTexture("_MainTex", Texture2D.whiteTexture);
                template.SetFloat("_InvFade", SoftFade);
                SetToggle(template, "_DisableRemapOn", "DISABLEREMAP", true);
                SetToggle(template, "_CloudsOn", "USE_CLOUDS", false);
                SetToggle(template, "_VertexColorOn", "VERTEXCOLOR", true);
                SetToggle(template, "_VertexAlphaOn", "VERTEXALPHA", true);
            }
            // A copy, so fading it doesn't change the other indicators.
            material = new Material(template);

            mesh = new Mesh { name = "LaserCutterRangeIndicator" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            int[] triangles = new int[Rows * Columns * 6];
            int t = 0;
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    int i = row * (Columns + 1) + column;
                    // The material draws both sides, so the winding doesn't matter.
                    triangles[t++] = i;
                    triangles[t++] = i + Columns + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + Columns + 1;
                    triangles[t++] = i + Columns + 2;
                }
            }
            mesh.triangles = triangles;

            sphere = new GameObject("LaserCutterRangeIndicator");
            sphere.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer meshRenderer = sphere.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            sphere.SetActive(false);

            lineObject = new GameObject("LaserCutterRangeLine");
            line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.widthMultiplier = LineWidth;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 2;
            // White: the material's colour gives the line its colour, as for the band.
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(LineAlpha, 0.2f),
                    new GradientAlphaKey(LineAlpha, 0.8f), new GradientAlphaKey(0f, 1f)
                });
            line.colorGradient = gradient;
            lineObject.SetActive(false);
        }

        // The shader's toggles are a float property and a keyword each.
        private static void SetToggle(Material material, string property, string keyword, bool on)
        {
            material.SetFloat(property, on ? 1f : 0f);
            if (on)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        // The band on a unit sphere around the horizontal aim direction (+Z), scaled to the range.
        private void Place(Vector3 origin, Vector3 direction, float range)
        {
            shownFrame = Time.frameCount;
            Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (horizontal.sqrMagnitude < 1e-6f)
            {
                return; // aiming straight up or down: keep the last shape
            }
            float aimElevation = 90f - Vector3.Angle(Vector3.up, direction);
            float top = Mathf.Min(aimElevation + AboveAim, 90f);
            for (int row = 0; row <= Rows; row++)
            {
                float elevation = Mathf.Lerp(-90f, top, (float)row / Rows);
                float topFade = Mathf.SmoothStep(0f, 1f, (top - elevation) / AboveAim);
                for (int column = 0; column <= Columns; column++)
                {
                    float side = Mathf.Lerp(-1f, 1f, (float)column / Columns);
                    Vector3 point = Quaternion.Euler(-elevation, side * HalfWidth, 0f) * Vector3.forward;
                    int i = row * (Columns + 1) + column;
                    vertices[i] = point;
                    colors[i] = new Color(1f, 1f, 1f, BandAlpha * topFade * (1f - side * side));
                }
            }
            mesh.vertices = vertices;
            mesh.normals = vertices; // on a unit sphere, the outward normals are the positions
            mesh.colors = colors;
            mesh.RecalculateBounds();
            Quaternion aim = Quaternion.LookRotation(horizontal);
            sphere.transform.SetPositionAndRotation(origin, aim);
            sphere.transform.localScale = new Vector3(range, range, range);
            PlaceLine(origin, aim, range, top);
        }

        // The line where the range sphere meets the ground: the points found at the yaws around the aim direction,
        // as far out on each side as every yaw has ground.
        private void PlaceLine(Vector3 origin, Quaternion aim, float range, float top)
        {
            hasLine = FindGround(origin, aim, range, 0f, top, out linePositions[LineSide], out float centreElevation);
            if (!hasLine)
            {
                return;
            }
            int first = LineSide;
            int last = LineSide;
            // Each yaw starts a little above where the one before found ground, which is about as high.
            float elevation = centreElevation;
            while (last < linePositions.Length - 1 && FindGround(origin, aim, range, (last + 1 - LineSide) * LineStep,
                Mathf.Min(elevation + WalkBack, top), out linePositions[last + 1], out elevation))
            {
                last++;
            }
            elevation = centreElevation;
            while (first > 0 && FindGround(origin, aim, range, (first - 1 - LineSide) * LineStep,
                Mathf.Min(elevation + WalkBack, top), out linePositions[first - 1], out elevation))
            {
                first--;
            }
            int count = last - first + 1;
            hasLine = count >= 2;
            if (hasLine)
            {
                Array.Copy(linePositions, first, linePositions, 0, count);
                line.positionCount = count;
                line.SetPositions(linePositions);
            }
        }

        // Walks down the sphere's vertical circle at this yaw from startElevation and returns the ground hit by the
        // first segment between two points on the sphere that meets it, and the elevation of the segment's end.
        private static bool FindGround(Vector3 origin, Quaternion aim, float range, float yaw, float startElevation,
            out Vector3 ground, out float elevation)
        {
            Vector3 previous = SpherePoint(origin, aim, range, yaw, startElevation);
            elevation = startElevation;
            while (elevation > LowestElevation)
            {
                elevation = Mathf.Max(elevation - WalkStep, LowestElevation);
                Vector3 next = SpherePoint(origin, aim, range, yaw, elevation);
                if (Physics.Linecast(previous, next, out RaycastHit hit, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                {
                    ground = hit.point + hit.normal * GroundOffset;
                    return true;
                }
                previous = next;
            }
            ground = default;
            return false;
        }

        private static Vector3 SpherePoint(Vector3 origin, Quaternion aim, float range, float yaw, float elevation)
        {
            return origin + aim * (Quaternion.Euler(-elevation, yaw, 0f) * Vector3.forward) * range;
        }

        private void LateUpdate()
        {
            alpha = Mathf.MoveTowards(alpha, shownFrame == Time.frameCount ? 1f : 0f, Time.deltaTime / FadeTime);
            sphere.SetActive(alpha > 0f);
            lineObject.SetActive(alpha > 0f && hasLine);
            material.SetColor(TintColor, new Color(IndicatorColor.r, IndicatorColor.g, IndicatorColor.b, alpha));
        }

        private void OnDestroy()
        {
            Destroy(sphere);
            Destroy(lineObject);
            Destroy(material);
            Destroy(mesh);
        }
    }
}
