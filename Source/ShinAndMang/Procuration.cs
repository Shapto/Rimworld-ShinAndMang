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
    public class ShinVariant_Procuration : ShinVariant
    {
        // Speed
        private const float NoviceMoveSpeedBonus = 0.05f;
        private const float SovereignMoveSpeedBonus = 0.15f;
        private const float NoviceAimingSpeedBonus = 0.05f;
        private const float SovereignAimingSpeedBonus = 0.15f;

        // Offense
        private const float NoviceAccuracyBonus = 0.05f;
        private const float SovereignAccuracyBonus = 0.10f;

        // Defense
        private const float NoviceDodgeBonus = 0.10f;
        private const float SovereignDodgeBonus = 0.25f;

        // Wounds
        private const float MissingHealthForFullEffect = 0.5f;
        private const float MaximumWoundDamageBonus = 0.40f;

        public float MoveSpeedBonus => Mathf.Lerp(NoviceMoveSpeedBonus, SovereignMoveSpeedBonus, MasteryProgress);
        public float AimingSpeedBonus => Mathf.Lerp(NoviceAimingSpeedBonus, SovereignAimingSpeedBonus, MasteryProgress);
        public float AccuracyBonus => Mathf.Lerp(NoviceAccuracyBonus, SovereignAccuracyBonus, MasteryProgress);
        public float DodgeBonus => Mathf.Lerp(NoviceDodgeBonus, SovereignDodgeBonus, MasteryProgress);

        public float WoundDamageBonus => Mathf.Clamp01((1f - ShinUser.health.summaryHealth.SummaryHealthPercent) / MissingHealthForFullEffect) * MaximumWoundDamageBonus;

        // Damage

        public override void ModifyOutgoingDamage(Thing target, ref DamageInfo damageInfo)
        {
            if (WoundDamageBonus > 0f) damageInfo.SetAmount(damageInfo.Amount * (1f + WoundDamageBonus));
        }

        // Stats

        public override IEnumerable<StatDef> AffectedStats
        {
            get
            {
                yield return StatDefOf.MoveSpeed;
                yield return StatDefOf.AimingDelayFactor;
                yield return StatDefOf.MeleeHitChance;
                yield return StatDefOf.ShootingAccuracyPawn;
                yield return StatDefOf.MeleeDodgeChance;
            }
        }

        public override float GetStatFactor(StatDef stat)
        {
            if (stat == StatDefOf.MoveSpeed) return 1f + MoveSpeedBonus;
            if (stat == StatDefOf.AimingDelayFactor) return 1f - AimingSpeedBonus;
            if (stat == StatDefOf.MeleeHitChance || stat == StatDefOf.ShootingAccuracyPawn) return 1f + AccuracyBonus;
            if (stat == StatDefOf.MeleeDodgeChance) return 1f + DodgeBonus;
            return 1f;
        }

        // Targeting

        public override string TargetingReadout(Thing target)
        {
            if (WoundDamageBonus <= 0f) return null;
            return "ShinAndMang_Procuration_Targeting".Translate(parent.LabelCap, WoundDamageBonus.ToStringPercent());
        }

        // Tooltip

        public override string EffectDescription => "ShinAndMang_Procuration_Effect".Translate(MoveSpeedBonus.ToStringPercent(), AimingSpeedBonus.ToStringPercent(), AccuracyBonus.ToStringPercent(), DodgeBonus.ToStringPercent(), WoundDamageBonus.ToStringPercent());
    }
}
