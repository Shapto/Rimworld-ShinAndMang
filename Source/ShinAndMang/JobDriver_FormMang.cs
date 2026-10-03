using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ShinAndMang
{
    /// <summary>
    /// The pawn stands still and forms Mang (望) rings one after another, until the target is reached.
    /// Being hurt breaks the concentration.
    /// </summary>
    public class JobDriver_FormMang : JobDriver
    {
        private const int TicksPerRing = 45;   // about one ring every 0.75 seconds

        private int ticksUntilNextRing = TicksPerRing;
        private int startTick;

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

                MangMechanics.TryFormRing(pawn);
            };

            // Concentration breaks if the pawn is hurt after starting.
            formRings.AddFailCondition(() => pawn.mindState.lastHarmTick > startTick);

            yield return formRings;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // TODO: save ticksUntilNextRing and startTick
        }
    }
}
