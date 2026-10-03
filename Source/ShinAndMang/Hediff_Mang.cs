using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ShinAndMang
{
    /// <summary>
    /// The Mang (望) rings a pawn currently holds. Severity is the ring count.
    /// </summary>
    public class Hediff_Mang : HediffWithComps
    {
        private int ticksUntilFade;

        public int RingCount => Mathf.RoundToInt(Severity);
        public MangSettings Settings => def.GetModExtension<MangSettings>();

        // Rings

        /// <summary>
        /// Called whenever a ring is added; restarts the fade timer.
        /// </summary>
        public void Notify_RingAdded()
        {
            ticksUntilFade = Settings?.fadeTicks ?? 1500;
        }

        // Lifecycle

        public override void PostTick()
        {
            base.PostTick();
            ticksUntilFade -= 1;
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            Notify_RingAdded();
        }

        // Removed when unused for too long, or when the holder is downed.
        public override bool ShouldRemove => /* TODO */ false;

        // Label
        public override string LabelInBrackets => $"{RingCount} rings, {ticksUntilFade / 60f:0}s";

        // Saving

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksUntilFade, "ticksUntilFade", 1500);
        }
    }
}
