using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ShinAndMang
{
    public class ShinVariant_Radiance : ShinVariant
    {
        // Mood
        private const float NoviceMoodRestore = 0.15f;
        private const float SovereignMoodRestore = 0.30f;

        // Flare
        private const float FlareHealthThreshold = 0.60f;
        private const int FlareDurationTicks = 600;          // 10 seconds
        private const float FlareDamageReduction = 0.60f;
        private static readonly Color RadianceGold = new Color(1f, 0.82f, 0.35f);

        private bool hasFlared;
        private int flareTicksRemaining;

        public float MoodRestore => Mathf.Lerp(NoviceMoodRestore, SovereignMoodRestore, MasteryProgress);
        public bool IsFlaring => flareTicksRemaining > 0;

        // Lifecycle

        public override void Notify_ShinActivated()
        {
            Need_Mood mood = ShinUser.needs?.mood;
            if (mood != null) mood.CurLevel += MoodRestore;
        }

        public override void TickWhileActive()
        {
            if (flareTicksRemaining > 0) flareTicksRemaining--;
        }

        // Incoming

        public override void ModifyIncomingDamage(ref DamageInfo damageInfo)
        {
            if (IsFlaring) damageInfo.SetAmount(damageInfo.Amount * (1f - FlareDamageReduction));
        }

        public override void Notify_DamageTaken(DamageInfo damageInfo, DamageWorker.DamageResult damageResult)
        {
            if (hasFlared || ShinUser.Dead) return;
            if (ShinUser.health.summaryHealth.SummaryHealthPercent >= FlareHealthThreshold) return;

            hasFlared = true;
            flareTicksRemaining = FlareDurationTicks;

            if (ShinUser.Spawned)
            {
                MoteMaker.ThrowText(ShinUser.DrawPos, ShinUser.Map, "ShinAndMang_Radiance_FlareText".Translate(), RadianceGold, 3f);
                Messages.Message("ShinAndMang_Radiance_FlareMessage".Translate(ShinUser.LabelShort), ShinUser, MessageTypeDefOf.NeutralEvent, historical: false);
            }
        }

        // Label

        public override string CompLabelInBracketsExtra => IsFlaring ? "ShinAndMang_Radiance_Flaring".Translate((flareTicksRemaining / 60f).ToString("0")).Resolve() : null;

        // Tooltip

        public override string EffectDescription => "ShinAndMang_Radiance_Effect".Translate(MoodRestore.ToStringPercent(), FlareHealthThreshold.ToStringPercent(), FlareDamageReduction.ToStringPercent(), (FlareDurationTicks / 60f).ToString("0"), hasFlared ? "ShinAndMang_Radiance_Spent".Translate() : "ShinAndMang_Radiance_Ready".Translate());

        // Saving

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref hasFlared, "hasFlared", false);
            Scribe_Values.Look(ref flareTicksRemaining, "flareTicksRemaining", 0);
        }
    }
}
