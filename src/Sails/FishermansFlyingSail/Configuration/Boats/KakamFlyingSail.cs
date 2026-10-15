// Owns Kakam's authored Flying Sail support pairs.

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Configuration.Boats
{
    internal static class KakamFlyingSail
    {
        internal static readonly FlyingSailDefinition Definition = new FlyingSailDefinition(
            boat: BoatConfiguration.Boats.Kakam.Definition,
            supports: new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 62,
                    foreSections: new[] { 5 },
                    aftSections: new[] { 7 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 70,
                    foreSections: new[] { 6 },
                    aftSections: new[] { 7 }
                ),
            }
        );
    }
}
