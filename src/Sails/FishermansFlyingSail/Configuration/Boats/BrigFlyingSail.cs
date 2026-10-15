// Owns Brig's authored Flying Sail support pairs.

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Configuration.Boats
{
    internal static class BrigFlyingSail
    {
        internal static readonly FlyingSailDefinition Definition = new FlyingSailDefinition(
            boat: BoatConfiguration.Boats.Brig.Definition,
            supports: Supports()
        );

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 15,
                    foreSections: new[] { 3 },
                    aftSections: new[] { 5 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 16,
                    foreSections: new[] { 3 },
                    aftSections: new[] { 4 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 18,
                    foreSections: new[] { 2 },
                    aftSections: new[] { 5 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 20,
                    foreSections: new[] { 2 },
                    aftSections: new[] { 4 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 22,
                    foreSections: new[] { 4 },
                    aftSections: new[] { 7 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 24,
                    foreSections: new[] { 4 },
                    aftSections: new[] { 6 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 61,
                    foreSections: new[] { 3 },
                    aftSections: new[] { 56, 5 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 62,
                    foreSections: new[] { 3 },
                    aftSections: new[] { 58, 4 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 63,
                    foreSections: new[] { 2 },
                    aftSections: new[] { 56, 5 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 64,
                    foreSections: new[] { 2 },
                    aftSections: new[] { 58, 4 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 65,
                    foreSections: new[] { 4 },
                    aftSections: new[] { 59, 7 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 66,
                    foreSections: new[] { 4 },
                    aftSections: new[] { 60, 6 }
                ),
            };
    }
}
