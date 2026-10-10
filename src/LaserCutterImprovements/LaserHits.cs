using EntityStates.Engi.EngiWeapon;
using RoR2;
using UnityEngine;

namespace LaserCutterImprovements
{
    /// <summary>
    /// What the player's Laser Cutter hits. The beam is a <see cref="BulletAttack"/> (a ray plus a sphere cast of
    /// <see cref="EngiLaserCutter.laserRadius"/>) that the owner's game fires several times a second and that stops at
    /// the first character it hits; the server heals a mechanical ally hit by it
    /// (<see cref="EngiLaserCutter.DamageMacroEngiHealMechanical"/>). <see cref="Configure"/> gives it this mod's
    /// filter (<c>TurretHitboxScale</c>) and hit callback (<c>HealAndDamage</c>); <see cref="Probe"/> fires a copy with
    /// the same rules that only records where it stops, for the visible beam and the range indicator.
    /// </summary>
    internal static class LaserHits
    {
        private static readonly BulletAttack.FilterCallback filterCallback = Filter;
        private static readonly BulletAttack.HitCallback hitCallback = Hit;
        private static readonly BulletAttack.HitCallback probeHitCallback = ProbeHit;

        // Reused for every probe (the game only fires bullets on the main thread).
        private static readonly BulletAttack probe = new BulletAttack();
        private static Vector3? probeStop;

        // Called on the owner just before EngiLaserCutter.FireLaser fires the beam.
        public static void Configure(BulletAttack attack)
        {
            attack.filterCallback = filterCallback;
            attack.hitCallback = hitCallback;
        }

        /// <summary>
        /// Where the laser's beam along <paramref name="aimRay"/> stops, by the same rules as the real one (set up as in
        /// <see cref="EngiLaserCutter.FireLaser"/>): true and the point where it stops, or false and its end at the
        /// maximum range. Deals no damage and plays no effects.
        /// </summary>
        public static bool Probe(EngiLaserCutter laser, Ray aimRay, out Vector3 end)
        {
            probe.Reset();
            probe.owner = laser.gameObject;
            probe.weapon = laser.gameObject;
            probe.origin = aimRay.origin;
            probe.aimVector = aimRay.direction;
            probe.maxDistance = laser.laserDistance;
            probe.radius = laser.laserRadius;
            probe.smartCollision = true;
            probe.filterCallback = filterCallback;
            probe.hitCallback = probeHitCallback;
            probeStop = null;
            probe.Fire();
            // Fire applies the game's aim assist to aimVector, as for the real beam.
            end = probeStop ?? probe.origin + probe.aimVector * probe.maxDistance;
            return probeStop.HasValue;
        }

        private static bool Filter(BulletAttack attack, ref BulletAttack.BulletHit hit)
        {
            if (!BulletAttack.defaultFilterCallback(attack, ref hit))
            {
                return false;
            }
            float scale = PluginConfig.TurretHitboxScale.Value;
            return scale >= 1f || !IsAlliedTurret(attack, hit.hitHurtBox) || InScaledHitbox(attack, hit, scale);
        }

        // Returns whether the beam goes on past this hit.
        private static bool Hit(BulletAttack attack, ref BulletAttack.BulletHit hit)
        {
            return BulletAttack.defaultHitCallback(attack, ref hit)
                || PluginConfig.HealAndDamage.Value && IsHealedAlly(attack, hit.hitHurtBox);
        }

        private static bool ProbeHit(BulletAttack attack, ref BulletAttack.BulletHit hit)
        {
            // The game's rule (BulletAttack.DefaultHitCallbackImplementation): colliders outside stopperMask let the
            // beam through.
            bool passes = ((1 << hit.collider.gameObject.layer) & attack.stopperMask) == 0
                || PluginConfig.HealAndDamage.Value && IsHealedAlly(attack, hit.hitHurtBox);
            if (!passes)
            {
                // A character the sphere cast already touches at the start is hit at the ray's origin; end the beam
                // at the character instead.
                probeStop = hit.distance == 0f && hit.hitHurtBox ? hit.hitHurtBox.collider.bounds.center : hit.point;
            }
            return passes;
        }

        // The same test as EngiLaserCutter.DamageMacroEngiHealMechanical.
        private static bool IsHealedAlly(BulletAttack attack, HurtBox hurtBox)
        {
            if (!hurtBox || !hurtBox.healthComponent)
            {
                return false;
            }
            CharacterBody body = hurtBox.healthComponent.body;
            return (body.bodyFlags & CharacterBody.BodyFlags.Mechanical) != 0
                && body.teamComponent.teamIndex == TeamComponent.GetObjectTeam(attack.owner);
        }

        private static bool IsAlliedTurret(BulletAttack attack, HurtBox hurtBox)
        {
            if (!hurtBox || !hurtBox.healthComponent)
            {
                return false;
            }
            CharacterBody body = hurtBox.healthComponent.body;
            return (body.bodyIndex == RoR2Content.BodyPrefabs.EngiTurretBody.bodyIndex
                    || body.bodyIndex == RoR2Content.BodyPrefabs.EngiWalkerTurretBody.bodyIndex)
                && body.teamComponent.teamIndex == TeamComponent.GetObjectTeam(attack.owner);
        }

        /// <summary>
        /// Whether the beam hits the character's hitbox scaled by <paramref name="scale"/>. The game's beam hits a
        /// hurtbox when its ray passes within <see cref="BulletAttack.radius"/> of it; scaling that region around the
        /// hurtbox's centre is the same as scaling the ray the other way around that centre, so the scaled hitbox is
        /// hit when the inversely scaled ray passes within the radius of the hurtbox. Every hurtbox of the character
        /// counts: the game only processes the first hit on each character.
        /// </summary>
        private static bool InScaledHitbox(BulletAttack attack, BulletAttack.BulletHit hit, float scale)
        {
            foreach (HurtBox hurtBox in hit.hitHurtBox.hurtBoxGroup.hurtBoxes)
            {
                Vector3 center = hurtBox.collider.bounds.center;
                Vector3 start = center + (hit.ray.origin - center) / scale;
                Vector3 end = start + hit.ray.direction * (attack.maxDistance / scale);
                if (Distance(hurtBox.collider, start, end) <= attack.radius)
                {
                    return true;
                }
            }
            return false;
        }

        // The distance between a segment and a convex collider, by alternating between the closest points on each
        // (which converges for convex shapes; hurtboxes are boxes, spheres and capsules).
        private static float Distance(Collider collider, Vector3 start, Vector3 end)
        {
            Vector3 onSegment = ClosestOnSegment(start, end, collider.bounds.center);
            for (int i = 0; i < 4; i++)
            {
                onSegment = ClosestOnSegment(start, end, collider.ClosestPoint(onSegment));
            }
            return Vector3.Distance(onSegment, collider.ClosestPoint(onSegment));
        }

        private static Vector3 ClosestOnSegment(Vector3 start, Vector3 end, Vector3 point)
        {
            Vector3 segment = end - start;
            float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / segment.sqrMagnitude);
            return start + t * segment;
        }
    }
}
