// Owns Junk's authored Flying Sail support pairs.

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Configuration.Boats
{
    internal static class JunkFlyingSail
    {
        internal static readonly FlyingSailDefinition Definition = new FlyingSailDefinition(
            boat: BoatConfiguration.Boats.Junk.Definition,
            supports: Supports()
        );

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(foreSections: new[] { 9 }, aftSections: new[] { 10 }),
                new MastSupportDefinition(foreSections: new[] { 58 }, aftSections: new[] { 11 }),
                new MastSupportDefinition(foreSections: new[] { 10 }, aftSections: new[] { 12 }),
                new MastSupportDefinition(foreSections: new[] { 11 }, aftSections: new[] { 12 }),
                new MastSupportDefinition(
                    foreSections: new[] { 10 },
                    aftSections: new[] { 53, 12 }
                ),
                new MastSupportDefinition(
                    foreSections: new[] { 11 },
                    aftSections: new[] { 53, 12 }
                ),
            };
    }
}
