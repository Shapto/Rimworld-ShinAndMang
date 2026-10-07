using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ShinAndMang
{
    /// <summary>
    /// A projectile launched with a pending Mang (望) empowerment is remembered as empowered.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_Projectile_Launch
    {
        // Launch has several versions; the longest is the one the others end up calling.
        private static MethodBase TargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(Projectile))
                .Where(method => method.Name == "Launch")
                .OrderByDescending(method => method.GetParameters().Length)
                .First();
        }

        public static void Postfix(Projectile __instance, Thing launcher, LocalTargetInfo intendedTarget)
        {
            if (!MangMechanics.TryTakePendingMangShot(launcher, out int ringCount, out float range)) return;

            // Ordinary bullets become a rail shot, replacing the bullet; anything else flies on, empowered.
            bool canBecomeRail = __instance is Bullet && __instance.def.projectile.explosionRadius <= 0f;
            if (canBecomeRail && launcher is Pawn shooter)
            {
                MangRail.QueueFire(shooter, __instance, intendedTarget, ringCount, range);
                return;
            }

            MangMechanics.EmpowerProjectile(__instance, MangMechanics.DamageMultiplier(ringCount));
        }
    }

    /// <summary>
    /// Empowered projectiles deal multiplied damage, on impact and in explosions alike.
    /// </summary>
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.DamageAmount), MethodType.Getter)]
    public static class Patch_Projectile_DamageAmount
    {
        public static void Postfix(Projectile __instance, ref int __result)
        {
            if (MangMechanics.TryGetProjectileEmpowerment(__instance, out float damageMultiplier)) __result = Mathf.RoundToInt(Mathf.Min(__result * damageMultiplier, MangMechanics.MaximumSafeDamage));
        }
    }
}
