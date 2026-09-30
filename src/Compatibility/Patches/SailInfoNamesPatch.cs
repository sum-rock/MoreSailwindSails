using System;
using System.Reflection;
using HarmonyLib;
using MoreSailwindSails.Sails.FishermansFlyingSail;
using MoreSailwindSails.Sails.FishermansStaysail;

namespace MoreSailwindSails.Compatibility.Patches
{
    // Supplies custom sail family names while leaving SailInfo's HUD and settings in control.
    internal static class SailInfoNamesPatch
    {
        internal static MethodInfo FindTarget(Type type)
        {
            if (type == null)
                return null;
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            if (type.GetField(name: "sailComponent", bindingAttr: flags)?.FieldType != typeof(Sail))
                return null;
            return Array.Find(
                array: type.GetMethods(bindingAttr: flags | BindingFlags.DeclaredOnly),
                match: method =>
                    method.Name == "SailName"
                    && !method.IsGenericMethod
                    && method.ReturnType == typeof(string)
                    && method.GetParameters().Length == 0
            );
        }

        internal static void Install(Harmony harmony)
        {
            var type = AccessTools.TypeByName(name: "SailInfo.WinchInfoSail");
            if (type == null)
                return;
            var target = FindTarget(type: type);
            if (target == null)
            {
                Plugin.Log.LogWarning(
                    "SailInfo's naming API changed; Fisherman's sail name integration skipped."
                );
                return;
            }
            harmony.Patch(
                original: target,
                prefix: new HarmonyMethod(
                    methodType: typeof(SailInfoNamesPatch),
                    methodName: nameof(Prefix)
                )
            );
        }

        private static bool Prefix(Sail ___sailComponent, ref string __result)
        {
            if (!___sailComponent || string.IsNullOrEmpty(value: ___sailComponent.sailName))
                return true;
            // WinchHUD gates names with SailInfo's setting and adds the halyard suffix.
            // Bypass positional-name caching and omit mark and size from the family label.
            if (___sailComponent.GetComponent<FishermansFlyingSailRig>())
                __result = FishermansFlyingSail.DisplayName;
            else if (___sailComponent.GetComponent<FishermansStaysailRig>())
                __result = "Fisherman's Staysail";
            else
                return true;
            return false;
        }
    }
}
