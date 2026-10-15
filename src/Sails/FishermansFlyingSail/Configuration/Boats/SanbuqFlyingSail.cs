// Owns Sanbuq's authored Flying Sail support pairs.

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Configuration.Boats
{
    internal static class SanbuqFlyingSail
    {
        internal static readonly FlyingSailDefinition Definition = new FlyingSailDefinition(
            boat: BoatConfiguration.Boats.Sanbuq.Definition,
            supports: Supports()
        );

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 60,
                    foreSections: new[] { 10 },
                    aftSections: new[] { 59, 55 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 67,
                    foreSections: new[] { 11 },
                    aftSections: new[] { 59, 55 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 71,
                    foreSections: new[] { 10 },
                    aftSections: new[] { 70, 69 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 81,
                    foreSections: new[] { 11 },
                    aftSections: new[] { 80, 12 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 82,
                    foreSections: new[] { 10 },
                    aftSections: new[] { 80, 12 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 58,
                    foreSections: new[] { 51 },
                    aftSections: new[] { 14, 11 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 68,
                    foreSections: new[] { 62 },
                    aftSections: new[] { 14, 11 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 79,
                    foreSections: new[] { 51 },
                    aftSections: new[] { 14, 11 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 54,
                    foreSections: new[] { 10 },
                    aftSections: new[] { 55 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 61,
                    foreSections: new[] { 10 },
                    aftSections: new[] { 69 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 66,
                    foreSections: new[] { 11 },
                    aftSections: new[] { 55 }
                ),
            };
    }
}
