using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;
using static RimWorld.PsychicRitualRoleDef;

namespace ShinAndMang
{
    /// <summary>
    /// Forming and spending Mang (望) rings.
    /// </summary>
    public static class MangMechanics
    {
        public static Hediff_Mang GetRings(Pawn pawn) => pawn?.health?.hediffSet?.GetFirstHediffOfDef(ShinDefOf.Mang_Rings) as Hediff_Mang;

        public static int CurrentRings(Pawn pawn) => GetRings(pawn)?.RingCount ?? 0;

        /// <summary>
        /// Most rings the pawn can hold: their mastery rank.
        /// </summary>
        public static int MaximumRings(Pawn pawn) => ShinMechanics.GetMastery(pawn)?.MasteryTier ?? 0;

        private static readonly Dictionary<Pawn, int> activeStrikeRings = new Dictionary<Pawn, int>();

        /// <summary>
        /// Mood cost of forming ring number "ringNumber" (1 to 7).
        /// </summary>
        public static float RingCost(Pawn pawn, int ringNumber)
        {
            MangSettings settings = ShinDefOf.Mang_Rings.GetModExtension<MangSettings>();
            Hediff_ShinMastery mastery = ShinMechanics.GetMastery(pawn);
            if (settings?.baseCostByMastery == null || mastery == null) return float.MaxValue;
            float baseCost = settings.baseCostByMastery.Evaluate(mastery.Severity);
            return baseCost * Mathf.Pow(2f, ringNumber - 1);
        }

        /// <summary>
        /// Chance that forming ring number "ringNumber" succeeds. Only the newest unlocked ring can fail.
        /// </summary>
        public static float RingSuccessChance(Pawn pawn, int ringNumber)
        {
            if (ringNumber < MaximumRings(pawn)) return 1;
            Hediff_ShinMastery mastery = ShinMechanics.GetMastery(pawn);
            MangSettings settings = ShinDefOf.Mang_Rings.GetModExtension<MangSettings>();
            if (mastery == null || settings == null) return 1f;

            List<HediffStage> stages = mastery.def.stages;

            //Sovereign's seventh, which is always reliable.
            if (ringNumber + 1 >= stages.Count) return 1f;

            float lowerThreshold = stages[ringNumber].minSeverity;      // where this ring unlocked
            float upperThreshold = stages[ringNumber + 1].minSeverity;  // where the next one unlocks
            float progress = Mathf.InverseLerp(lowerThreshold, upperThreshold, mastery.Severity);
            return Mathf.Lerp(settings.newestRingChanceAtUnlock, 1f, progress);
        }

        public static bool CanFormRing(Pawn pawn, out string reason)
        {
            reason = null;

            if (ShinMechanics.GetMastery(pawn) == null) { reason = "ShinAndMang_NoShin".Translate(); return false; }
            if (pawn.Dead || pawn.Downed) { reason = "ShinAndMang_Incapacitated".Translate(); return false; }
            if (pawn.WorkTagIsDisabled(WorkTags.Violent)) { reason = "ShinAndMang_IncapableOfViolence".Translate(); return false; }

            int maximumRings = MaximumRings(pawn);
            if (maximumRings == 0) { reason = "ShinAndMang_NoRingsUnlocked".Translate(); return false; }
            if (CurrentRings(pawn) >= maximumRings) { reason = "ShinAndMang_RingsFull".Translate(maximumRings); return false; }

            Need_Mood mood = pawn.needs?.mood;
            if (mood == null) { reason = "ShinAndMang_NoMood".Translate(); return false; }

            float nextRingCost = RingCost(pawn, CurrentRings(pawn) + 1);
            if (mood.CurLevel < nextRingCost) { reason = "ShinAndMang_NotEnoughMood".Translate(mood.CurLevel.ToStringPercent(), nextRingCost.ToStringPercent()); return false; }

            return true;
        }

        /// <summary>
        /// True if paying this cost would drop the pawn's mood into minor break range.
        /// </summary>
        public static bool WouldRiskBreak(Pawn pawn, float moodCost)
        {
            Need_Mood mood = pawn.needs?.mood;
            if (mood == null) return true;

            float moodAfterPaying = mood.CurLevel - moodCost;
            return moodAfterPaying <= pawn.mindState.mentalBreaker.BreakThresholdMinor;
        }

