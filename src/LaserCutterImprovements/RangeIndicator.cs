using RoR2;
using RoR2BepInExPack.GameAssetPathsBetter;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

namespace LaserCutterImprovements
{
    /// <summary>
    /// A part of the Laser Cutter's range sphere ahead of the player, drawn with Focus Crystal's range indicator
    /// material: a band around the aim direction from a little above it down to straight below. The material
    /// highlights where it meets the ground, which draws the line where the range ends. Added to the player's body
    /// the first time it is shown; <see cref="LaserHooks"/> shows it every frame the beam reaches its maximum range,
    /// and it fades out in the frames it isn't shown.
    /// </summary>
    internal sealed class RangeIndicator : MonoBehaviour
    {
        private const float HalfWidth = 25f; // degrees to each side of the aim direction
        private const float AboveAim = 15f; // degrees above the aim direction, over which the top fades out
        private const int Columns = 24;
        private const int Rows = 24;
        private const float FadeTime = 0.15f;
        private const string TintColor = "_TintColor";

        private static Material template;

        private GameObject sphere;
        private Material material;
        private Mesh mesh;
        private readonly Vector3[] vertices = new Vector3[(Rows + 1) * (Columns + 1)];
        private readonly Color[] colors = new Color[(Rows + 1) * (Columns + 1)];
        private Color tint;
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
                template = Addressables.LoadAssetAsync<Material>(
                    RoR2_Base_NearbyDamageBonus.matNearbyDamageBonusRangeIndicator_mat).WaitForCompletion();
            }
            // A copy, so fading it doesn't change Focus Crystal's indicator.
            material = new Material(template);
            tint = material.GetColor(TintColor);

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
                    colors[i] = new Color(1f, 1f, 1f, topFade * (1f - side * side));
                }
            }
            mesh.vertices = vertices;
            mesh.normals = vertices; // on a unit sphere, the outward normals are the positions
            mesh.colors = colors;
            mesh.RecalculateBounds();
            sphere.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(horizontal));
            sphere.transform.localScale = new Vector3(range, range, range);
        }

        private void LateUpdate()
        {
            alpha = Mathf.MoveTowards(alpha, shownFrame == Time.frameCount ? 1f : 0f, Time.deltaTime / FadeTime);
            sphere.SetActive(alpha > 0f);
            material.SetColor(TintColor, new Color(tint.r, tint.g, tint.b, tint.a * alpha));
        }

        private void OnDestroy()
        {
            Destroy(sphere);
            Destroy(material);
            Destroy(mesh);
        }
    }
}
