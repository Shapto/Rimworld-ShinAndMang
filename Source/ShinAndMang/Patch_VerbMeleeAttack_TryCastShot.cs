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
    /// <summary>
    /// A melee swing consumes the attacker's Mang (望) rings, empowering every hit of that swing.
    /// </summary>
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    public static class Patch_VerbMeleeAttack_TryCastShot
    {
        public static void Prefix(Verb_MeleeAttack __instance, out bool __state)
        {
            __state = false;
            Pawn attacker = __instance.CasterPawn;
            if (attacker == null) return;
            DamageDef damageDef = __instance.GetDamageDef();
            if (damageDef == null || !damageDef.harmsHealth) return;
            int ringCount = MangMechanics.ConsumeRings(attacker);
            if (ringCount == 0) return;
            MangMechanics.BeginStrike(attacker, ringCount);
            __state = true;
        }

        public static void Postfix(Verb_MeleeAttack __instance, bool __state)
        {
            if (__state) MangMechanics.EndStrike(__instance.CasterPawn);
        }
    }
}
