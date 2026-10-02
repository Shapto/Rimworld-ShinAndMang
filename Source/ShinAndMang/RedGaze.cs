using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ShinAndMang
{
    public class ShinVariant_RedGaze : ShinVariant
    {
        // Damage
        private const float NoviceDamageBonus = 0.15f;
        private const float SovereignDamageBonus = 0.50f;

        public float DamageBonus => Mathf.Lerp(NoviceDamageBonus, SovereignDamageBonus, MasteryProgress);

        public override void ModifyOutgoingDamage(Thing target, ref DamageInfo damageInfo)
        {
            damageInfo.SetAmount(damageInfo.Amount * (1f + DamageBonus));
        }

        // Targeting

        public override string TargetingReadout(Thing target) => "ShinAndMang_RedGaze_Targeting".Translate(parent.LabelCap, DamageBonus.ToStringPercent());

        // Tooltip

        public override string EffectDescription => "ShinAndMang_RedGaze_Effect".Translate(DamageBonus.ToStringPercent());
    }
}
