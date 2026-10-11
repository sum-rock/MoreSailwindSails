using MoreSailwindSails.Utils.Profiling;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Selects only replaced spritsail rope visuals for the diagnostic bypass and attributes counters.
    internal static class HiddenNativeRopeVisuals
    {
        internal static bool ShouldSkip(RopeEffect rope)
        {
            bool bypass = PerformanceProfile.IsBypassed(target: ProfileBypass.HiddenNativeRopes);
            if (!bypass && !PerformanceProfile.Selected(target: ProfileTarget.Ropes))
                return false;

            ProfileFamily family;
            if (rope.GetComponent<LooseFootedSpritsail.LooseFootedSpritsailNativeSheetVisual>())
                family = ProfileFamily.LooseFootedSpritsail;
            else if (rope.GetComponent<BoomedSpritsail.BoomedSpritsailReplacedRopeVisual>())
                family = ProfileFamily.BoomedSpritsail;
            else
                return false;

            PerformanceProfile.Count(
                target: ProfileTarget.Ropes,
                counter: bypass
                    ? ProfileCounter.HiddenNativeVisualSkips
                    : ProfileCounter.HiddenNativeVisualUpdates,
                owner: family
            );
            return bypass;
        }
    }
}
