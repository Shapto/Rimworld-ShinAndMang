using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ShinAndMang
{
    /// <summary>
    /// Rarely, newly generated pawns already know Shin (心).
    /// </summary>
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
    public static class Patch_PawnGenerator_GeneratePawn
    {
        public static void Postfix(Pawn __result)
        {
            if (__result == null || !ShinMechanics.CanLearnShin(__result)) return;
            float spawnChance = ShinAndMangMod.Settings.everybodyHasShin ? 1f : ShinAndMangMod.Settings.shinSpawnChance;
            if (!Rand.Chance(spawnChance)) return;
            ShinMasteryGainExtension gainSettings = ShinDefOf.Shin_Mastery.GetModExtension<ShinMasteryGainExtension>();
            float rolledMastery = gainSettings?.spawnMasteryWeights != null ? Rand.ByCurve(gainSettings.spawnMasteryWeights) : 0f;
            Hediff mastery = __result.health.AddHediff(ShinDefOf.Shin_Mastery);
            mastery.Severity = rolledMastery;
        }
    }
}
