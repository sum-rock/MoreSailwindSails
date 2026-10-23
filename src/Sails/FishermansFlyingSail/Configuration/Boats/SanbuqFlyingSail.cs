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
                    foreSections: new[] { 10 },
                    aftSections: new[] { 59, 55 }
                ),
                new MastSupportDefinition(
                    foreSections: new[] { 11 },
                    aftSections: new[] { 59, 55 }
                ),
                new MastSupportDefinition(
                    foreSections: new[] { 10 },
                    aftSections: new[] { 70, 69 }
                ),
                new MastSupportDefinition(
                    foreSections: new[] { 11 },
                    aftSections: new[] { 80, 12 }
                ),
                new MastSupportDefinition(
                    foreSections: new[] { 10 },
                    aftSections: new[] { 80, 12 }
                ),
                new MastSupportDefinition(
                    foreSections: new[] { 51 },
                    aftSections: new[] { 14, 11 }
                ),
                new MastSupportDefinition(
                    foreSections: new[] { 62 },
                    aftSections: new[] { 14, 11 }
                ),
                new MastSupportDefinition(
                    foreSections: new[] { 51 },
                    aftSections: new[] { 14, 11 }
                ),
                new MastSupportDefinition(foreSections: new[] { 10 }, aftSections: new[] { 55 }),
                new MastSupportDefinition(foreSections: new[] { 10 }, aftSections: new[] { 69 }),
                new MastSupportDefinition(foreSections: new[] { 11 }, aftSections: new[] { 55 }),
            };
    }
}
