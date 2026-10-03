using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.Patches
{
    // Suppresses replaced reef and topping-lift visuals while retaining the native boom sheet.
    [HarmonyPatch(typeof(RopeEffect), "LateUpdate")]
    internal static class BoomedSpritsailReplacedRopeVisualPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            RopeEffect __instance,
            LineRenderer ___lineRenderer,
            ClothRope ___clothRope
        )
        {
            var visual = __instance.GetComponent<BoomedSpritsailReplacedRopeVisual>();
            if (visual)
                visual.Suppress(line: ___lineRenderer, cloth: ___clothRope);
        }
    }
}
