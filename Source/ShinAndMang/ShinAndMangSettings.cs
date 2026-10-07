using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ShinAndMang
{
    /// <summary>
    /// What Mang (望) rings past the seventh cost.
    /// </summary>
    public enum ExtraMangRingCost
    {
        KeepDoubling,
        SameAsSeventh,
        Free
    }

    /// <summary>
    /// Values the player can change in the Mod Settings menu.
    /// </summary>
    public class ShinAndMangSettings : ModSettings
    {
        public float masteryGainMultiplier = 1f;
        public bool allowTeachingThroughConversation = true;
        public float shinSpawnChance = 0.01f; // 1% of eligible pawns
        public bool everybodyHasShin = false;
        public float noviceSharpArmor = 0.30f;
        public float sovereignSharpArmor = 1.20f;
        public float noviceBluntArmor = 0.15f;
        public float sovereignBluntArmor = 0.80f;
        public float noviceHeatArmor = 0.05f;
        public float sovereignHeatArmor = 0.30f;
        public int maximumMangRings = 7;
        public ExtraMangRingCost ringsPastSeventhCost = ExtraMangRingCost.SameAsSeventh;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref masteryGainMultiplier, "masteryGainMultiplier", 1f);
            Scribe_Values.Look(ref allowTeachingThroughConversation, "allowTeachingThroughConversation", true);
            Scribe_Values.Look(ref shinSpawnChance, "shinSpawnChance", 0.01f);
            Scribe_Values.Look(ref everybodyHasShin, "everybodyHasShin", false);
            Scribe_Values.Look(ref noviceSharpArmor, "noviceSharpArmor", 0.30f);
            Scribe_Values.Look(ref sovereignSharpArmor, "sovereignSharpArmor", 1.20f);
            Scribe_Values.Look(ref noviceBluntArmor, "noviceBluntArmor", 0.15f);
            Scribe_Values.Look(ref sovereignBluntArmor, "sovereignBluntArmor", 0.80f);
            Scribe_Values.Look(ref noviceHeatArmor, "noviceHeatArmor", 0.05f);
            Scribe_Values.Look(ref sovereignHeatArmor, "sovereignHeatArmor", 0.30f);
            Scribe_Values.Look(ref maximumMangRings, "maximumMangRings", 7);
            Scribe_Values.Look(ref ringsPastSeventhCost, "ringsPastSeventhCost", ExtraMangRingCost.SameAsSeventh);
        }
    }
}
