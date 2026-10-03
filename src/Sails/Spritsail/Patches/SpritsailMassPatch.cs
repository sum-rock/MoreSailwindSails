using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Gives spritsails the gaff/junk boat-mass contribution independently of propulsion tuning.
    [HarmonyPatch(typeof(BoatMass), "GetSailMass")]
    internal static class SpritsailMassPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail sail, ref float __result)
        {
            if (!SpritsailCategory.IsSpritsail(sail: sail))
                return true;
            __result = SpritsailRules.Mass(realPower: sail.GetRealSailPower());
            return false;
        }
    }
}
