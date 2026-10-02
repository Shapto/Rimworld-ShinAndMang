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
    public class ShinVariant_Fate : ShinVariant
    {
        private const float MissingHealthPerDamageUp = 0.15f;
        private const int MaximumDamageUp = 5;
        private const float DamageBonusPerDamageUp = 0.05f;

        private const float MissingHealthPerAttackPowerUp = 0.40f;
        private const int MaximumAttackPowerUp = 2;
        private const float AccuracyBonusPerAttackPowerUp = 0.05f;

        private const float MoodDifferencePerPercentDamage = 0.02f;
        private const float MaximumMoodDamageBonus = 0.30f;

        // Small margin so values like 0.30 / 0.15 don't round down to 1.999.
        private const float RoundingMargin = 0.0001f;

        private float MissingHealth => 1f - ShinUser.health.summaryHealth.SummaryHealthPercent;

        public int DamageUpStacks
            => Mathf.Min(MaximumDamageUp, Mathf.FloorToInt(MissingHealth / MissingHealthPerDamageUp + RoundingMargin));

        public int AttackPowerUpStacks
            => Mathf.Min(MaximumAttackPowerUp, Mathf.FloorToInt(MissingHealth / MissingHealthPerAttackPowerUp + RoundingMargin));

        private float MoodDamageBonusAgainst(Thing target)
        {
            Need_Mood userMood = ShinUser.needs?.mood;
            Need_Mood targetMood = (target as Pawn)?.needs?.mood;
            if (userMood == null || targetMood == null) return 0f;

            float moodDifference = userMood.CurLevel - targetMood.CurLevel;
            if (moodDifference <= 0f) return 0f;

            float damageBonus = Mathf.Floor(moodDifference / MoodDifferencePerPercentDamage + RoundingMargin) * 0.01f;
            return Mathf.Min(MaximumMoodDamageBonus, damageBonus);
        }

        // Damage

        public override void ModifyOutgoingDamage(Thing target, ref DamageInfo damageInfo)
        {
            float damageMultiplier = 1f + DamageUpStacks * DamageBonusPerDamageUp + MoodDamageBonusAgainst(target);
            if (damageMultiplier != 1f) damageInfo.SetAmount(damageInfo.Amount * damageMultiplier);
        }

        // Accuracy

        public override IEnumerable<StatDef> AffectedStats
        {
            get
            {
                yield return StatDefOf.MeleeHitChance;
                yield return StatDefOf.ShootingAccuracyPawn;
            }
        }

        public override float GetStatFactor(StatDef stat)
        {
            if (stat == StatDefOf.MeleeHitChance || stat == StatDefOf.ShootingAccuracyPawn)
                return 1f + AttackPowerUpStacks * AccuracyBonusPerAttackPowerUp;
            return 1f;
        }

        // Tooltip

        public override string EffectDescription => "ShinAndMang_Fate_Effect".Translate(
            DamageUpStacks,
            (DamageUpStacks * DamageBonusPerDamageUp).ToStringPercent(),
            AttackPowerUpStacks,
            (AttackPowerUpStacks * AccuracyBonusPerAttackPowerUp).ToStringPercent());
    }
}

