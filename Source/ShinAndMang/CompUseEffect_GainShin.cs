using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ShinAndMang
{
    public class CompUseEffect_GainShin : CompUseEffect
    {
        /// <summary>
        /// Decides whether the right-click option is available, and the reason shown if not.
        /// </summary>
        public override AcceptanceReport CanBeUsedBy(Pawn pawn)
        {
            if (ShinMechanics.GetMastery(pawn) != null) return "ShinAndMang_AlreadyKnowsShin".Translate().ToString();
            if (!ShinMechanics.CanLearnShin(pawn)) return "ShinAndMang_CannotLearnShin".Translate().ToString();
            return true;
        }

        /// <summary>
        /// Runs when the pawn finishes using the item.
        /// </summary>
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            usedBy.health.AddHediff(ShinDefOf.Shin_Mastery);
        }
    }
}
