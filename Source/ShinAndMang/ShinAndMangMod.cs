using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ShinAndMang
{
    public class ShinAndMangMod : Mod
    {
        public static ShinAndMangSettings Settings { get; private set; }

        public ShinAndMangMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ShinAndMangSettings>();
            new Harmony("shapto.shinandmang").PatchAll();
        }

        public override string SettingsCategory() => "Shin (心) and Mang (望)";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("Mastery gain speed: x" + Settings.masteryGainMultiplier.ToString("0.00"));
            Settings.masteryGainMultiplier = listing.Slider(Settings.masteryGainMultiplier, 0.25f, 4f);
            listing.CheckboxLabeled("ShinAndMang_SettingAllowTeaching".Translate(), ref Settings.allowTeachingThroughConversation);

            listing.End();
        }
    }
}
