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
                new MastSupportDefinition(foreSections: new[] { 1 }, aftSections: new[] { 2 }),
                new MastSupportDefinition(foreSections: new[] { 2 }, aftSections: new[] { 3 }),
                new MastSupportDefinition(foreSections: new[] { 51 }, aftSections: new[] { 52 }),
                new MastSupportDefinition(foreSections: new[] { 51 }, aftSections: new[] { 3 }),
                new MastSupportDefinition(foreSections: new[] { 1 }, aftSections: new[] { 52 }),
                new MastSupportDefinition(foreSections: new[] { 52 }, aftSections: new[] { 4 }),
            };
    }
}
