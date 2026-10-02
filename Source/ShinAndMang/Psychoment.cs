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
    public class ShinVariant_Psychoment : ShinVariant
    {
        // Defense
        private const float NoviceDodgeBonus = 0.10f;
        private const float SovereignDodgeBonus = 0.30f;
        private const float NoviceDamageReduction = 0.05f;
        private const float SovereignDamageReduction = 0.15f;

        // Protection
        private const float ProtectionPerMoodSpent = 3f;          // 10% mood spent gives 30% protection
        private const float MaximumProtection = 0.50f;
        private const int ProtectionDecayIntervalTicks = 60;
        private const float ProtectionDecayPerInterval = 0.03f;   // 30% protection fades in about 10 seconds

        // Mind
        private const float NovicePsychicSensitivityFactor = 0.80f;
        private const float SovereignPsychicSensitivityFactor = 0.50f;

        private float protection;

        public float DodgeBonus => Mathf.Lerp(NoviceDodgeBonus, SovereignDodgeBonus, MasteryProgress);
        public float DamageReduction => Mathf.Lerp(NoviceDamageReduction, SovereignDamageReduction, MasteryProgress);
        public float PsychicSensitivityFactor => Mathf.Lerp(NovicePsychicSensitivityFactor, SovereignPsychicSensitivityFactor, MasteryProgress);

        /// <summary>
        /// Total share of damage removed: the constant reduction plus current protection, combined multiplicatively.
        /// </summary>
        public float TotalDamageReduction => 1f - (1f - DamageReduction) * (1f - protection);

        // Mood

        public override void Notify_MoodSpent(float moodSpent)
        {
            protection = Mathf.Min(MaximumProtection, protection + moodSpent * ProtectionPerMoodSpent);
        }

        // Lifecycle

        public override void TickWhileActive()
        {
            if (protection <= 0f || !ShinUser.IsHashIntervalTick(ProtectionDecayIntervalTicks)) return;
            protection = Mathf.Max(0f, protection - ProtectionDecayPerInterval);
        }

        // Incoming

        public override void ModifyIncomingDamage(ref DamageInfo damageInfo)
        {
            damageInfo.SetAmount(damageInfo.Amount * (1f - TotalDamageReduction));
        }

        // Stats

        public override IEnumerable<StatDef> AffectedStats
        {
            get
            {
                yield return StatDefOf.MeleeDodgeChance;
                yield return StatDefOf.PsychicSensitivity;
            }
        }

        public override float GetStatFactor(StatDef stat)
        {
            if (stat == StatDefOf.MeleeDodgeChance) return 1f + DodgeBonus;
            if (stat == StatDefOf.PsychicSensitivity) return PsychicSensitivityFactor;
            return 1f;
        }

        // Label

        public override string CompLabelInBracketsExtra => protection > 0f ? "ShinAndMang_Psychoment_Protection".Translate(protection.ToStringPercent()).Resolve() : null;

        // Tooltip

        public override string EffectDescription => "ShinAndMang_Psychoment_Effect".Translate(DodgeBonus.ToStringPercent(), DamageReduction.ToStringPercent(), protection.ToStringPercent(), PsychicSensitivityFactor.ToStringPercent());

        // Saving

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref protection, "protection", 0f);
        }
    }
}
