// Owns Cog's authored shared winch sources and fallback placements.

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class CogControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Cog.Definition,
            sheetCategories: SheetCategories(),
            halyardGroups: new[]
            {
                // Midstay 2-1 and 1-1 share one reef seat beside the original mizzen.
                new HalyardWinchGroup(mast: 8, sources: new[] { 8, 51, 65 }),
                // Midstay 2-2's reef seat is beside mizzen mast 2.
                new HalyardWinchGroup(mast: 57, sources: new[] { 57, 58 }),
            }
        );

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 6, 5, 59, 63 },
                    sources: new[] { 6, 5, 59, 63, 58, 3, 4, 53, 54, 55, 60, 61, 62, 64, 67 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 56, 68 },
                    sources: new[] { 56, 68, 69, 70 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 8, 57, 66 },
                    sources: new[] { 8, 57, 66, 51, 65 },
                    fallback: null
                ),
            };
    }
}
