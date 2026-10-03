using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ShinAndMang
{
    /// <summary>
    /// The pawn stands still and forms Mang (望) rings one after another, until the target is reached.
    /// Being hurt breaks the concentration.
    /// </summary>
    public class JobDriver_FormMang : JobDriver
    {
        private const int TicksPerRing = 20;   // about one ring every 0.33 seconds

        private int ticksUntilNextRing = TicksPerRing;

        private int startTick;

        private int ringsFormed;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil formRings = ToilMaker.MakeToil("FormMang");
            formRings.defaultCompleteMode = ToilCompleteMode.Never;
            formRings.initAction = () => startTick = Find.TickManager.TicksGame;
            formRings.tickAction = () =>
            {
                ticksUntilNextRing--;
                if (ticksUntilNextRing > 0) return;   // not time for the next ring yet
                ticksUntilNextRing = TicksPerRing;

                Hediff_ShinMastery mastery = ShinMechanics.GetMastery(pawn);
                if (mastery == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                int targetRings = Mathf.Min(mastery.mangTargetRings, MangMechanics.MaximumRings(pawn));
                int nextRingNumber = MangMechanics.CurrentRings(pawn) + 1;

                bool reachedTarget = nextRingNumber > targetRings;
                bool cannotForm = !MangMechanics.CanFormRing(pawn, out _);

                if (reachedTarget || cannotForm)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                if (MangMechanics.TryFormRing(pawn)) ringsFormed++;
            };

            // Concentration breaks if the pawn is hurt after starting.
            formRings.AddFailCondition(() => pawn.mindState.lastHarmTick > startTick);
            // Concluding sound, whenever the job ends (finished, interrupted or cancelled), if any ring formed.
            AddFinishAction(jobCondition =>
            {
                if (ringsFormed > 0 && pawn.Spawned) ShinDefOf.Mang_RingFormComplete.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            });
            yield return formRings;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksUntilNextRing, "ticksUntilNextRing", TicksPerRing);
            Scribe_Values.Look(ref startTick, "startTick", 0);
            Scribe_Values.Look(ref ringsFormed, "ringsFormed", 0);
        }
    }
}
