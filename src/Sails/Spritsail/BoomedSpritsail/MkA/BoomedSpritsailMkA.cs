using System;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.MkA
{
    // Preserves the original Mk.A identity and 105-degree, full-span head.
    internal static class BoomedSpritsailMkA
    {
        internal const int PrefabIndex = 406;
        internal const string DisplayName = "Boomed Spritsail Mk.A";
        internal const float ThroatAngleDegrees = 105;
        internal const float PeakReach = 1;
        internal static readonly float PeakRise =
            PeakReach * (float)Math.Tan((ThroatAngleDegrees - 90) * Math.PI / 180);
        internal static readonly BoomedSpritsailDefinition Definition =
            new BoomedSpritsailDefinition(
                prefabIndex: PrefabIndex,
                displayName: DisplayName,
                peakReach: PeakReach,
                peakRise: PeakRise
            );
    }
}
