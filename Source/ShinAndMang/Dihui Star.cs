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
    public class ShinVariant_DihuiStar : ShinVariant
    {
        // Speed
        private const float NoviceMoveSpeedBonus = 0.08f;
        private const float SovereignMoveSpeedBonus = 0.20f;

        // Focus
        private const float NoviceUnopposedDamageReduction = 0.20f;
        private const float SovereignUnopposedDamageReduction = 0.35f;

        public float MoveSpeedBonus => Mathf.Lerp(NoviceMoveSpeedBonus, SovereignMoveSpeedBonus, MasteryProgress);
        public float UnopposedDamageReduction => Mathf.Lerp(NoviceUnopposedDamageReduction, SovereignUnopposedDamageReduction, MasteryProgress);

        // Incoming

        public override void ModifyIncomingDamage(ref DamageInfo damageInfo)
        {
            if (damageInfo.Instigator == null || IsFighting(ShinUser, damageInfo.Instigator)) return;
            damageInfo.SetAmount(damageInfo.Amount * (1f - UnopposedDamageReduction));
        }

        // Stats

        public override IEnumerable<StatDef> AffectedStats
        {
            get { yield return StatDefOf.MoveSpeed; }
        }

        public override float GetStatFactor(StatDef stat) => stat == StatDefOf.MoveSpeed ? 1f + MoveSpeedBonus : 1f;

        // Tooltip

        public override string EffectDescription => "ShinAndMang_DihuiStar_Effect".Translate(MoveSpeedBonus.ToStringPercent(), UnopposedDamageReduction.ToStringPercent());
    }
}
