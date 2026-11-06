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

        private bool variantConfirmed;

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
            bool needsVariantChoice = !variantConfirmed && ShinAndMangMod.Settings.allowChoosingShinVariant && pawn.Spawned && pawn.Faction == Faction.OfPlayer;
            if (needsVariantChoice && !Find.WindowStack.IsOpen(typeof(Dialog_ChooseShinVariant))) Find.WindowStack.Add(new Dialog_ChooseShinVariant(this));
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

            // Only weapon attacks build rings: aiming a weapon, or recovering between melee swings.
            bool isAimingWeapon = pawn.stances?.curStance is Stance_Warmup warmup && warmup.verb?.EquipmentSource != null && MangMechanics.CanUseMang(warmup.verb);
            bool isBetweenSwings = pawn.stances?.curStance is Stance_Cooldown cooldownStance && cooldownStance.verb is Verb_MeleeAttack;
            bool isWindingUp = isAimingWeapon || isBetweenSwings;
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
            if (ShinAndMangMod.Settings.allowChoosingShinVariant) return; // the choice window confirms it and sends the letter
            variantConfirmed = true;
            SendLearnedShinLetter();
        }

        /// <summary>
        /// Locks in the pawn's variant. A null choice keeps the random roll.
        /// </summary>
        public void ConfirmVariant(HediffDef chosenVariant)
        {
            ShinMechanics.EndShin(pawn);
            if (chosenVariant != null) variant = chosenVariant;
            variantConfirmed = true;
            SendLearnedShinLetter();
        }

        /// <summary>
        /// The "learned Shin (心)" letter, only for spawned player pawns.
        /// </summary>
        private void SendLearnedShinLetter()
        {
            if (!pawn.Spawned || pawn.Faction == null || !pawn.Faction.IsPlayer) return;
            string variantLabel = variant?.label ?? "?";
            Find.LetterStack.ReceiveLetter("ShinAndMang_LearnedShinLabel".Translate(), "ShinAndMang_LearnedShinText".Translate(pawn.LabelShort, variantLabel), LetterDefOf.PositiveEvent, pawn);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref variant, "variant");

            // Variant's def was removed (e.g. its mod was uninstalled): reroll instead of breaking.
            if (Scribe.mode == LoadSaveMode.PostLoadInit && variant == null) variant = ShinMechanics.RollVariant();

            // Mastery corrupted to NaN by an older version: reset instead of staying broken.
            if (Scribe.mode == LoadSaveMode.PostLoadInit && float.IsNaN(Severity)) Severity = 0f;

            Scribe_Values.Look(ref variantConfirmed, "variantConfirmed", true);

            Scribe_Values.Look(ref autoFormMang, "autoFormMang", true);
            Scribe_Values.Look(ref mangTargetRings, "mangTargetRings", 7);
        }

    }
}
