using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using HarmonyLib;

namespace ShinAndMang
{
    public class ShinVariantHooks
    {
        /// <summary>
        /// True while a variant's extra damage is being applied, so it doesn't re-trigger any hooks.
        /// </summary>
        public static bool IsApplyingExtraDamage { get; private set; }

        /// <summary>
        /// The variant classes on the pawn's active Shin (心), if any.
        /// </summary>
        public static IEnumerable<ShinVariant> ActiveVariants(Pawn pawn)
        {
            Hediff_Shin activeShin = ShinMechanics.GetActiveShin(pawn);
            if (activeShin?.comps == null) yield break;

            foreach (HediffComp comp in activeShin.comps)
            {
                if (comp is ShinVariant variant) yield return variant;
            }
        }

        /// <summary>
        /// All targeting readout lines from the user's active variants against this target, or an empty string.
        /// </summary>
        public static string TargetingReadout(Pawn shinUser, Thing target)
        {
            var readoutLines = new StringBuilder();
            foreach (ShinVariant variant in ActiveVariants(shinUser))
            {
                string readout = variant.TargetingReadout(target);
                if (!readout.NullOrEmpty()) readoutLines.AppendLine(readout);
            }
            return readoutLines.ToString().TrimEndNewlines();
        }

        public static void ApplyExtraDamage(Thing target, DamageInfo extraDamage)
        {
            IsApplyingExtraDamage = true;
            try { target.TakeDamage(extraDamage); }
            finally { IsApplyingExtraDamage = false; }
        }

        /// <summary>
        /// Every attack in the game passes through Thing.TakeDamage: melee, projectiles, abilities.
        /// </summary>
        [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
        public static class Patch_Thing_TakeDamage
        {
            public static void Prefix(Thing __instance, ref DamageInfo dinfo)
            {
                if (ShinVariantHooks.IsApplyingExtraDamage) return;

                // Attacker's variant first, then the victim's, so a defensive variant reduces the already-boosted hit.
                if (dinfo.Instigator is Pawn attacker)
                {
                    foreach (ShinVariant variant in ShinVariantHooks.ActiveVariants(attacker))
                        variant.ModifyOutgoingDamage(__instance, ref dinfo);
                }

                if (__instance is Pawn victim)
                {
                    foreach (ShinVariant variant in ShinVariantHooks.ActiveVariants(victim))
                        variant.ModifyIncomingDamage(ref dinfo);
                }
            }

            public static void Postfix(Thing __instance, DamageInfo dinfo, DamageWorker.DamageResult __result)
            {
                if (ShinVariantHooks.IsApplyingExtraDamage) return;

                if (dinfo.Instigator is Pawn attacker)
                {
                    foreach (ShinVariant variant in ShinVariantHooks.ActiveVariants(attacker))
                        variant.Notify_DamageDealt(__instance, dinfo, __result);
                }

                if (__instance is Pawn victim)
                {
                    foreach (ShinVariant variant in ShinVariantHooks.ActiveVariants(victim))
                        variant.Notify_DamageTaken(dinfo, __result);
                }
            }
        }
    }
}
