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
    /// Ends Shin (心) when its user dies. Dead pawns stop ticking, so Hediff_Shin can't remove itself.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill
    {
        public static void Postfix(Pawn __instance)
        {
            if (!__instance.Dead) return;

            ShinMechanics.EndShin(__instance);
            MangMechanics.ConsumeRings(__instance);
        }
    }
}
