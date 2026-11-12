using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ShinAndMang
{
    [HarmonyPatch(typeof(Need), nameof(Need.CurLevel), MethodType.Setter)]
    public static class Patch_Need_CurLevel
    {
        private static readonly AccessTools.FieldRef<Need, Pawn> needPawnField = AccessTools.FieldRefAccess<Need, Pawn>("pawn");

        public static void Prefix(Need __instance, ref float value)
        {
            if (!float.IsNaN(value)) return;
            Pawn needOwner = needPawnField(__instance);
            Log.ErrorOnce($"[Shin and Mang] Blocked NaN {__instance.def.defName} on {needOwner?.LabelShort}. Source:\n{Environment.StackTrace}", __instance.GetHashCode() ^ 0x4E614E);
            float currentLevel = __instance.CurLevel;
            value = float.IsNaN(currentLevel) ? __instance.MaxLevel * 0.5f : currentLevel;
        }
    }
}
