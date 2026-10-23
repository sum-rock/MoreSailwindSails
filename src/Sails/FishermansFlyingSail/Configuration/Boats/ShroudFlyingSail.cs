// Owns Shroud's authored Flying Sail support pairs.

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Configuration.Boats
{
    internal static class ShroudFlyingSail
    {
        internal static readonly FlyingSailDefinition Definition = new FlyingSailDefinition(
            boat: BoatConfiguration.Boats.Shroud.Definition,
            supports: Supports()
        );

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(foreSections: new[] { 7 }, aftSections: new[] { 9 }),
            };
    }
}
