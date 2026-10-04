using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ShinAndMang
{
    /// <summary>
    /// The first shot of a burst fired while holding Mang (望) rings becomes a rail shot, spending the rings.
    /// </summary>
    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    public static class Patch_VerbLaunchProjectile_TryCastShot
    {
        private static readonly AccessTools.FieldRef<Verb, int> BurstShotsLeft = AccessTools.FieldRefAccess<Verb, int>("burstShotsLeft");

        public static bool Prefix(Verb_LaunchProjectile __instance, ref bool __result)
        {
            Pawn shooter = __instance.CasterPawn;
            if (shooter == null || !shooter.Spawned) return true;

            // Only the first shot of a burst.
            if (BurstShotsLeft(__instance) != __instance.BurstShotCount) return true;

            int ringCount = MangMechanics.CurrentRings(shooter);
            if (ringCount == 0) return true;

            ThingDef projectileDef = __instance.GetProjectile();
            if (projectileDef?.projectile == null) return true;

            // Projectiles that can't hurt or disable (smoke, firefoam) don't use the rings.
            DamageDef projectileDamage = projectileDef.projectile.damageDef;
            if (projectileDamage == null || !(projectileDamage.harmsHealth || projectileDamage == DamageDefOf.EMP)) return true;

            LocalTargetInfo target = __instance.CurrentTarget;
            if (!target.IsValid) return true;

            // Spend the rings first, so they flare where they are as the shot leaves.
            MangMechanics.ConsumeRings(shooter, MangRingEnding.Flare);

            // Ordinary bullets become a rail shot; anything else flies normally, empowered.
            bool canBecomeRail = typeof(Bullet).IsAssignableFrom(projectileDef.thingClass) && projectileDef.projectile.explosionRadius <= 0f;
            if (canBecomeRail)
            {
                MangRail.Fire(shooter, __instance, target, ringCount);
                __result = true;   // the shot happened, so sound, muzzle flash and burst counting continue as normal
                return false;      // skip the normal bullet
            }

            MangMechanics.SetPendingEmpowerment(shooter, MangMechanics.DamageMultiplier(ringCount));
            return true;
        }

        public static void Postfix(Verb_LaunchProjectile __instance)
        {
            if (__instance.CasterPawn != null) MangMechanics.ClearPendingEmpowerment(__instance.CasterPawn);
        }
    }
}
