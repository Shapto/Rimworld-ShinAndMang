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
        private static bool isCalculating;
        private static readonly List<Thought> moodThoughtGroups = new List<Thought>();

        private int cachedTick = -1;
        private float cachedMoodOffset;

        // Mood

        public override float MoodOffset()
        {
            UpdateCache();
            return cachedMoodOffset;
        }

        // Calculation
        private void UpdateCache()
        {
            int currentTick = Find.TickManager.TicksGame;
            if (cachedTick == currentTick || isCalculating) return;

            isCalculating = true;
            try
            {
                cachedTick = currentTick;
                cachedMoodOffset = 0f;

                ThoughtHandler thoughtHandler = pawn.needs?.mood?.thoughts;
                if (thoughtHandler == null) return;

                moodThoughtGroups.Clear();
                thoughtHandler.GetDistinctMoodThoughtGroups(moodThoughtGroups);

                float worstMoodOffset = 0f;
                foreach (Thought thoughtGroup in moodThoughtGroups)
                {
                    // Skip ourselves: asking our own offset here would loop forever.
                    if (thoughtGroup.def == def) continue;

                    float groupMoodOffset = thoughtHandler.MoodOffsetOfGroup(thoughtGroup);
                    if (groupMoodOffset < worstMoodOffset)
                    {
                        worstMoodOffset = groupMoodOffset;
                    }
                }

                cachedMoodOffset = -worstMoodOffset;
            }
            finally
            {
                isCalculating = false;
            }
        }
    }

}
