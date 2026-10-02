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
    public class HediffCompProperties_ShinVariant : HediffCompProperties
    {
        public HediffCompProperties_ShinVariant() => compClass = typeof(ShinVariant);
    }

    /// <summary>
    /// Base class for every Shin (心) variant: one subclass per variant (ShinVariant_Fate, ...).
    /// It lives on the variant's hediff, so it only exists while Shin (心) is active.
    /// Override only the hooks the variant needs.
    /// </summary>
    public class ShinVariant : HediffComp
    {
        // Context
        public Pawn ShinUser => Pawn;
        public Hediff_ShinMastery Mastery => ShinMechanics.GetMastery(Pawn);

        /// <summary>
        /// Mastery from 0 to 1, for effects that grow with every percent.
        /// </summary>
        public float MasteryProgress => Mastery?.Severity ?? 0f;

        /// <summary>
        /// Mastery rank from 0 (novice) to 7 (Sovereign), for effects that step up per rank.
        /// </summary>
        public int MasteryTier => Mastery?.MasteryTier ?? 0;

        // Lifecycle

        /// <summary>
        /// Shin (心) was just manifested.
        /// </summary>
        public virtual void Notify_ShinActivated() { }

        /// <summary>
        /// Shin (心) just ended, for any reason.
        /// </summary>
        public virtual void Notify_ShinEnded() { }

        /// <summary>
        /// Every tick while Shin (心) is active. Throttle with ShinUser.IsHashIntervalTick(interval).
        /// </summary>
        public virtual void TickWhileActive() { }

        public sealed override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            Notify_ShinActivated();
        }
        public sealed override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            Notify_ShinEnded();
        }

        public sealed override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            TickWhileActive();
        }

        // Attacks by the Shin (心) user

        /// <summary>
        /// Before the user's damage lands: change it.
        /// </summary>
        public virtual void ModifyOutgoingDamage(Thing target, ref DamageInfo damageInfo) { }

        /// <summary>
        /// After the user's damage landed: react to it.
        /// </summary>
        public virtual void Notify_DamageDealt(Thing target, DamageInfo damageInfo, DamageWorker.DamageResult damageResult) { }

        // Attacks against the Shin (心) user

        /// <summary>
        /// Before damage to the user lands: change it.
        /// </summary>
        public virtual void ModifyIncomingDamage(ref DamageInfo damageInfo) { }

        /// <summary>
        /// After damage to the user landed: react to it.
        /// </summary>
        public virtual void Notify_DamageTaken(DamageInfo damageInfo, DamageWorker.DamageResult damageResult) { }

        // Stats

        /// <summary>
        /// Every stat this variant can change. Read once at startup from a blank instance,
        /// so it must be a fixed list that doesn't depend on the pawn.
        /// </summary>
        public virtual IEnumerable<StatDef> AffectedStats => Enumerable.Empty<StatDef>();

        // Targeting

        /// <summary>
        /// A short line shown when the player hovers a target with this Shin (心) user selected. Null for nothing.
        /// </summary>
        public virtual string TargetingReadout(Thing target) => null;

        public virtual float GetStatOffset(StatDef stat) => 0f;
        public virtual float GetStatFactor(StatDef stat) => 1f;

        // Mood

        /// <summary>
        /// The user just spent mood on Shin (心) or Mang (望). The amount is what was actually lost, after clamping.
        /// </summary>
        public virtual void Notify_MoodSpent(float moodSpent) { }

        // Tooltip

        /// <summary>What this variant does, shown below the base Shin (心) stats. Can include live values.</summary>
        public virtual string EffectDescription => null;

        private static readonly Color ShinGold = new Color(1f, 0.82f, 0.35f);

        public sealed override string CompTipStringExtra
        {
            get
            {
                string effectDescription = EffectDescription;
                if (effectDescription.NullOrEmpty()) return null;
                return "\n" + "ShinAndMang_VariantEffectsHeader".Translate().Resolve().Colorize(ShinGold) + "\n" + effectDescription;
            }
        }

        // Helpers for subclasses

        /// <summary>
        /// Extra damage from a variant effect (e.g. fire on hit), without re-triggering the variant's own hooks.
        /// </summary>
        protected void DealExtraDamage(Thing target, DamageInfo extraDamage) => ShinVariantHooks.ApplyExtraDamage(target, extraDamage);

        /// <summary>
        /// True if the fighter is currently fighting the opponent: targeting them, or acting on them.
        /// </summary>
        protected static bool IsFighting(Pawn fighter, Thing opponent)
        {
            if (fighter == null || opponent == null) return false;
            if (fighter.mindState?.enemyTarget == opponent) return true;
            return fighter.CurJob != null && fighter.CurJob.targetA.Thing == opponent;
        }

        /// <summary>
        /// True if another pawn of the user's faction, standing or downed, is within the radius.
        /// </summary>
        protected bool IsAllyNearby(float radius)
        {
            if (!ShinUser.Spawned || ShinUser.Faction == null) return false;

            foreach (Pawn ally in ShinUser.Map.mapPawns.SpawnedPawnsInFaction(ShinUser.Faction))
            {
                if (ally != ShinUser && !ally.Dead && ShinUser.Position.InHorDistOf(ally.Position, radius)) return true;
            }
            return false;
        }
    }
}
