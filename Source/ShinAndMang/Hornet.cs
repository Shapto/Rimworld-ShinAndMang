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
    public class ShinVariant_Hornet : ShinVariant
    {
        // Precision
        private const float NoviceAccuracyBonus = 0.10f;
        private const float SovereignAccuracyBonus = 0.25f;
        private const float NoviceDodgeBonus = 0.10f;
        private const float SovereignDodgeBonus = 0.25f;

        // Mood
        private const float MoodForNoEffect = 0.50f;
        private const float MoodForFullEffect = 0.95f;
        private const float NoviceMaximumMoodDamageBonus = 0.20f;
        private const float SovereignMaximumMoodDamageBonus = 0.40f;

        // Retaliation
        private const int RetaliationWindowTicks = 600;
        private const float MoodPerRetaliatingHit = 0.01f;

        private Pawn lastAttacker;
        private int lastAttackedTick = -99999;

        public float AccuracyBonus => Mathf.Lerp(NoviceAccuracyBonus, SovereignAccuracyBonus, MasteryProgress);
        public float DodgeBonus => Mathf.Lerp(NoviceDodgeBonus, SovereignDodgeBonus, MasteryProgress);
        public float MaximumMoodDamageBonus => Mathf.Lerp(NoviceMaximumMoodDamageBonus, SovereignMaximumMoodDamageBonus, MasteryProgress);

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

        public float MoodDamageBonus => MoodProgress * MaximumMoodDamageBonus;

        private bool IsRecentAttacker(Thing target) => target != null && target == lastAttacker && Find.TickManager.TicksGame - lastAttackedTick <= RetaliationWindowTicks;

        // Outgoing

        public override void ModifyOutgoingDamage(Thing target, ref DamageInfo damageInfo)
        {
            if (MoodDamageBonus > 0f) damageInfo.SetAmount(damageInfo.Amount * (1f + MoodDamageBonus));
        }

        public override void Notify_DamageDealt(Thing target, DamageInfo damageInfo, DamageWorker.DamageResult damageResult)
        {
            if (!IsRecentAttacker(target)) return;

            Need_Mood mood = ShinUser.needs?.mood;
            if (mood != null) mood.CurLevel += MoodPerRetaliatingHit;
        }

        // Incoming

        public override void Notify_DamageTaken(DamageInfo damageInfo, DamageWorker.DamageResult damageResult)
        {
            if (!(damageInfo.Instigator is Pawn attacker)) return;
            lastAttacker = attacker;
            lastAttackedTick = Find.TickManager.TicksGame;
        }

        // Stats

        public override IEnumerable<StatDef> AffectedStats
        {
            get
            {
                yield return StatDefOf.MeleeHitChance;
                yield return StatDefOf.ShootingAccuracyPawn;
                yield return StatDefOf.MeleeDodgeChance;
            }
        }

        public override float GetStatFactor(StatDef stat)
        {
            if (stat == StatDefOf.MeleeHitChance || stat == StatDefOf.ShootingAccuracyPawn) return 1f + AccuracyBonus;
            if (stat == StatDefOf.MeleeDodgeChance) return 1f + DodgeBonus;
            return 1f;
        }

        // Targeting

        public override string TargetingReadout(Thing target)
        {
            if (MoodDamageBonus <= 0f) return null;
            return "ShinAndMang_Hornet_Targeting".Translate(parent.LabelCap, MoodDamageBonus.ToStringPercent());
        }

        // Tooltip

        public override string EffectDescription => "ShinAndMang_Hornet_Effect".Translate(AccuracyBonus.ToStringPercent(), DodgeBonus.ToStringPercent(), MoodDamageBonus.ToStringPercent(), MaximumMoodDamageBonus.ToStringPercent(), MoodPerRetaliatingHit.ToStringPercent());

        // Saving

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref lastAttacker, "lastAttacker");
            Scribe_Values.Look(ref lastAttackedTick, "lastAttackedTick", -99999);
        }
    }
}
