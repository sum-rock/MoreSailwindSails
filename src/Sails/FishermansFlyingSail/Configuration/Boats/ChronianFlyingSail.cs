// Owns Chronian's authored Flying Sail support pairs.

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Configuration.Boats
{
    internal static class ChronianFlyingSail
    {
        internal static readonly FlyingSailDefinition Definition = new FlyingSailDefinition(
            boat: BoatConfiguration.Boats.Chronian.Definition,
            supports: new[]
            {
                new MastSupportDefinition(
                    foreSections: new[] { 15, 3, 2 },
                    aftSections: new[] { 16, 5, 4 }
                ),
                new MastSupportDefinition(
                    foreSections: new[] { 16, 5, 4 },
                    aftSections: new[] { 17, 7, 6 }
                ),
            }
        );
    }
}
