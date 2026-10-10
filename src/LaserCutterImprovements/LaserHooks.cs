using System;
using EntityStates.Engi.EngiWeapon;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace LaserCutterImprovements
{
    /// <summary>
    /// The hooks on the Laser Cutter's entity state, <see cref="EngiLaserCutter"/> (its subclasses EngiLaserCutter2
    /// and 3 are the later, stronger stages of the same beam and share these methods).
    /// </summary>
    internal static class LaserHooks
    {
        public static void Init()
        {
            IL.EntityStates.Engi.EngiWeapon.EngiLaserCutter.FireLaser += EngiLaserCutter_FireLaser;
            On.EntityStates.Engi.EngiWeapon.EngiLaserCutter.UpdateLaser += EngiLaserCutter_UpdateLaser;
        }

        // FireLaser builds the beam's BulletAttack and fires it, on the owner only (isAuthority); give it this mod's
        // callbacks just before the Fire call.
        private static void EngiLaserCutter_FireLaser(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (!c.TryGotoNext(x => x.MatchCallOrCallvirt<BulletAttack>(nameof(BulletAttack.Fire))))
            {
                LaserCutterImprovementsPlugin.Log.LogError($"IL hook {il.Method.Name} failed: BulletAttack.Fire not found");
                return;
            }
            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<BulletAttack, EngiLaserCutter>>(ConfigurePlayerBeam);
        }

        // The settings are the player's own: AI lasers (fired by the server) stay as in the game.
        private static void ConfigurePlayerBeam(BulletAttack attack, EngiLaserCutter laser)
        {
            if (laser.characterBody.isPlayerControlled)
            {
                LaserHits.Configure(attack);
            }
        }

        // UpdateLaser places the end of the visible beam every frame, on every machine, with a thin ray that misses
        // what the beam's radius hits. After it, for the local player's own laser (other players' settings aren't
        // known here), find where the attack really stops: end the beam there (BeamStopsAtTarget) and show the range
        // indicator if it stops nowhere (RangeIndicator).
        private static void EngiLaserCutter_UpdateLaser(On.EntityStates.Engi.EngiWeapon.EngiLaserCutter.orig_UpdateLaser orig,
            EngiLaserCutter self, Ray aimRay)
        {
            orig(self, aimRay);
            bool beamStopsAtTarget = PluginConfig.BeamStopsAtTarget.Value;
            bool rangeIndicator = PluginConfig.RangeIndicator.Value;
            if (!self.isAuthority || !self.characterBody.isPlayerControlled || !beamStopsAtTarget && !rangeIndicator)
            {
                return;
            }
            bool stops = LaserHits.Probe(self, aimRay, out Vector3 end);
            if (beamStopsAtTarget)
            {
                self.laserEndPoint = end;
                if (self.laserEndL)
                {
                    self.laserEndL.position = end;
                }
                if (self.laserEndR)
                {
                    self.laserEndR.position = end;
                }
            }
            if (rangeIndicator && !stops)
            {
                RangeIndicator.Show(self.characterBody, aimRay.origin, end - aimRay.origin, self.laserDistance);
            }
        }
    }
}
