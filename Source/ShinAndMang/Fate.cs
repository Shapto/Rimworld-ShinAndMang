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
    /// <summary>
    /// Fate: refusing to fall. Grows more dangerous as death nears, shrugs off pain,
    /// and overpowers those whose will is weaker.
    /// </summary>
    public class ShinVariant_Fate : ShinVariant
    {
        // Wounds
        private const float MissingHealthForFullEffect = 0.5f;      // full effect once half of health is gone
        private const float MaximumWoundDamageBonus = 0.30f;
        private const float MaximumPainShockThresholdBonus = 0.25f;

        // Will
        private const float DamageBonusPerMoodDifference = 0.5f;    // +1% damage per 2% mood difference
        private const float MaximumWillDamageBonus = 0.30f;

        /// <summary>
        /// 0 when unhurt, 1 once half of health is gone.
        /// </summary>
        private float WoundProgress => Mathf.Clamp01((1f - ShinUser.health.summaryHealth.SummaryHealthPercent) / MissingHealthForFullEffect);

        public float WoundDamageBonus => WoundProgress * MaximumWoundDamageBonus;
        public float PainShockThresholdBonus => WoundProgress * MaximumPainShockThresholdBonus;

        public float WillDamageBonusAgainst(Thing target)
        {
            Need_Mood userMood = ShinUser.needs?.mood;
            Need_Mood targetMood = (target as Pawn)?.needs?.mood;
            if (userMood == null || targetMood == null) return 0f;

            float moodDifference = userMood.CurLevel - targetMood.CurLevel;
            if (moodDifference <= 0f) return 0f;
            return Mathf.Min(MaximumWillDamageBonus, moodDifference * DamageBonusPerMoodDifference);
        }

        /// <summary>
        /// Used both for the actual hit and for the targeting readout, so they always match.
        /// </summary>
        public float DamageMultiplierAgainst(Thing target) => 1f + WoundDamageBonus + WillDamageBonusAgainst(target);

        // Damage

        public override void ModifyOutgoingDamage(Thing target, ref DamageInfo damageInfo)
        {
            float damageMultiplier = DamageMultiplierAgainst(target);
            if (damageMultiplier != 1f) damageInfo.SetAmount(damageInfo.Amount * damageMultiplier);
        }

        // Stats

        public override IEnumerable<StatDef> AffectedStats
        {
            get 
            {
                yield return StatDefOf.PainShockThreshold; 
            }
        }

        public override float GetStatOffset(StatDef stat) => stat == StatDefOf.PainShockThreshold ? PainShockThresholdBonus : 0f;

        // Targeting

        public override string TargetingReadout(Thing target)
        {
            float damageBonus = DamageMultiplierAgainst(target) - 1f;
            if (damageBonus <= 0f) return null;
            return "ShinAndMang_Fate_Targeting".Translate(parent.LabelCap, damageBonus.ToStringPercent());
        }

        // Tooltip

        public override string EffectDescription => "ShinAndMang_Fate_Effect".Translate(
            WoundDamageBonus.ToStringPercent(),
            PainShockThresholdBonus.ToStringPercent(),
            MaximumWillDamageBonus.ToStringPercent());
    }
}

