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
                new MastSupportDefinition(foreSections: new[] { 8 }, aftSections: new[] { 5 }),
                new MastSupportDefinition(foreSections: new[] { 5 }, aftSections: new[] { 57 }),
                new MastSupportDefinition(foreSections: new[] { 8 }, aftSections: new[] { 6 }),
            };
    }
}
