// Owns Caelanor's authored Flying Sail support pairs.

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Configuration.Boats
{
    internal static class CaelanorFlyingSail
    {
        internal static readonly FlyingSailDefinition Definition = new FlyingSailDefinition(
            boat: BoatConfiguration.Boats.Caelanor.Definition,
            supports: new[]
            {
                new MastSupportDefinition(
                    foreSections: new[] { 17, 2, 1 },
                    aftSections: new[] { 18, 6, 5 }
                ),
            }
        );
    }
}
