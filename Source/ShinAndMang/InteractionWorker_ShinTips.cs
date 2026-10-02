using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ShinAndMang
{
    public class InteractionWorker_ShinTips : InteractionWorker
    {
        /// <summary>
        /// How likely this interaction is picked. 0 means never.
        /// </summary>
        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            Hediff_ShinMastery teacherMastery = ShinMechanics.GetMastery(initiator);
            Hediff_ShinMastery studentMastery = ShinMechanics.GetMastery(recipient);
            ShinMasteryGainExtension gainSettings = ShinDefOf.Shin_Mastery.GetModExtension<ShinMasteryGainExtension>();
            if(gainSettings == null) return 0f;
            if (teacherMastery != null && ShinMechanics.CanLearnShin(recipient)) return gainSettings.teachSelectionWeight;
            if (teacherMastery == null || studentMastery == null) return 0f;
            float gap = teacherMastery.Severity - studentMastery.Severity;
            if (gap < gainSettings.tipsMinimumGap) return 0f;
            return gainSettings.tipsSelectionWeight;
        }

        /// <summary>
        /// Runs when the interaction happens.
        /// </summary>
        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks,
            out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            // These must always be set; null means "no letter".
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;

            Hediff_ShinMastery teacherMastery = ShinMechanics.GetMastery(initiator);
            Hediff_ShinMastery studentMastery = ShinMechanics.GetMastery(recipient);
            ShinMasteryGainExtension gainSettings = ShinDefOf.Shin_Mastery.GetModExtension<ShinMasteryGainExtension>();
            if (gainSettings == null) return;
            if (teacherMastery != null && ShinMechanics.CanLearnShin(recipient))
            {
                float chance = gainSettings.teachChanceAtFullMastery * teacherMastery.Severity;
                if (Rand.Chance(chance))
                {
                    recipient.health.AddHediff(ShinDefOf.Shin_Mastery);
                }
                return;
            }
            if (teacherMastery == null || studentMastery == null) return;
            float gap = teacherMastery.Severity - studentMastery.Severity;
            if (gap <= 0) return;
            float gain = Math.Min(gap * gainSettings.tipsGainPerGap, gap);

            ShinMechanics.GainMastery(recipient, gain);
        }
    }
}
