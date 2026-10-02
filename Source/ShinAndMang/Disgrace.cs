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
    public class ShinVariant_Disgrace : ShinVariant
    {
        // Scorch
        private const float NoviceFireDamageBonus = 0.20f;
        private const float SovereignFireDamageBonus = 0.50f;

        // Shake
        private const int NoviceStaggerExtensionTicks = 30;
        private const int SovereignStaggerExtensionTicks = 90;

        // Speed
        private const float NoviceMoveSpeedBonus = 0.05f;
        private const float SovereignMoveSpeedBonus = 0.15f;

        // Shame
        private const float DamageTakenForFullShame = 30f;
        private const int ShameDecayIntervalTicks = 60;
        private const float ShameDecayPerInterval = 0.1f;   // full shame fades in about 10 seconds
        private const float MaximumShameDamageReduction = 0.20f;
        private const float MaximumShameMoveSpeedBonus = 0.15f;

        private float shame;

        /// <summary>
        /// Shame from 0 to 1. Builds when struck, fades over time.
        /// </summary>
        public float Shame => shame;

        public float FireDamageBonus => Mathf.Lerp(NoviceFireDamageBonus, SovereignFireDamageBonus, MasteryProgress);
        public int StaggerExtensionTicks => Mathf.RoundToInt(Mathf.Lerp(NoviceStaggerExtensionTicks, SovereignStaggerExtensionTicks, MasteryProgress));
        public float BaseMoveSpeedBonus => Mathf.Lerp(NoviceMoveSpeedBonus, SovereignMoveSpeedBonus, MasteryProgress);
        public float ShameDamageReduction => shame * MaximumShameDamageReduction;
        public float ShameMoveSpeedBonus => shame * MaximumShameMoveSpeedBonus;

        private static bool IsFireDamage(DamageDef damageDef) => damageDef == DamageDefOf.Flame || damageDef == DamageDefOf.Burn;

        // Lifecycle

        public override void TickWhileActive()
        {
            if (shame <= 0f || !ShinUser.IsHashIntervalTick(ShameDecayIntervalTicks)) return;
            shame = Mathf.Max(0f, shame - ShameDecayPerInterval);
        }

        // Outgoing

        public override void ModifyOutgoingDamage(Thing target, ref DamageInfo damageInfo)
        {
            if (IsFireDamage(damageInfo.Def)) damageInfo.SetAmount(damageInfo.Amount * (1f + FireDamageBonus));
        }

        public override void Notify_DamageDealt(Thing target, DamageInfo damageInfo, DamageWorker.DamageResult damageResult)
        {
            if (!(target is Pawn targetPawn) || targetPawn.Dead) return;

            // Only lengthens staggers that already happened; never creates one.
            if (targetPawn.stances?.stagger != null && targetPawn.stances.stagger.Staggered)
            {
                targetPawn.stances.stagger.StaggerFor(StaggerExtensionTicks);
            }
        }

        // Incoming

        public override void ModifyIncomingDamage(ref DamageInfo damageInfo)
        {
            if (ShameDamageReduction > 0f) damageInfo.SetAmount(damageInfo.Amount * (1f - ShameDamageReduction));
        }

        public override void Notify_DamageTaken(DamageInfo damageInfo, DamageWorker.DamageResult damageResult)
        {
            if (ShinUser.Dead) return;
            shame = Mathf.Min(1f, shame + damageResult.totalDamageDealt / DamageTakenForFullShame);
        }

        // Stats

        public override IEnumerable<StatDef> AffectedStats
        {
            get { yield return StatDefOf.MoveSpeed; }
        }

        public override float GetStatFactor(StatDef stat) => stat == StatDefOf.MoveSpeed ? 1f + BaseMoveSpeedBonus + ShameMoveSpeedBonus : 1f;

        // Targeting

        public override string TargetingReadout(Thing target)
        {
            DamageDef weaponDamage = ShinUser.equipment?.PrimaryEq?.PrimaryVerb?.GetDamageDef();
            if (!IsFireDamage(weaponDamage)) return null;
            return "ShinAndMang_Disgrace_Targeting".Translate(parent.LabelCap, FireDamageBonus.ToStringPercent());
        }

        // Label

        public override string CompLabelInBracketsExtra => "ShinAndMang_Disgrace_Shame".Translate(shame.ToStringPercent());

        // Tooltip

        public override string EffectDescription => "ShinAndMang_Disgrace_Effect".Translate(FireDamageBonus.ToStringPercent(), (StaggerExtensionTicks / 60f).ToString("0.0"), shame.ToStringPercent(), ShameDamageReduction.ToStringPercent(), (BaseMoveSpeedBonus + ShameMoveSpeedBonus).ToStringPercent());

        // Saving

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref shame, "shame", 0f);
        }
    }
}
