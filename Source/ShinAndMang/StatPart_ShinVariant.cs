using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ShinAndMang
{
    /// <summary>
    /// Applies the active Shin (心) variant's stat bonuses to its user.
    /// </summary>
    public class StatPart_ShinVariant : StatPart
    {
        public override void TransformValue(StatRequest request, ref float value)
        {
            if (!(request.Thing is Pawn pawn)) return;

            foreach (ShinVariant variant in ShinVariantHooks.ActiveVariants(pawn))
            {
                value += variant.GetStatOffset(parentStat);
                value *= variant.GetStatFactor(parentStat);
            }
        }

        public override string ExplanationPart(StatRequest request)
        {
            if (!(request.Thing is Pawn pawn)) return null;

            var explanation = new StringBuilder();
            foreach (ShinVariant variant in ShinVariantHooks.ActiveVariants(pawn))
            {
                float offset = variant.GetStatOffset(parentStat);
                float factor = variant.GetStatFactor(parentStat);

                if (offset != 0f)
                    explanation.AppendLine(variant.parent.LabelCap + ": " + parentStat.ValueToString(offset, ToStringNumberSense.Offset));
                if (factor != 1f)
                    explanation.AppendLine(variant.parent.LabelCap + ": x" + factor.ToStringPercent());
            }

            return explanation.Length > 0 ? explanation.ToString().TrimEndNewlines() : null;
        }

        /// <summary>At startup, attaches StatPart_ShinVariant to every stat any variant declares in AffectedStats.</summary>
        [StaticConstructorOnStartup]
        public static class ShinVariantStatSetup
        {
            static ShinVariantStatSetup()
            {
                var statsToHook = new HashSet<StatDef>();

                foreach (HediffDef variantDef in DefDatabase<HediffDef>.AllDefs.Where(ShinMechanics.IsShinVariantDef))
                {
                    if (variantDef.comps == null) continue;

                    foreach (HediffCompProperties compProperties in variantDef.comps)
                    {
                        if (compProperties.compClass == null || !typeof(ShinVariant).IsAssignableFrom(compProperties.compClass)) continue;

                        // A blank instance, only used to read the fixed list of affected stats.
                        var blankVariant = (ShinVariant)Activator.CreateInstance(compProperties.compClass);
                        foreach (StatDef stat in blankVariant.AffectedStats)
                        {
                            if (stat != null) statsToHook.Add(stat);
                        }
                    }
                }

                foreach (StatDef stat in statsToHook)
                {
                    if (stat.parts == null) stat.parts = new List<StatPart>();
                    stat.parts.Add(new StatPart_ShinVariant { parentStat = stat });
                }
            }
        }
    }
}
