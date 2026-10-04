using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ShinAndMang
{
    /// <summary>
    /// Tunable Mang (望) values, attached to the Mang_Rings def in XML.
    /// </summary>
    public class MangSettings : DefModExtension
    {
        // Cost
        public SimpleCurve baseCostByMastery;          // mood cost of the first ring, by mastery

        // Damage
        public float damageMultiplierPerRing = 1.35f;  // used when an attack consumes the rings

        // Rail shot
        public float railDamageFalloff = 0.75f;   // damage kept after each thing pierced
        public int maximumWallsPierced = 3;

        // Lifetime
        public int fadeTicks = 1500;                   // 25 seconds

        // Reliability
        public float newestRingChanceAtUnlock = 0.5f;  // success chance right at the newest ring's threshold

        // Mastery
        public float masteryGainPerRingInCombat = 0.002f;
        public float outOfCombatMasteryFactor = 0.2f;
    }
}
