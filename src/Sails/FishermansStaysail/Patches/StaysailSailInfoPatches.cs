// Groups Staysail patches for optional SailInfo angle reporting.
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail.Patches
{
    // Optional integration: no assembly reference or replacement HUD. SailInfo
    // keeps its settings, efficiency calculations and all other sail readouts.
    internal static class FishermansStaysailSailInfoPatch
    {
        internal static MethodInfo FindTarget(Type type)
        {
            if (type == null)
                return null;
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var method = type.GetMethod(
                "SailDegree",
                flags | BindingFlags.DeclaredOnly,
                null,
                Type.EmptyTypes,
                null
            );
            var field = type.GetField("sailComponent", flags);
            return
                method != null
                && !method.IsStatic
                && method.ReturnType == typeof(int)
                && field?.FieldType == typeof(Sail)
                ? method
                : null;
        }

        internal static void Install(Harmony harmony)
        {
            var type = AccessTools.TypeByName("SailInfo.WinchInfoSail");
            if (type == null)
                return;
            var target = FindTarget(type);
            if (target == null)
            {
                Plugin.Log.LogWarning(
                    "SailInfo's angle API changed; Fisherman's staysail angle integration skipped."
                );
                return;
            }
            harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(FishermansStaysailSailInfoPatch), nameof(Prefix))
            );
        }

        private static bool Prefix(Sail ___sailComponent, ref int __result)
        {
            var rig = ___sailComponent
                ? ___sailComponent.GetComponent<FishermansStaysailRig>()
                : null;
            if (!rig || !rig.TrySheetAngle(out float angle))
                return true;
            __result = Mathf.RoundToInt(angle);
            return false;
        }
    }
}
