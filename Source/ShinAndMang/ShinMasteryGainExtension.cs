using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ShinAndMang
{
    /// Tunable settings for mastery gain, attached to Shin_Mastery in XML.
    public class ShinMasteryGainExtension : DefModExtension
    {
        /// x = current mastery (0..1), y = how much of each gain actually applies.
        public SimpleCurve gainCurve;

        public float combatGainPerHour = 0.01f;         // base mastery per in-game hour of real fighting
        public int recentCombatTicks = 600;             // "fighting" = attacked or was hit within this many ticks
        public float woundIntensityWeight = 1.5f;       // added intensity at 100% missing health
        public float moodIntensityWeight = 1.0f;        // added intensity at the most extreme mood
        public float allyDownedIntensity = 1.0f;        // added intensity while a nearby ally is downed
        public float allyDownedRadius = 15f;
        public float maximumIntensity = 4f;
    }
}
