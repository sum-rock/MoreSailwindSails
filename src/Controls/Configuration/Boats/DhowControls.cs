// Owns Dhow's authored shared winch sources and fallback placements.

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class DhowControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Dhow.Definition,
            sheetCategories: new[]
            {
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 6, 7 },
                    sources: new[] { 6, 7, 54 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 51 },
                    sources: new[] { 51 },
                    fallback: null
                ),
            }
        );
    }
}
