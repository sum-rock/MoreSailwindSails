// Owns Jong's authored Flying Sail support pairs.

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Configuration.Boats
{
    internal static class JongFlyingSail
    {
        internal static readonly FlyingSailDefinition Definition = new FlyingSailDefinition(
            boat: BoatConfiguration.Boats.Jong.Definition,
            supports: Supports()
        );

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 10,
                    foreSections: new[] { 1 },
                    aftSections: new[] { 2 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 12,
                    foreSections: new[] { 2 },
                    aftSections: new[] { 3 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 55,
                    foreSections: new[] { 51 },
                    aftSections: new[] { 52 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 58,
                    foreSections: new[] { 51 },
                    aftSections: new[] { 3 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 73,
                    foreSections: new[] { 1 },
                    aftSections: new[] { 52 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 71,
                    foreSections: new[] { 52 },
                    aftSections: new[] { 4 }
                ),
            };
    }
}
