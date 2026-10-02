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
    public class ShinVariant_Sidekick : ShinVariant
    {
        // Armor
        private const float DamageTakenForFullArmor = 30f;
        private const int ArmorDecayIntervalTicks = 60;
        private const float ArmorDecayPerInterval = 0.1f;      // full armor fades in about 10 seconds
        private const float NoviceMaximumSharpArmor = 0.20f;
        private const float SovereignMaximumSharpArmor = 0.50f;
        private const float BluntArmorShare = 0.6f;            // blunt armor is 60% of sharp armor
        private const float HeatArmorShare = 0.3f;

        // Damage
        private const float NoviceDamageBonus = 0.05f;
        private const float SovereignDamageBonus = 0.15f;
        private const float NoviceGuardianDamageBonus = 0.15f;
        private const float SovereignGuardianDamageBonus = 0.30f;
        private const float AllyRadius = 10f;
        private const int RecentAttackTicks = 600;

        // Mood
        private const float NoviceMoodPerProtectingHit = 0.005f;
        private const float SovereignMoodPerProtectingHit = 0.015f;

        private float armorBuildUp;

        public float MaximumSharpArmor => Mathf.Lerp(NoviceMaximumSharpArmor, SovereignMaximumSharpArmor, MasteryProgress);
        public float SharpArmorBonus => armorBuildUp * MaximumSharpArmor;
        public float DamageBonus => Mathf.Lerp(NoviceDamageBonus, SovereignDamageBonus, MasteryProgress);
        public float GuardianDamageBonus => Mathf.Lerp(NoviceGuardianDamageBonus, SovereignGuardianDamageBonus, MasteryProgress);
        public float MoodPerProtectingHit => Mathf.Lerp(NoviceMoodPerProtectingHit, SovereignMoodPerProtectingHit, MasteryProgress);

        /// <summary>
        /// True if the target recently attacked another member of the user's faction who is near the user.
        /// </summary>
        private bool AttackedNearbyAlly(Thing target)
        {
            if (!(target is Pawn targetPawn) || ShinUser.Faction == null) return false;
            if (Find.TickManager.TicksGame - targetPawn.LastAttackTargetTick > RecentAttackTicks) return false;

            Pawn victim = targetPawn.LastAttackedTarget.Thing as Pawn;
            return victim != null && victim != ShinUser && victim.Faction == ShinUser.Faction && victim.Spawned && ShinUser.Position.InHorDistOf(victim.Position, AllyRadius);
        }

        /// <summary>
        /// Used both for the actual hit and for the targeting readout, so they always match.
        /// </summary>
        public float DamageMultiplierAgainst(Thing target) => 1f + DamageBonus + (AttackedNearbyAlly(target) ? GuardianDamageBonus : 0f);

        // Lifecycle

        public override void TickWhileActive()
        {
            if (armorBuildUp <= 0f || !ShinUser.IsHashIntervalTick(ArmorDecayIntervalTicks)) return;
            armorBuildUp = Mathf.Max(0f, armorBuildUp - ArmorDecayPerInterval);
        }

        // Outgoing

        public override void ModifyOutgoingDamage(Thing target, ref DamageInfo damageInfo)
        {
            damageInfo.SetAmount(damageInfo.Amount * DamageMultiplierAgainst(target));
        }

        // Incoming

        public override void Notify_DamageTaken(DamageInfo damageInfo, DamageWorker.DamageResult damageResult)
        {
            if (ShinUser.Dead) return;

            armorBuildUp = Mathf.Min(1f, armorBuildUp + damageResult.totalDamageDealt / DamageTakenForFullArmor);

            Need_Mood mood = ShinUser.needs?.mood;
            if (mood != null && IsAllyNearby(AllyRadius)) mood.CurLevel += MoodPerProtectingHit;
        }

        // Stats

        public override IEnumerable<StatDef> AffectedStats
        {
            get
            {
                yield return StatDefOf.ArmorRating_Sharp;
                yield return StatDefOf.ArmorRating_Blunt;
                yield return StatDefOf.ArmorRating_Heat;
            }
        }

        public override float GetStatOffset(StatDef stat)
        {
            if (stat == StatDefOf.ArmorRating_Sharp) return SharpArmorBonus;
            if (stat == StatDefOf.ArmorRating_Blunt) return SharpArmorBonus * BluntArmorShare;
            if (stat == StatDefOf.ArmorRating_Heat) return SharpArmorBonus * HeatArmorShare;
            return 0f;
        }

        // Targeting

        public override string TargetingReadout(Thing target) => "ShinAndMang_Sidekick_Targeting".Translate(parent.LabelCap, (DamageMultiplierAgainst(target) - 1f).ToStringPercent());

        // Label

        public override string CompLabelInBracketsExtra => armorBuildUp > 0f ? "ShinAndMang_Sidekick_Armor".Translate(armorBuildUp.ToStringPercent()).Resolve() : null;

        // Tooltip

        public override string EffectDescription => "ShinAndMang_Sidekick_Effect".Translate(SharpArmorBonus.ToStringPercent(), MaximumSharpArmor.ToStringPercent(), DamageBonus.ToStringPercent(), GuardianDamageBonus.ToStringPercent(), MoodPerProtectingHit.ToStringPercent());

        // Saving

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref armorBuildUp, "armorBuildUp", 0f);
        }
    }
}
