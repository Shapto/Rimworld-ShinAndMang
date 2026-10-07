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
            listing.CheckboxLabeled("Everybody has Shin (心)", ref Settings.everybodyHasShin, "Every eligible adult is generated knowing Shin (心).");
            if (!Settings.everybodyHasShin)
            {
                listing.Label("Shin (心) spawn chance: " + Settings.shinSpawnChance.ToStringPercent());
                Settings.shinSpawnChance = listing.Slider(Settings.shinSpawnChance, 0f, 0.1f);
            }
            listing.GapLine();
            GUI.color = ColorLibrary.RedReadable;
            listing.Label("ShinAndMang_SettingRestartRequired".Translate());
            GUI.color = Color.white;
            ArmorSlider(listing, "ShinAndMang_SettingNoviceSharpArmor".Translate(), ref Settings.noviceSharpArmor);
            ArmorSlider(listing, "ShinAndMang_SettingSovereignSharpArmor".Translate(), ref Settings.sovereignSharpArmor);
            ArmorSlider(listing, "ShinAndMang_SettingNoviceBluntArmor".Translate(), ref Settings.noviceBluntArmor);
            ArmorSlider(listing, "ShinAndMang_SettingSovereignBluntArmor".Translate(), ref Settings.sovereignBluntArmor);
            ArmorSlider(listing, "ShinAndMang_SettingNoviceHeatArmor".Translate(), ref Settings.noviceHeatArmor);
            ArmorSlider(listing, "ShinAndMang_SettingSovereignHeatArmor".Translate(), ref Settings.sovereignHeatArmor);
            listing.Label("ShinAndMang_SettingMaximumMangRings".Translate(Settings.maximumMangRings));
            Settings.maximumMangRings = Mathf.RoundToInt(listing.Slider(Settings.maximumMangRings, 1f, 500f));
            if (Settings.maximumMangRings > 7)
            {
                listing.Label("ShinAndMang_SettingExtraRingCost".Translate());
                if (listing.RadioButton("ShinAndMang_ExtraRingCost_KeepDoubling".Translate(), Settings.ringsPastSeventhCost == ExtraMangRingCost.KeepDoubling)) Settings.ringsPastSeventhCost = ExtraMangRingCost.KeepDoubling;
                if (listing.RadioButton("ShinAndMang_ExtraRingCost_SameAsSeventh".Translate(), Settings.ringsPastSeventhCost == ExtraMangRingCost.SameAsSeventh)) Settings.ringsPastSeventhCost = ExtraMangRingCost.SameAsSeventh;
                if (listing.RadioButton("ShinAndMang_ExtraRingCost_Free".Translate(), Settings.ringsPastSeventhCost == ExtraMangRingCost.Free)) Settings.ringsPastSeventhCost = ExtraMangRingCost.Free;
            }
            listing.End();
        }

        /// <summary>
        /// One labeled armor slider from 0% to 200%, rounded to whole percents.
        /// </summary>
        private static void ArmorSlider(Listing_Standard listing, string label, ref float value)
        {
            listing.Label(label + ": " + value.ToStringPercent());
            value = Mathf.Round(listing.Slider(value, 0f, 2f) * 100f) / 100f;
        }
    }
}
