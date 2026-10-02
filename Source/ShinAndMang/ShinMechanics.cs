using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ShinAndMang
{
    public class ShinMechanics
    {
        /// <summary>
        /// Retrieves the Shin Mastery hediff associated with the specified pawn, if present.
        /// </summary>
        /// <param name="pawn">The pawn from which to retrieve the Shin Mastery hediff. Can be null.</param>
        /// <returns>The Shin Mastery hediff for the specified pawn, or null if the pawn does not have the hediff or if the pawn
        /// is null.</returns>
        public static Hediff_ShinMastery GetMastery(Pawn pawn) => pawn?.health?.hediffSet?.GetFirstHediffOfDef(ShinDefOf.Shin_Mastery) as Hediff_ShinMastery;

        /// <summary>
        /// Retrieves the first active Hediff_Shin instance from the specified pawn, if present.
        /// </summary>
        /// <param name="pawn">The pawn whose health conditions are searched for an active Hediff_Shin. Can be null.</param>
        /// <returns>The first Hediff_Shin found in the pawn's health conditions; otherwise, null if none is present or if the
        /// pawn is null.</returns>
        public static Hediff_Shin GetActiveShin(Pawn pawn) => pawn?.health?.hediffSet?.hediffs.FirstOrDefault(hediff => hediff is Hediff_Shin) as Hediff_Shin;

        /// <summary>
        /// Determines whether the specified pawn currently has an active Shin effect.
        /// </summary>
        /// <param name="pawn">The pawn to check for an active Shin effect. Cannot be null.</param>
        /// <returns>true if the pawn has an active Shin effect; otherwise, false.</returns>
        public static bool IsShinActive(Pawn pawn) => GetActiveShin(pawn) != null;

        /// <summary>
        /// Gets the mood cost required to activate the specified pawn's mastery ability.
        /// </summary>
        /// <param name="pawn">The pawn for which to retrieve the activation mood cost. Cannot be null.</param>
        /// <returns>The mood cost required to activate the pawn's mastery ability. Returns 0 if the pawn does not have a mastery
        /// or the relevant component.</returns>
        public static float ActivationMoodCost(Pawn pawn) => GetMastery(pawn)?.TryGetComp<HediffComp_ShinActivation>()?.Props.activationMoodCost ?? 0f;

        /// <summary>
        /// Determines whether the specified hediff definition represents a Shin variant.
        /// </summary>
        /// <param name="hediffDef">The hediff definition to evaluate. Cannot be null.</param>
        /// <returns>true if the hediff definition's class is assignable from Hediff_Shin; otherwise, false.</returns>
        public static bool IsShinVariantDef(HediffDef hediffDef) => hediffDef.hediffClass != null && typeof(Hediff_Shin).IsAssignableFrom(hediffDef.hediffClass);

        /// <summary>
        /// Calculates the commonality value for the specified hediff definition.
        /// </summary>
        /// <param name="def">The hediff definition for which to determine the commonality value. Cannot be null.</param>
        /// <returns>A floating-point value representing the commonality of the specified hediff definition.</returns>
        private static float VariantCommonality(HediffDef def) => 1f;

        ///Shin (心) is a combat technique.
        public static bool IsInCombat(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed) return false;
            if (pawn.Faction != null && pawn.Faction.IsPlayer) return pawn.Drafted;
            bool hasEnemyTarget = pawn.mindState.enemyTarget != null;
            ShinMasteryGainExtension gainSettings = ShinDefOf.Shin_Mastery.GetModExtension<ShinMasteryGainExtension>();
            bool foughtRecently = gainSettings != null && IsActivelyFighting(pawn, gainSettings.recentCombatTicks);
            return (hasEnemyTarget || foughtRecently);
        }

        /// Every mastery gain calls this one.
        public static void GainMastery(Pawn pawn, float baseAmount)
        {
            Hediff_ShinMastery mastery = GetMastery(pawn);
            if (mastery == null) return;

            ShinMasteryGainExtension gainSettings = mastery.def.GetModExtension<ShinMasteryGainExtension>();

            float multiplier = gainSettings?.gainCurve?.Evaluate(mastery.Severity) ?? 1f;
            mastery.Severity = Mathf.Min(1f, mastery.Severity + baseAmount * multiplier);

            NotifyMasteryChanged(pawn);
        }

        /// <summary>
        /// Small mastery gain from meditation.
        /// </summary>
        public static void GainMeditationMastery(Pawn pawn, int intervalTicks)
        {
            ShinMasteryGainExtension gainSettings = ShinDefOf.Shin_Mastery.GetModExtension<ShinMasteryGainExtension>();
            if (gainSettings == null) return;
            GainMastery(pawn, gainSettings.meditationGainPerHour * (intervalTicks / 2500f));
        }

        /// <summary>
        /// True if the pawn attacked or was hit recently, not just standing around drafted.
        /// </summary>
        public static bool IsActivelyFighting(Pawn pawn, int recentCombatTicks)
        {
            int currentTick = Find.TickManager.TicksGame;
            bool attackedRecently = currentTick - pawn.LastAttackTargetTick <= recentCombatTicks;
            bool harmedRecently = currentTick - pawn.mindState.lastHarmTick <= recentCombatTicks;
            return attackedRecently || harmedRecently;
        }

        /// <summary>
        /// Adults with a mood need who don't know Shin (心) yet.
        /// </summary>
        public static bool CanLearnShin(Pawn pawn)
        {
            if (GetMastery(pawn) != null) return false;
            if (pawn.needs?.mood == null) return false;
            if (!pawn.DevelopmentalStage.Adult()) return false;
            return true;
        }

        /// <summary>
        /// How emotional the fight is for this pawn: 1 = ordinary, up to maximumIntensity.
        /// </summary>
        public static float CombatIntensity(Pawn pawn, ShinMasteryGainExtension gainSettings)
        {
            float intensity = 1f;
            Need_Mood mood = pawn.needs?.mood;
            intensity += (1f - pawn.health.summaryHealth.SummaryHealthPercent) * gainSettings.woundIntensityWeight;
            if (mood != null)
            {
                intensity += (Mathf.Abs(mood.CurLevel - 0.5f) * 2f) * gainSettings.moodIntensityWeight;
            }
            if (pawn.Spawned)
            {
                foreach (Pawn ally in pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction))
                {
                    if (ally == pawn) continue;
                    if (ally.Downed && pawn.Position.InHorDistOf(ally.Position, gainSettings.allyDownedRadius))
                    {
                        intensity += gainSettings.allyDownedIntensity;
                        break;
                    }
                }
            }
            return Math.Min(intensity, gainSettings.maximumIntensity);
        }

        /// <summary>
        /// The only place mood is spent on Shin (心) or Mang (望). Tells the user's active variant how much was spent.
        /// </summary>
        public static void SpendMood(Pawn pawn, float moodCost)
        {
            Need_Mood mood = pawn.needs?.mood;
            if (mood == null || moodCost <= 0f) return;

            float moodBefore = mood.CurLevel;
            mood.CurLevel -= moodCost;
            float moodSpent = moodBefore - mood.CurLevel;

            foreach (ShinVariant variant in ShinVariantHooks.ActiveVariants(pawn))
                variant.Notify_MoodSpent(moodSpent);
        }

        public static bool CanActivateShin(Pawn pawn, out string disabledReason)
        {
            disabledReason = null;
            Hediff_ShinMastery mastery = GetMastery(pawn);

            if (mastery == null) { disabledReason = "ShinAndMang_NoShin".Translate(); return false; }
            if (mastery.Variant == null) { disabledReason = "ShinAndMang_NoVariant".Translate(); return false; }
            if (IsShinActive(pawn)) { disabledReason = "ShinAndMang_AlreadyActive".Translate(); return false; }
            if (!IsInCombat(pawn)) { disabledReason = "ShinAndMang_NotInCombat".Translate(); return false; }

            Need_Mood mood = pawn.needs?.mood;
            if (mood == null) { disabledReason = "ShinAndMang_NoMood".Translate(); return false; }

            float moodCost = ActivationMoodCost(pawn);
            if (mood.CurLevel < moodCost)
            {
                disabledReason = "ShinAndMang_NotEnoughMood".Translate(mood.CurLevel.ToStringPercent(), moodCost.ToStringPercent());
                return false;
            }
            return true;
        }

        public static bool TryActivateShin(Pawn pawn)
        {
            if (!CanActivateShin(pawn, out _)) return false;

            float moodCost = ActivationMoodCost(pawn);
            pawn.health.AddHediff(GetMastery(pawn).Variant);
            SpendMood(pawn, moodCost);
            return true;
        }

        public static void EndShin(Pawn pawn)
        {
            Hediff_Shin activeShin = GetActiveShin(pawn);
            if (activeShin != null) pawn.health.RemoveHediff(activeShin);
        }


        public static HediffDef RollVariant()
        {
            var variantPool = DefDatabase<HediffDef>.AllDefs.Where(IsShinVariantDef);

            return variantPool.TryRandomElementByWeight(VariantCommonality, out HediffDef rolledVariant) ? rolledVariant : null;
        }

        ///Call whenever mastery changes, so an active Shin (心) picks up its new stage.</summary>
        public static void NotifyMasteryChanged(Pawn pawn)
        {
            Hediff_Shin activeShin = GetActiveShin(pawn);
            if (activeShin != null) pawn.health.Notify_HediffChanged(activeShin);
        }
    }
}
