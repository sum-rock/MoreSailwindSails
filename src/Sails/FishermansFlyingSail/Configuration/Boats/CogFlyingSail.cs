// Owns Cog's authored Flying Sail support pairs.

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Configuration.Boats
{
    internal static class CogFlyingSail
    {
        internal static readonly FlyingSailDefinition Definition = new FlyingSailDefinition(
            boat: BoatConfiguration.Boats.Cog.Definition,
            supports: Supports()
        );

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 51,
                    foreSections: new[] { 8 },
                    aftSections: new[] { 5 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 58,
                    foreSections: new[] { 5 },
                    aftSections: new[] { 57 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 65,
                    foreSections: new[] { 8 },
                    aftSections: new[] { 6 }
                ),
            };
    }
}
