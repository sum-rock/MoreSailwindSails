using HarmonyLib;
using ShipyardExpansion;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Allows uniform sizing while disabling SE's shipyard rotation and flipping for all spritsails.
    [HarmonyPatch(typeof(SailScaler), "Awake")]
    internal static class SpritsailScalerPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SailScaler __instance, Sail ___sail)
        {
            if (!SpritsailCategory.IsSpritsail(sail: ___sail))
                return;
            // SE hides both rotate buttons and SetAngle returns immediately for a null target.
            __instance.rotatablePart = null;
            __instance.scaleType = ScaleType.Uniform;
            __instance.flippable = false;
        }
    }
}
