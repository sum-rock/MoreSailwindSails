// Owns Chronian's authored shared winch sources and fallback placements.

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class ChronianControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Chronian.Definition,
            sheetCategories: new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 2 },
                    sources: new[] { 2, 3, 15, 11, 12, 8, 9, 10 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 4 },
                    sources: new[] { 4, 5, 16, 13, 14 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 6 },
                    sources: new[] { 6, 7, 17 },
                    fallback: null
                ),
            },
            halyardGroups: new[]
            {
                new HalyardWinchGroup(mast: 2, sources: new[] { 2, 8 }),
                new HalyardWinchGroup(mast: 3, sources: new[] { 3, 9, 10 }),
                new HalyardWinchGroup(mast: 4, sources: new[] { 4, 11 }),
                new HalyardWinchGroup(mast: 5, sources: new[] { 5, 12 }),
                new HalyardWinchGroup(mast: 6, sources: new[] { 6, 13 }),
                new HalyardWinchGroup(mast: 7, sources: new[] { 7, 14 }),
            }
        );
    }
}