        /// <summary>
        /// Tries to form one more ring. The mood is spent even if the ring fizzles.
        /// </summary>
        public static bool TryFormRing(Pawn pawn)
        {
            if (!CanFormRing(pawn, out _)) return false;
            int ringNumber = CurrentRings(pawn) + 1;
            ShinMechanics.SpendMood(pawn, RingCost(pawn, ringNumber));
            MangSettings settings = ShinDefOf.Mang_Rings.GetModExtension<MangSettings>();
            ShinMasteryGainExtension gainSettings = ShinDefOf.Shin_Mastery.GetModExtension<ShinMasteryGainExtension>();
            if (settings != null && gainSettings != null)
            {
                float masteryGain = settings.masteryGainPerRingInCombat * ringNumber;
                if (!ShinMechanics.IsActivelyFighting(pawn, gainSettings.recentCombatTicks)) masteryGain *= settings.outOfCombatMasteryFactor;
                ShinMechanics.GainMastery(pawn, masteryGain);
            }
            if (!Rand.Chance(RingSuccessChance(pawn, ringNumber)))
            {
                if (pawn.Spawned) MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "ShinAndMang_RingFizzled".Translate(), Color.gray);
                return false;
            }
            Hediff_Mang rings = GetRings(pawn);
            if (rings == null)
            {
                pawn.health.AddHediff(ShinDefOf.Mang_Rings);
            }
            else
            {
                rings.Severity += 1f;
                rings.Notify_RingAdded();
            }
            if (pawn.Spawned) ShinDefOf.Mang_RingForm.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            return true;
        }

        /// <summary>
        /// Total rings the pawn could hold after forming more right now, without dropping into minor break range.
        /// </summary>
        public static int SafeRingCount(Pawn pawn)
        {
            Need_Mood mood = pawn.needs?.mood;
            if (mood == null) return CurrentRings(pawn);
            float moodLeft = mood.CurLevel;
            int ringCount = CurrentRings(pawn);
            int maximumRings = MaximumRings(pawn);
            for (int ringNumber = ringCount + 1; ringNumber <= maximumRings; ringNumber++)
            {
                moodLeft -= RingCost(pawn, ringNumber);
                if (moodLeft <= pawn.mindState.mentalBreaker.BreakThresholdMinor) break;
                ringCount++;
            }
            return ringCount;
        }

        /// <summary>
        /// Mood cost of forming rings from the current count up to the target, if none fizzle.
        /// </summary>
        public static float TotalCostUpTo(Pawn pawn, int targetRings)
        {
            float totalCost = 0f;

            for (int ringNumber = CurrentRings(pawn) + 1; ringNumber <= targetRings; ringNumber++)
            {
                totalCost += RingCost(pawn, ringNumber);
            }
            return totalCost;
        }

        //Offense

        /// <summary>
        /// Removes the pawn's rings and returns how many there were (0 if none).
        /// </summary>
        public static int ConsumeRings(Pawn pawn)
        {
            Hediff_Mang rings = GetRings(pawn);
            if (rings == null) return 0;

            int ringCount = rings.RingCount;
            pawn.health.RemoveHediff(rings);
            return ringCount;
        }

        /// <summary>
        /// Damage multiplier for an attack carrying this many rings: the per-ring multiplier to the power of the ring count.
        /// </summary>
        public static float DamageMultiplier(int ringCount)
        {
            float multiplierPerRing = ShinDefOf.Mang_Rings.GetModExtension<MangSettings>()?.damageMultiplierPerRing ?? 1.35f;
            return Mathf.Pow(multiplierPerRing, ringCount);
        }

        public static void BeginStrike(Pawn pawn, int ringCount) => activeStrikeRings[pawn] = ringCount;
        public static void EndStrike(Pawn pawn) => activeStrikeRings.Remove(pawn);
        public static bool TryGetActiveStrike(Pawn pawn, out int ringCount) => activeStrikeRings.TryGetValue(pawn, out ringCount);
    }
}
