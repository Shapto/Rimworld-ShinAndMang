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
    /// <summary>
    /// At startup, turns each Shin (心) variant's two endpoint stages (novice, Sovereign) into
    /// one stage per whole percent of mastery. Any stages a variant adds after the two
    /// endpoints are its own effects and get merged into every generated stage.
    /// </summary>
    [StaticConstructorOnStartup]
    public class ShinStageInterpolator
    {
        public const int GeneratedStageCount = 101;

        private static List<StatModifier> InterpolateStatModifiers(List<StatModifier> noviceModifiers, List<StatModifier> sovereignModifiers, float progress, float neutralValue)
        {
            noviceModifiers = noviceModifiers ?? new List<StatModifier>();
            sovereignModifiers = sovereignModifiers ?? new List<StatModifier>();

            var result = new List<StatModifier>();
            foreach (StatDef stat in noviceModifiers.Select(modifier => modifier.stat).Union(sovereignModifiers.Select(modifier => modifier.stat)))
            {
                float noviceValue = noviceModifiers.FirstOrDefault(modifier => modifier.stat == stat)?.value ?? neutralValue;
                float sovereignValue = sovereignModifiers.FirstOrDefault(modifier => modifier.stat == stat)?.value ?? neutralValue;
                result.Add(new StatModifier { stat = stat, value = Mathf.Lerp(noviceValue, sovereignValue, progress) });
            }
            return result;
        }
        private static List<PawnCapacityModifier> InterpolateCapacityModifiers(List<PawnCapacityModifier> noviceModifiers, List<PawnCapacityModifier> sovereignModifiers, float progress)
        {
            noviceModifiers = noviceModifiers ?? new List<PawnCapacityModifier>();
            sovereignModifiers = sovereignModifiers ?? new List<PawnCapacityModifier>();

            var result = new List<PawnCapacityModifier>();
            foreach (PawnCapacityDef capacity in noviceModifiers.Select(modifier => modifier.capacity).Union(sovereignModifiers.Select(modifier => modifier.capacity)))
            {
                PawnCapacityModifier noviceModifier = noviceModifiers.FirstOrDefault(modifier => modifier.capacity == capacity);
                PawnCapacityModifier sovereignModifier = sovereignModifiers.FirstOrDefault(modifier => modifier.capacity == capacity);
                result.Add(new PawnCapacityModifier
                {
                    capacity = capacity,
                    offset = Mathf.Lerp(noviceModifier?.offset ?? 0f, sovereignModifier?.offset ?? 0f, progress),
                    postFactor = Mathf.Lerp(noviceModifier?.postFactor ?? 1f, sovereignModifier?.postFactor ?? 1f, progress)
                });
            }
            return result;
        }

        // One entry per stat: vanilla only reads the first matching entry, so duplicates would be ignored.
        private static void CombineStatModifiers(List<StatModifier> target, List<StatModifier> additions, Func<float, float, float> combine)
        {
            if (additions == null) return;
            foreach (StatModifier addition in additions)
            {
                StatModifier existingModifier = target.FirstOrDefault(modifier => modifier.stat == addition.stat);
                if (existingModifier == null) target.Add(new StatModifier { stat = addition.stat, value = addition.value });
                else existingModifier.value = combine(existingModifier.value, addition.value);
            }
        }

        /// <summary>Adds a variant's own effects onto a generated stage: offsets add, factors multiply.</summary>
        private static void MergeInto(HediffStage generatedStage, HediffStage variantStage)
        {
            CombineStatModifiers(generatedStage.statOffsets, variantStage.statOffsets, (existingValue, addedValue) => existingValue + addedValue);
            CombineStatModifiers(generatedStage.statFactors, variantStage.statFactors, (existingValue, addedValue) => existingValue * addedValue);

            if (variantStage.capMods != null)
            {
                foreach (PawnCapacityModifier variantModifier in variantStage.capMods)
                {
                    PawnCapacityModifier existingModifier = generatedStage.capMods.FirstOrDefault(modifier => modifier.capacity == variantModifier.capacity);
                    if (existingModifier == null)
                    {
                        generatedStage.capMods.Add(new PawnCapacityModifier { capacity = variantModifier.capacity, offset = variantModifier.offset, postFactor = variantModifier.postFactor });
                    }
                    else
                    {
                        existingModifier.offset += variantModifier.offset;
                        existingModifier.postFactor *= variantModifier.postFactor;
                    }
                }
            }
        }
        private static HediffStage Interpolate(HediffStage noviceStage, HediffStage sovereignStage, float progress)
        {
            return new HediffStage
            {
                minSeverity = progress,
                statOffsets = InterpolateStatModifiers(noviceStage.statOffsets, sovereignStage.statOffsets, progress, neutralValue: 0f),
                statFactors = InterpolateStatModifiers(noviceStage.statFactors, sovereignStage.statFactors, progress, neutralValue: 1f),
                capMods = InterpolateCapacityModifiers(noviceStage.capMods, sovereignStage.capMods, progress)
            };
        }
        static ShinStageInterpolator()
        {
            var variantDefs = DefDatabase<HediffDef>.AllDefs
                .Where(hediffDef => hediffDef.hediffClass != null && typeof(Hediff_Shin).IsAssignableFrom(hediffDef.hediffClass));

            foreach (HediffDef variantDef in variantDefs)
            {
                if (variantDef.stages == null || variantDef.stages.Count < 2)
                {
                    Log.Error($"[Shin and Mang] {variantDef.defName} needs at least two stages (novice and Sovereign).");
                    continue;
                }

                HediffStage noviceStage = variantDef.stages[0];
                HediffStage sovereignStage = variantDef.stages[1];
                List<HediffStage> variantOwnStages = variantDef.stages.Skip(2).ToList();

                var generatedStages = new List<HediffStage>(GeneratedStageCount);
                for (int percent = 0; percent < GeneratedStageCount; percent++)
                {
                    float progress = percent / 100f;
                    HediffStage generatedStage = Interpolate(noviceStage, sovereignStage, progress);
                    foreach (HediffStage variantStage in variantOwnStages)
                        MergeInto(generatedStage, variantStage);
                    generatedStages.Add(generatedStage);
                }

                variantDef.stages = generatedStages;
            }
        }
    }
}
