using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.Patches
{
    // Scopes sheet visual patch behavior to the spritsail family.
    [HarmonyPatch(typeof(RopeEffect), "LateUpdate")]
    internal static class LooseFootedSpritsailSheetVisualPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            RopeEffect __instance,
            LineRenderer ___lineRenderer,
            ClothRope ___clothRope
        )
        {
            var visual = __instance.GetComponent<LooseFootedSpritsailNativeSheetVisual>();
            if (visual)
                visual.Suppress(line: ___lineRenderer, cloth: ___clothRope);
        }
    }
}
