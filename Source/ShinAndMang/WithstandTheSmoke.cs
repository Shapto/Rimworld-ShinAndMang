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
    public class ShinVariant_WithstandTheSmoke : ShinVariant
    {
        // Speed
        private const float NoviceMoveSpeedBonus = 0.08f;
        private const float SovereignMoveSpeedBonus = 0.15f;

        // Unopposed
        private const float NoviceUnopposedDamageBonus = 0.10f;
        private const float SovereignUnopposedDamageBonus = 0.25f;

        // Mood
        private const float MoodForNoEffect = 0.50f;
        private const float MoodForFullEffect = 0.95f;
        private const float MaximumMoodMoveSpeedBonus = 0.15f;
        private const float MaximumMoodAccuracyBonus = 0.10f;
        private const float MaximumMoodDamageBonus = 0.15f;

        public float BaseMoveSpeedBonus => Mathf.Lerp(NoviceMoveSpeedBonus, SovereignMoveSpeedBonus, MasteryProgress);
        public float UnopposedDamageBonus => Mathf.Lerp(NoviceUnopposedDamageBonus, SovereignUnopposedDamageBonus, MasteryProgress);

        /// <summary>
        /// 0 at 50% mood or lower, 1 at 95% mood or higher.
        /// </summary>
        public float MoodProgress
        {
            get
            {
                Need_Mood mood = ShinUser.needs?.mood;
                return mood == null ? 0f : Mathf.Clamp01((mood.CurLevel - MoodForNoEffect) / (MoodForFullEffect - MoodForNoEffect));
            }
        }

        public float MoodMoveSpeedBonus => MoodProgress * MaximumMoodMoveSpeedBonus;
        public float MoodAccuracyBonus => MoodProgress * MaximumMoodAccuracyBonus;
        public float MoodDamageBonus => MoodProgress * MaximumMoodDamageBonus;

        /// <summary>
        /// Used both for the actual hit and for the targeting readout, so they always match.
        /// </summary>
        public float DamageMultiplierAgainst(Thing target)
        {
            float unopposedBonus = target is Pawn targetPawn && !IsFighting(targetPawn, ShinUser) ? UnopposedDamageBonus : 0f;
            return 1f + MoodDamageBonus + unopposedBonus;
        }

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
                yield return StatDefOf.MoveSpeed;
                yield return StatDefOf.MeleeHitChance;
                yield return StatDefOf.ShootingAccuracyPawn;
            }
        }

        public override float GetStatFactor(StatDef stat)
        {
            if (stat == StatDefOf.MoveSpeed) return 1f + BaseMoveSpeedBonus + MoodMoveSpeedBonus;
            if (stat == StatDefOf.MeleeHitChance || stat == StatDefOf.ShootingAccuracyPawn) return 1f + MoodAccuracyBonus;
            return 1f;
        }

        // Targeting

        public override string TargetingReadout(Thing target)
        {
            float damageBonus = DamageMultiplierAgainst(target) - 1f;
            if (damageBonus <= 0f) return null;
            return "ShinAndMang_WithstandTheSmoke_Targeting".Translate(parent.LabelCap, damageBonus.ToStringPercent());
        }

        // Tooltip

        public override string EffectDescription => "ShinAndMang_WithstandTheSmoke_Effect".Translate((BaseMoveSpeedBonus + MoodMoveSpeedBonus).ToStringPercent(), MoodAccuracyBonus.ToStringPercent(), MoodDamageBonus.ToStringPercent(), UnopposedDamageBonus.ToStringPercent());
    }
}
