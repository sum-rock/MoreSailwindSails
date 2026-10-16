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
                new MastSupportDefinition(foreSections: new[] { 3 }, aftSections: new[] { 5 }),
                new MastSupportDefinition(foreSections: new[] { 3 }, aftSections: new[] { 4 }),
                new MastSupportDefinition(foreSections: new[] { 2 }, aftSections: new[] { 5 }),
                new MastSupportDefinition(foreSections: new[] { 2 }, aftSections: new[] { 4 }),
                new MastSupportDefinition(foreSections: new[] { 4 }, aftSections: new[] { 7 }),
                new MastSupportDefinition(foreSections: new[] { 4 }, aftSections: new[] { 6 }),
                new MastSupportDefinition(foreSections: new[] { 3 }, aftSections: new[] { 56, 5 }),
                new MastSupportDefinition(foreSections: new[] { 3 }, aftSections: new[] { 58, 4 }),
                new MastSupportDefinition(foreSections: new[] { 2 }, aftSections: new[] { 56, 5 }),
                new MastSupportDefinition(foreSections: new[] { 2 }, aftSections: new[] { 58, 4 }),
                new MastSupportDefinition(foreSections: new[] { 4 }, aftSections: new[] { 59, 7 }),
                new MastSupportDefinition(foreSections: new[] { 4 }, aftSections: new[] { 60, 6 }),
            };
    }
}
