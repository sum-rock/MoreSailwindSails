// Owns Gloriana's authored Flying Sail support pairs.

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Configuration.Boats
{
    internal static class GlorianaFlyingSail
    {
        internal static readonly FlyingSailDefinition Definition = new FlyingSailDefinition(
            boat: BoatConfiguration.Boats.Gloriana.Definition,
            supports: new[]
            {
                new MastSupportDefinition(foreSections: new[] { 1 }, aftSections: new[] { 2 }),
                new MastSupportDefinition(foreSections: new[] { 2 }, aftSections: new[] { 4, 3 }),
            }
        );
    }
}
