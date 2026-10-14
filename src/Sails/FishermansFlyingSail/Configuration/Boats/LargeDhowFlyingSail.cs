// Owns LargeDhow's authored Flying Sail support pairs.

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Configuration.Boats
{
    internal static class LargeDhowFlyingSail
    {
        internal static readonly FlyingSailDefinition Definition = new FlyingSailDefinition(
            boat: BoatConfiguration.Boats.LargeDhow.Definition,
            supports: Supports()
        );

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 20,
                    foreSections: new[] { 0 },
                    aftSections: new[] { 3, 2 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 23,
                    foreSections: new[] { 1 },
                    aftSections: new[] { 3, 2 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 26,
                    foreSections: new[] { 0 },
                    aftSections: new[] { 5, 4 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 29,
                    foreSections: new[] { 1 },
                    aftSections: new[] { 5, 4 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 32,
                    foreSections: new[] { 3, 2 },
                    aftSections: new[] { 6 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 34,
                    foreSections: new[] { 5, 4 },
                    aftSections: new[] { 6 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 36,
                    foreSections: new[] { 3, 2 },
                    aftSections: new[] { 7 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 38,
                    foreSections: new[] { 5, 4 },
                    aftSections: new[] { 7 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 40,
                    foreSections: new[] { 3, 2 },
                    aftSections: new[] { 8 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 42,
                    foreSections: new[] { 5, 4 },
                    aftSections: new[] { 8 }
                ),
            };
    }
}
