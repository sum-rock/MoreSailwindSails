// Owns Dhow's authored Flying Sail support pairs.

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Configuration.Boats
{
    internal static class DhowFlyingSail
    {
        internal static readonly FlyingSailDefinition Definition = new FlyingSailDefinition(
            boat: BoatConfiguration.Boats.Dhow.Definition,
            supports: new[]
            {
                new MastSupportDefinition(foreSections: new[] { 6, 7 }, aftSections: new[] { 51 }),
            }
        );
    }
}
