using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.Patches
{
    // Bounds the ordinary gaff controller's final hinge limits without changing its single-sheet input.
    [HarmonyPatch(typeof(RopeControllerSailAngle), "LateUpdate")]
    internal static class BoomedSpritsailTravelPatch
    {
        [HarmonyPostfix]
        private static void Postfix(RopeControllerSailAngle __instance, Sail ___sail)
        {
            if (!___sail || !___sail.GetComponent<BoomedSpritsailRig>() || !__instance.sailHinge)
                return;
            var hinge = __instance.sailHinge;
            var limits = hinge.limits;
            float min = limits.min,
                max = limits.max;
            SpritsailTravel.ConstrainHinge(
                min: ref min,
                max: ref max,
                allowedMin: ___sail.minAngle,
                allowedMax: ___sail.maxAngle
            );
            limits.min = min;
            limits.max = max;
            hinge.limits = limits;
        }
    }
}
