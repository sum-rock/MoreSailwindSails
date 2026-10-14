// Owns Caelanor's authored shared winch sources and fallback placements.

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class CaelanorControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Caelanor.Definition,
            sheetCategories: new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 1 },
                    sources: new[] { 1, 2, 17, 13, 14, 7, 8, 9 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 5, 3 },
                    sources: new[] { 5, 6, 18, 3, 4, 19, 10, 12, 16 },
                    fallback: null
                ),
            },
            halyardGroups: new[]
            {
                new HalyardWinchGroup(mast: 1, sources: new[] { 1, 7 }),
                new HalyardWinchGroup(mast: 2, sources: new[] { 2, 8, 9 }),
                new HalyardWinchGroup(mast: 3, sources: new[] { 3, 10 }),
                new HalyardWinchGroup(mast: 4, sources: new[] { 4, 12, 16 }),
                new HalyardWinchGroup(mast: 5, sources: new[] { 5, 13 }),
                new HalyardWinchGroup(mast: 6, sources: new[] { 6, 14 }),
            }
        );
    }
}
