using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;

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
    /// Which group of mod sounds a volume slider controls.
    /// </summary>
    public enum ShinAndMangSoundCategory
    {
        ShinActivation,
        ShinLoop,
        MangRings
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
        public float masterVolume = 1f;
        public float shinActivationVolume = 1f;
        public float shinLoopVolume = 1f;
        public float mangRingsVolume = 1f;
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
            Scribe_Values.Look(ref masterVolume, "masterVolume", 1f);
            Scribe_Values.Look(ref shinActivationVolume, "shinActivationVolume", 1f);
            Scribe_Values.Look(ref shinLoopVolume, "shinLoopVolume", 1f);
            Scribe_Values.Look(ref mangRingsVolume, "mangRingsVolume", 1f);
        }

        /// <summary>
        /// Returns master volume multiplied by the category's own volume.
        /// </summary>
        public float GetEffectiveVolume(ShinAndMangSoundCategory soundCategory)
        {
            float categoryVolume = 1f;
            switch (soundCategory)
            {
                case ShinAndMangSoundCategory.ShinActivation:
                    categoryVolume = shinActivationVolume;
                    break;
                case ShinAndMangSoundCategory.ShinLoop:
                    categoryVolume = shinLoopVolume;
                    break;
                case ShinAndMangSoundCategory.MangRings:
                    categoryVolume = mangRingsVolume;
                    break;
            }
            return masterVolume * categoryVolume;
        }

        /// <summary>
        /// Volume slider entry point.
        /// </summary>
        public static class ShinAndMangSoundPlayer
        {
            public static void PlayOneShot(SoundDef soundDef, TargetInfo target, ShinAndMangSoundCategory soundCategory)
            {
                if (soundDef == null) return;
                float volume = ShinAndMangMod.Settings.GetEffectiveVolume(soundCategory);
                if (volume <= 0f) return;
                SoundInfo soundInfo = SoundInfo.InMap(target);
                soundInfo.volumeFactor = volume;
                soundDef.PlayOneShot(soundInfo);
            }

            public static Sustainer SpawnSustainer(SoundDef soundDef, TargetInfo target, ShinAndMangSoundCategory soundCategory)
            {
                if (soundDef == null) return null;
                SoundInfo soundInfo = SoundInfo.InMap(target, MaintenanceType.PerTick);
                soundInfo.volumeFactor = ShinAndMangMod.Settings.GetEffectiveVolume(soundCategory);
                return soundDef.TrySpawnSustainer(soundInfo);
            }
        }
    }
}
