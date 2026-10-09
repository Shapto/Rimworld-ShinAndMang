using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;
using static RimWorld.PsychicRitualRoleDef;
using static ShinAndMang.MapComponent_MangRings;
using static ShinAndMang.ShinAndMangSettings;

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
        /// How many rings the pawn can hold: one per mastery rank, capped by the setting.
        /// Every ring past the seventh unlocks at 100% mastery, so a Sovereign gets the setting's full value.
        /// </summary>
        public static int MaximumRings(Pawn pawn)
        {
            int masteryTier = ShinMechanics.GetMastery(pawn)?.MasteryTier ?? 0;
            int ringSetting = ShinAndMangMod.Settings.maximumMangRings;
            return masteryTier >= 7 ? ringSetting : Mathf.Min(masteryTier, ringSetting);
        }

        /// <summary>
        /// True if this attack can carry Mang (望) rings: melee strikes, and weapons that launch projectiles.
        /// Beam weapons and other special attacks can't.
        /// </summary>
        public static bool CanUseMang(Verb verb) => verb is Verb_MeleeAttack || (verb is Verb_LaunchProjectile && verb.EquipmentSource != null);

        private class ActiveStrike
        {
            public int ringCount;
            public bool landed;
        }

        private class PendingMangShot
        {
            public int ringCount;
            public float range;
        }


        private static readonly Dictionary<Pawn, ActiveStrike> activeStrikes = new Dictionary<Pawn, ActiveStrike>();

        private static readonly Dictionary<Pawn, PendingMangShot> pendingMangShots = new Dictionary<Pawn, PendingMangShot>();

        private static readonly ConditionalWeakTable<Projectile, StrongBox<float>> empoweredProjectiles = new ConditionalWeakTable<Projectile, StrongBox<float>>();

        /// <summary>
        /// Mood cost of forming ring number "ringNumber".
        /// </summary>
        public static float RingCost(Pawn pawn, int ringNumber)
        {
            MangSettings settings = ShinDefOf.Mang_Rings.GetModExtension<MangSettings>();
            Hediff_ShinMastery mastery = ShinMechanics.GetMastery(pawn);
            if (settings?.baseCostByMastery == null || mastery == null) return float.MaxValue;

            int doublings = ringNumber - 1;
            if (ringNumber > 7)
            {
                ExtraMangRingCost extraRingCost = ShinAndMangMod.Settings.ringsPastSeventhCost;
                if (extraRingCost == ExtraMangRingCost.Free) return 0f;
                if (extraRingCost == ExtraMangRingCost.SameAsSeventh) doublings = 6;
            }

            float baseCost = settings.baseCostByMastery.Evaluate(mastery.Severity);
            return baseCost * Mathf.Pow(2f, doublings);
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
            if (pawn.Faction != null && pawn.Faction.IsPlayer && !pawn.Drafted) { reason = "ShinAndMang_NotInCombat".Translate(); return false; }
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
                if (pawn.Spawned) MapComponent_MangRings.NotifyFizzle(pawn, ringNumber);
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
            if (pawn.Spawned) ShinAndMangSoundPlayer.PlayOneShot(ShinDefOf.Mang_RingForm, pawn, ShinAndMangSoundCategory.MangRings);
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
        /// Removes the pawn's rings, ending them the given way, and returns how many there were (0 if none).
        /// </summary>
        public static int ConsumeRings(Pawn pawn, MangRingEnding? ending = null)
        {
            Hediff_Mang rings = GetRings(pawn);
            if (rings == null) return 0;

            int ringCount = rings.RingCount;
            rings.removalEnding = ending;
            pawn.health.RemoveHediff(rings);
            return ringCount;
        }

        /// <summary>
        /// The largest damage a single Mang (望) hit can deal. Higher would overflow RimWorld's damage math (negative or NaN damage).
        /// </summary>
        public const float MaximumSafeDamage = 1000000000f;

        /// <summary>
        /// Damage multiplier for an attack carrying this many rings: the per-ring multiplier to the power of the ring count.
        /// </summary>
        public static float DamageMultiplier(int ringCount)
        {
            float multiplierPerRing = ShinDefOf.Mang_Rings.GetModExtension<MangSettings>()?.damageMultiplierPerRing ?? 1.35f;
            return Mathf.Pow(multiplierPerRing, ringCount);
        }

        /// <summary>
        /// Base damage with the rings' multiplier applied, clamped to a safe maximum.
        /// </summary>
        public static float MultipliedDamage(float baseDamage, int ringCount)
        {
            if (baseDamage <= 0f) return baseDamage;
            return Mathf.Min(baseDamage * DamageMultiplier(ringCount), MaximumSafeDamage);
        }

        public static void BeginStrike(Pawn pawn, int ringCount) => activeStrikes[pawn] = new ActiveStrike { ringCount = ringCount };

        public static bool TryGetActiveStrike(Pawn pawn, out int ringCount)
        {
            ringCount = activeStrikes.TryGetValue(pawn, out ActiveStrike strike) ? strike.ringCount : 0;
            return strike != null;
        }


        /// <summary>
        /// Called when the strike's damage actually reaches a target.
        /// </summary>
        public static void MarkStrikeLanded(Pawn pawn)
        {
            if (activeStrikes.TryGetValue(pawn, out ActiveStrike strike)) strike.landed = true;
        }

        /// <summary>
        /// The shooter's next launched projectile carries these rings.
        /// </summary>
        public static void SetPendingMangShot(Pawn shooter, int ringCount, float range) => pendingMangShots[shooter] = new PendingMangShot { ringCount = ringCount, range = range };

        /// <summary>
        /// Takes (and removes) a pending Mang (望) shot for this launcher, if there is one.
        /// </summary>
        public static bool TryTakePendingMangShot(Thing launcher, out int ringCount, out float range)
        {
            ringCount = 0;
            range = 0f;
            if (!(launcher is Pawn shooter) || !pendingMangShots.TryGetValue(shooter, out PendingMangShot pendingShot)) return false;

            pendingMangShots.Remove(shooter);
            ringCount = pendingShot.ringCount;
            range = pendingShot.range;
            return true;
        }

        public static void ClearPendingMangShot(Pawn shooter) => pendingMangShots.Remove(shooter);

        /// <summary>
        /// Remembers that this projectile's damage is multiplied. Forgotten automatically once the projectile is gone.
        /// </summary>
        public static void EmpowerProjectile(Projectile projectile, float damageMultiplier)
        {
            empoweredProjectiles.Remove(projectile);
            empoweredProjectiles.Add(projectile, new StrongBox<float>(damageMultiplier));
        }

        public static bool TryGetProjectileEmpowerment(Projectile projectile, out float damageMultiplier)
        {
            bool isEmpowered = empoweredProjectiles.TryGetValue(projectile, out StrongBox<float> storedMultiplier);
            damageMultiplier = isEmpowered ? storedMultiplier.Value : 1f;
            return isEmpowered;
        }

        /// <summary>
        /// True if ring number "ringNumber" is fully reliable for this pawn; otherwise it's drawn unstable.
        /// </summary>
        public static bool IsRingStable(Pawn pawn, int ringNumber) => RingSuccessChance(pawn, ringNumber) >= 1f;

        /// <summary>
        /// Ends the strike and returns whether it landed.
        /// </summary>
        public static bool EndStrike(Pawn pawn)
        {
            bool landed = activeStrikes.TryGetValue(pawn, out ActiveStrike strike) && strike.landed;
            activeStrikes.Remove(pawn);
            return landed;
        }
    }
}
