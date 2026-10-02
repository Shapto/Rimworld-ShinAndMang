using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ShinAndMang
{
    /// <summary>
    /// Values the player can change in the Mod Settings menu.
    /// </summary>
    public class ShinAndMangSettings : ModSettings
    {
        public float masteryGainMultiplier = 1f;
        public bool allowTeachingThroughConversation = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref masteryGainMultiplier, "masteryGainMultiplier", 1f);
            Scribe_Values.Look(ref allowTeachingThroughConversation, "allowTeachingThroughConversation", true);
            // TODO: the same for allowTeachingThroughConversation, default true
        }
    }
}
