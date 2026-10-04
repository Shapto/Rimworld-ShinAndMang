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
    /// The first shot of a burst fired while holding Mang (望) rings spends them, and marks the next launched
    /// projectile. Never skips the shot itself, so other mods that change firing (ammo, accuracy) keep working.
    /// </summary>
    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    public static class Patch_VerbLaunchProjectile_TryCastShot
    {
        private static readonly AccessTools.FieldRef<Verb, int> BurstShotsLeft = AccessTools.FieldRefAccess<Verb, int>("burstShotsLeft");

        [HarmonyPriority(Priority.First)]
        public static void Prefix(Verb_LaunchProjectile __instance)
        {
            Pawn shooter = __instance.CasterPawn;
            if (shooter == null || !shooter.Spawned) return;

            // Only the first shot of a burst.
            if (BurstShotsLeft(__instance) != __instance.BurstShotCount) return;

            int ringCount = MangMechanics.CurrentRings(shooter);
            if (ringCount == 0) return;

            ThingDef projectileDef = __instance.GetProjectile();
            if (projectileDef?.projectile == null) return;

            // Projectiles that can't hurt or disable (smoke, firefoam) don't use the rings.
            DamageDef projectileDamage = projectileDef.projectile.damageDef;
            if (projectileDamage == null || !(projectileDamage.harmsHealth || projectileDamage == DamageDefOf.EMP)) return;

            if (!__instance.CurrentTarget.IsValid) return;

            // Spend the rings, so they flare where they are as the shot leaves, and mark the coming projectile.
            MangMechanics.ConsumeRings(shooter, MangRingEnding.Flare);
            MangMechanics.SetPendingMangShot(shooter, ringCount, __instance.verbProps.range);
        }

        public static void Postfix(Verb_LaunchProjectile __instance)
        {
            // Whatever happened, a note must never carry over to a later shot.
            if (__instance.CasterPawn != null) MangMechanics.ClearPendingMangShot(__instance.CasterPawn);
        }
    }
}
