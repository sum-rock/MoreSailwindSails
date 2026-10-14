// Owns Junk's authored shared winch sources and fallback placements.

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class JunkControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Junk.Definition,
            sheetCategories: SheetCategories(),
            halyardGroups: HalyardGroups()
        );

        // Installed rig identities and support audit: Controls/HalyardMounts.txt.
        private static HalyardWinchGroup[] HalyardGroups() =>
            new[]
            {
                // Foremast and its topmast; native and long-sprit forestay controls.
                new HalyardWinchGroup(mast: 9, sources: new[] { 9, 14, 54, 67 }),
                new HalyardWinchGroup(mast: 62, sources: new[] { 62, 14, 54, 67 }),
                // Raked foremast; corresponding forestay control.
                new HalyardWinchGroup(mast: 58, sources: new[] { 58, 60 }),
                // Mainmast 1; forestays and fore-to-main stays.
                new HalyardWinchGroup(mast: 10, sources: new[] { 10, 0, 1, 2, 15, 16, 63, 64 }),
                // Mainmast 2; forestays and fore-to-main stay.
                new HalyardWinchGroup(mast: 11, sources: new[] { 11, 3, 4, 55, 56, 61, 68 }),
                // Mizzenmast 1; mast_mizzen_0 midstay and matching topmast stay controls.
                new HalyardWinchGroup(mast: 12, sources: new[] { 12, 5, 6, 65, 66 }),
                new HalyardWinchGroup(mast: 53, sources: new[] { 53, 5, 6, 65, 66 }),
                // Mizzenmast 2; mast_mizzen_1 midstay control.
                new HalyardWinchGroup(mast: 13, sources: new[] { 13, 7 }),
                // Mizzenmast 3; mast_miz_2 midstay control.
                new HalyardWinchGroup(mast: 57, sources: new[] { 57, 52 }),
            };

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 9, 58, 69 },
                    sources: new[] { 9, 58, 62, 69, 15, 16, 61, 14, 54, 60, 67, 72 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 10, 11, 70 },
                    sources: new[]
                    {
                        10,
                        11,
                        70,
                        5,
                        6,
                        7,
                        52,
                        65,
                        66,
                        0,
                        1,
                        2,
                        3,
                        4,
                        55,
                        56,
                        63,
                        64,
                        68,
                        73,
                        74,
                    },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 12, 13, 57, 71 },
                    sources: new[] { 12, 13, 57, 53, 71 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Bowsprit",
                    physicalMasts: new[] { 8, 51 },
                    sources: new[] { 8, 51 },
                    fallback: null
                ),
            };
    }
}
