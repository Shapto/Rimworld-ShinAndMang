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
    public class Hediff_ShinMastery : HediffWithComps
    {
        private HediffDef variant;
        public HediffDef Variant => variant;

        /// <summary>0 at the lowest rank, 7 at Sovereign. Drives both Shin (心) strength and the maximum Mang (望) rings.</summary>
        public int MasteryTier => CurStageIndex;

        //prevents removal of mastery at 0% as its supposed to start at 0
        public override bool ShouldRemove => false;

        private const int CheckIntervalTicks = 250;

        /// <summary>
        /// How many Mang (望) rings the player wants formed: used by Form Mang (望) and by automatic formation.
        /// </summary>
        public int mangTargetRings = 7;

        /// <summary>
        /// Whether Mang (望) rings form on their own during attack windups.
        /// </summary>
        public bool autoFormMang = true;

        private int ticksUntilNextAutoRing;

        private const int AutoTicksPerRing = 8;   // during a windup, about 7 rings per second

        // "Shin (心) Sovereign (100%)"
        public override string LabelInBrackets
        {
            get
            {
                string stageLabel = base.LabelInBrackets;
                string masteryPercentage = Severity.ToStringPercent();
                return stageLabel.NullOrEmpty() ? masteryPercentage : stageLabel + ", " + masteryPercentage;
            }
        }


        public override void PostTick()
        {
            base.PostTick();
            TickAutoFormation();
            if (!pawn.IsHashIntervalTick(CheckIntervalTicks)) return;
            TryAutoActivateShin();
            if (!ModsConfig.RoyaltyActive || !pawn.Spawned) return;
            bool isMeditating = pawn.CurJobDef == JobDefOf.Meditate;
            bool isWalking = pawn.pather.MovingNow;
            if (isMeditating && !isWalking)
            {
                ShinMechanics.GainMeditationMastery(pawn, CheckIntervalTicks);
            }
        }

        /// <summary>
        /// Forms Mang (望) rings during an attack's windup: while aiming, or between melee swings.
        /// </summary>
        private void TickAutoFormation()
        {
            if (!autoFormMang || !pawn.Spawned) return;

            bool isWindingUp = pawn.stances?.curStance is Stance_Warmup || (pawn.stances?.curStance is Stance_Cooldown cooldownStance && cooldownStance.verb is Verb_MeleeAttack);
            if (!isWindingUp)
            {
                ticksUntilNextAutoRing = AutoTicksPerRing;
                return;
            }

            ticksUntilNextAutoRing--;
            if (ticksUntilNextAutoRing > 0) return;
            ticksUntilNextAutoRing = AutoTicksPerRing;

            int targetRings = Mathf.Min(mangTargetRings, MangMechanics.MaximumRings(pawn));
            int nextRingNumber = MangMechanics.CurrentRings(pawn) + 1;
            if (nextRingNumber > targetRings) return;
            if (!MangMechanics.CanFormRing(pawn, out _)) return;
            if (MangMechanics.WouldRiskBreak(pawn, MangMechanics.RingCost(pawn, nextRingNumber))) return;

            MangMechanics.TryFormRing(pawn);
        }

        /// <summary>
        /// AI pawns manifest Shin (心) on their own when a fight starts.
        /// </summary>
        private void TryAutoActivateShin()
        {
            if (!pawn.Spawned) return;
            // The player decides for their own pawns.
            if (pawn.Faction != null && pawn.Faction.IsPlayer) return;
            if (ShinMechanics.IsShinActive(pawn)) return;

            ShinMasteryGainExtension gainSettings = ShinDefOf.Shin_Mastery.GetModExtension<ShinMasteryGainExtension>();
            if (gainSettings == null) return;

            // Only once the fight has become emotional enough.
            if (ShinMechanics.CombatIntensity(pawn, gainSettings) < gainSettings.aiActivationIntensity) return;

            // Don't spend the last of their mood and break right after.
            Need_Mood mood = pawn.needs?.mood;
            if (mood == null) return;
            float moodAfterCost = mood.CurLevel - ShinMechanics.ActivationMoodCost(pawn);
            if (moodAfterCost <= pawn.mindState.mentalBreaker.BreakThresholdMinor) return;

            // Checks being in combat and affording the cost itself.
            ShinMechanics.TryActivateShin(pawn);
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (variant == null) variant = ShinMechanics.RollVariant();
            if (pawn.Spawned && pawn.Faction != null && pawn.Faction.IsPlayer)
            {
                string variantLabel = variant?.label ?? "?";
                Find.LetterStack.ReceiveLetter("ShinAndMang_LearnedShinLabel".Translate(), "ShinAndMang_LearnedShinText".Translate(pawn.LabelShort, variantLabel), LetterDefOf.PositiveEvent, pawn);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref variant, "variant");

            // Variant's def was removed (e.g. its mod was uninstalled): reroll instead of breaking.
            if (Scribe.mode == LoadSaveMode.PostLoadInit && variant == null)
                variant = ShinMechanics.RollVariant();

            Scribe_Values.Look(ref autoFormMang, "autoFormMang", true);
        }

    }
}
