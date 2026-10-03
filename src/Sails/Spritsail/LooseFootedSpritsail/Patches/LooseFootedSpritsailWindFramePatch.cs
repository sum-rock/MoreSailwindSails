using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Scopes wind frame patch behavior to the spritsail family.
    [HarmonyPatch(typeof(Sail), "UpdateWindForceOnSail")]
    internal static class LooseFootedSpritsailWindFramePatch
    {
        [HarmonyPrefix]
        private static void Prefix(Sail __instance, out float? __state)
        {
            __state = null;
            var rig = __instance.GetComponent<LooseFootedSpritsailRig>();
            if (!rig)
                return;
            rig.RefreshAerodynamics();
            __state = __instance.currentUnroll;
            // Native force already multiplies by currentUnroll. Substitute the
            // posed area fraction for that calculation only, then restore save/input state.
            __instance.currentUnroll =
                rig.HasActiveSupport && !GameState.currentlyLoading ? rig.ExposedAreaFraction : 0;
        }

        [HarmonyFinalizer]
        private static void Finalizer(Sail __instance, float? __state)
        {
            if (__state.HasValue)
                __instance.currentUnroll = __state.Value;
        }
    }
}
