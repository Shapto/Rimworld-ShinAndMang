using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;
using static ShinAndMang.ShinAndMangSettings;

namespace ShinAndMang
{
    public class Hediff_Shin : HediffWithComps
    {
        // Letting the health tracker remove it is safer than removing itself mid-tick.
        public override bool ShouldRemove => !ShinMechanics.IsInCombat(pawn);
        private const int GainIntervalTicks = 250;
        private const int SparkIntervalTicks = 30;
        private Sustainer loopSustainer;
        private float loopSustainerVolume;
        private Mote auraMote;
        public override void PostTick()
        {
            base.PostTick();
            if (pawn.Spawned)
            {
                if (auraMote == null || auraMote.Destroyed) auraMote = MoteMaker.MakeAttachedOverlay(pawn, ShinDefOf.Mote_ShinAura, Vector3.zero);
                auraMote.Maintain();
                if (pawn.IsHashIntervalTick(SparkIntervalTicks)) ShinVisuals.ThrowSpark(pawn);
                float loopVolume = ShinAndMangMod.Settings.GetEffectiveVolume(ShinAndMangSoundCategory.ShinLoop);
                if (loopSustainer != null && !loopSustainer.Ended && loopSustainerVolume != loopVolume) loopSustainer.End();
                if (loopSustainer == null || loopSustainer.Ended)
                {
                    loopSustainer = ShinAndMangSoundPlayer.SpawnSustainer(ShinDefOf.Shin_Loop, pawn, ShinAndMangSoundCategory.ShinLoop);
                    loopSustainerVolume = loopVolume;
                }
                loopSustainer?.Maintain();
            }

            if (!pawn.Spawned) return;
            if (!pawn.IsHashIntervalTick(GainIntervalTicks)) return;

            ShinMasteryGainExtension gainSettings = ShinDefOf.Shin_Mastery.GetModExtension<ShinMasteryGainExtension>();
            if (gainSettings != null && ShinMechanics.IsActivelyFighting(pawn, gainSettings.recentCombatTicks))
            {
                float gain = gainSettings.combatGainPerHour * (GainIntervalTicks / 2500f) * ShinMechanics.CombatIntensity(pawn, gainSettings);
                ShinMechanics.GainMastery(pawn, gain);
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (pawn.Spawned) ShinAndMangSoundPlayer.PlayOneShot(ShinDefOf.Shin_Add, pawn, ShinAndMangSoundCategory.ShinActivation);
        }

        public override int CurStageIndex
        {
            get
            {
                Hediff_ShinMastery mastery = ShinMechanics.GetMastery(pawn);
                if (mastery == null || def.stages.NullOrEmpty()) return 0;
                int masteryPercent = Mathf.FloorToInt(mastery.Severity * 100f + 0.0001f);
                return Mathf.Clamp(masteryPercent, 0, def.stages.Count - 1);
            }
        }
    }
}
