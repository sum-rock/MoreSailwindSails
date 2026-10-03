using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.Patches
{
    // Scopes sheet visual patch behavior to the spritsail family.
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
