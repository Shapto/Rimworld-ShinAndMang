using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ShinAndMang
{
    /// <summary>
    /// Melee Animation draws animated weapons itself. After each animation frame, records every animated weapon's
    /// final transform, so Mang (望) rings follow the animation. Skipped entirely if Melee Animation isn't installed.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_MeleeAnimation_Draw
    {
        private static FieldInfo snapshotsField;
        private static FieldInfo rootTransformField;
        private static FieldInfo mirrorHorizontalField;
        private static FieldInfo mirrorVerticalField;
        private static MethodInfo getOverrideMethod;

        private static FieldInfo snapshotPartField;
        private static FieldInfo snapshotWorldMatrixField;
        private static FieldInfo snapshotFlipXField;
        private static FieldInfo snapshotFlipYField;

        private static FieldInfo overrideWeaponField;
        private static FieldInfo overrideFlipXField;
        private static FieldInfo overrideFlipYField;

        /// <summary>
        /// Finds Melee Animation's classes by name. Returning false makes Harmony skip this patch.
        /// </summary>
        private static bool Prepare()
        {
            Type rendererType = AccessTools.TypeByName("AM.AnimRenderer");
            Type snapshotType = AccessTools.TypeByName("AnimPartSnapshot");
            Type partType = AccessTools.TypeByName("AnimPartData");
            Type overrideType = AccessTools.TypeByName("AnimPartOverrideData");
            if (rendererType == null || snapshotType == null || partType == null || overrideType == null) return false;

            snapshotsField = AccessTools.Field(rendererType, "snapshots");
            rootTransformField = AccessTools.Field(rendererType, "RootTransform");
            mirrorHorizontalField = AccessTools.Field(rendererType, "MirrorHorizontal");
            mirrorVerticalField = AccessTools.Field(rendererType, "MirrorVertical");
            getOverrideMethod = AccessTools.Method(rendererType, "GetOverride", new[] { partType });

            snapshotPartField = AccessTools.Field(snapshotType, "Part");
            snapshotWorldMatrixField = AccessTools.Field(snapshotType, "WorldMatrix");
            snapshotFlipXField = AccessTools.Field(snapshotType, "FlipX");
            snapshotFlipYField = AccessTools.Field(snapshotType, "FlipY");

            overrideWeaponField = AccessTools.Field(overrideType, "Weapon");
            overrideFlipXField = AccessTools.Field(overrideType, "FlipX");
            overrideFlipYField = AccessTools.Field(overrideType, "FlipY");

            return snapshotsField != null && getOverrideMethod != null && snapshotWorldMatrixField != null && overrideWeaponField != null;
        }

        private static MethodBase TargetMethod() => AccessTools.Method("AM.AnimRenderer:Draw");

        public static void Postfix(object __instance)
        {
            if (!(snapshotsField.GetValue(__instance) is Array snapshots)) return;

            Matrix4x4 rootTransform = (Matrix4x4)rootTransformField.GetValue(__instance);
            bool mirrorHorizontal = (bool)mirrorHorizontalField.GetValue(__instance);
            bool mirrorVertical = (bool)mirrorVerticalField.GetValue(__instance);

            foreach (object snapshot in snapshots)
            {
                object part = snapshotPartField.GetValue(snapshot);
                if (part == null) continue;

                object overrideData = getOverrideMethod.Invoke(__instance, new[] { part });
                if (overrideData == null || !(overrideWeaponField.GetValue(overrideData) is Thing weapon)) continue;

                Pawn holder = (weapon.ParentHolder as Pawn_EquipmentTracker)?.pawn;
                if (holder == null) continue;

                // The same flip logic Melee Animation uses when drawing the part.
                bool flipHorizontally = (bool)snapshotFlipXField.GetValue(snapshot) ^ (bool)overrideFlipXField.GetValue(overrideData) ^ mirrorHorizontal;
                bool flipVertically = (bool)snapshotFlipYField.GetValue(snapshot) ^ (bool)overrideFlipYField.GetValue(overrideData) ^ mirrorVertical;

                Matrix4x4 worldMatrix = rootTransform * (Matrix4x4)snapshotWorldMatrixField.GetValue(snapshot);
                WeaponDrawRecord.RecordMatrix(holder, weapon.def, worldMatrix, flipHorizontally, flipVertically);
            }
        }
    }
}
