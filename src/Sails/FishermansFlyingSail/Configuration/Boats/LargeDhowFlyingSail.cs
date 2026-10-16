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
                new MastSupportDefinition(foreSections: new[] { 0 }, aftSections: new[] { 3, 2 }),
                new MastSupportDefinition(foreSections: new[] { 1 }, aftSections: new[] { 3, 2 }),
                new MastSupportDefinition(foreSections: new[] { 0 }, aftSections: new[] { 5, 4 }),
                new MastSupportDefinition(foreSections: new[] { 1 }, aftSections: new[] { 5, 4 }),
                new MastSupportDefinition(foreSections: new[] { 3, 2 }, aftSections: new[] { 6 }),
                new MastSupportDefinition(foreSections: new[] { 5, 4 }, aftSections: new[] { 6 }),
                new MastSupportDefinition(foreSections: new[] { 3, 2 }, aftSections: new[] { 7 }),
                new MastSupportDefinition(foreSections: new[] { 5, 4 }, aftSections: new[] { 7 }),
                new MastSupportDefinition(foreSections: new[] { 3, 2 }, aftSections: new[] { 8 }),
                new MastSupportDefinition(foreSections: new[] { 5, 4 }, aftSections: new[] { 8 }),
            };
    }
}
