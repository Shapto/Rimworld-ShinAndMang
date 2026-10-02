using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private const int MeditationCheckIntervalTicks = 250;

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
            if (!pawn.IsHashIntervalTick(MeditationCheckIntervalTicks)) return;
            if (!ModsConfig.RoyaltyActive || !pawn.Spawned) return;
            bool isMeditating = pawn.CurJobDef == JobDefOf.Meditate;
            bool isWalking = pawn.pather.MovingNow;
            if (isMeditating && !isWalking)
            {
                ShinMechanics.GainMeditationMastery(pawn, MeditationCheckIntervalTicks);
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (variant == null) variant = ShinMechanics.RollVariant();
            if (pawn.Faction == Faction.OfPlayer && pawn.Spawned)
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
        }

    }
}
