using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ShinAndMang
{
    /// <summary>
    /// The thought applies while Shin (心) is active.
    /// </summary>
    public class ThoughtWorker_ShinSuppression : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn) => ShinMechanics.IsShinActive(pawn);
    }
    /// <summary>While Shin (心) is active, cancels out the pawn's single worst negative thought group.</summary>
    public class Thought_ShinSuppression : Thought_Situational
    {
        public override float MoodOffset() => ShinMechanics.GetActiveShin(pawn)?.SuppressedMoodOffset ?? 0f;
    }

}
