using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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

        public static void Postfix(Projectile __instance, Thing launcher)
        {
            if (MangMechanics.TryTakePendingEmpowerment(launcher, out float damageMultiplier)) MangMechanics.EmpowerProjectile(__instance, damageMultiplier);
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
            if (MangMechanics.TryGetProjectileEmpowerment(__instance, out float damageMultiplier)) __result = UnityEngine.Mathf.RoundToInt(__result * damageMultiplier);
        }
    }
}
