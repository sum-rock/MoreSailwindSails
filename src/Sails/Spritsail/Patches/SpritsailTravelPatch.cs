using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Scopes travel patch behavior to the spritsail family.
    [HarmonyPatch(typeof(JibAngleMaster), "Update")]
    internal static class SpritsailTravelPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Sail ___sail, out bool __state)
        {
            __state = SpritsailCategory.IsSpritsail(sail: ___sail);
            if (!__state)
                return;
            // Native Update caches these for both sheet controllers. Saved
            // limits and later mast refreshes may still contain the old range.
            ___sail.minAngle = SpritsailTravel.Clamp(angle: ___sail.minAngle);
            ___sail.maxAngle = SpritsailTravel.Clamp(angle: ___sail.maxAngle);
        }

        [HarmonyPostfix]
        private static void Postfix(JibAngleMaster __instance, Sail ___sail, bool __state)
        {
            if (!__state)
                return;
            var hinge = __instance.sailHinge;
            var limits = hinge.limits;
            float min = limits.min,
                max = limits.max;
            // ApplySway runs inside native Update after the sheet limits are
            // assigned. Bound its final result, including stale sheet values,
            // without changing the sail pose or resetting Cloth.
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
