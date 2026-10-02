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

        ///How likely each starting mastery is for pawns generated with Shin (心). x = mastery, y = relative weight.
        public SimpleCurve spawnMasteryWeights;

        public float combatGainPerHour = 0.01f;         // base mastery per in-game hour of real fighting
        public int recentCombatTicks = 600;             // "fighting" = attacked or was hit within this many ticks
        public float woundIntensityWeight = 1.5f;       // added intensity at 100% missing health
        public float moodIntensityWeight = 1.0f;        // added intensity at the most extreme mood
        public float allyDownedIntensity = 1.0f;        // added intensity while a nearby ally is downed
        public float allyDownedRadius = 15f;
        public float maximumIntensity = 4f;
        public float tipsSelectionWeight = 0.050f;   // how often it's picked; vanilla chitchat is 1, deep talk is 0.075
        public float tipsMinimumGap = 0.05f;       // teacher must be at least this much ahead
        public float tipsGainPerGap = 0.002f;       // gain = gap * this, before the curve
        public float teachSelectionWeight = 0.01f;           // much rarer than tips
        public float teachChanceAtFullMastery = 0.3f;        // success chance scales with the teacher's mastery
        public float meditationGainPerHour = 0.0005f;   //0.05% per hour of meditation
        public float aiActivationIntensity = 2f; // AI pawns manifest Shin (心) only once the fight is this intense
    }
}
