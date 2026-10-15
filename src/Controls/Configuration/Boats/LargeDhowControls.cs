// Owns LargeDhow's authored shared winch sources and fallback placements.

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class LargeDhowControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.LargeDhow.Definition,
            sheetCategories: SheetCategories(),
            halyardGroups: HalyardGroups()
        );

        // Installed rig identities and support audit: Controls/HalyardMounts.txt.
        private static HalyardWinchGroup[] HalyardGroups() =>
            new[]
            {
                // Foremast 1; stay reef seat parented to mast_fore_0.
                new HalyardWinchGroup(mast: 0, sources: new[] { 0, 10 }),
                // Foremast 2; stay reef seat parented to mast_fore_1__raked_.
                new HalyardWinchGroup(mast: 1, sources: new[] { 1, 11 }),
                // Mainmast 1 and topmast; stay seats parented to mast_main_0.
                new HalyardWinchGroup(
                    mast: 2,
                    sources: new[] { 2, 12, 13, 14, 15, 20, 21, 22, 23, 24, 25 }
                ),
                new HalyardWinchGroup(
                    mast: 3,
                    sources: new[] { 3, 12, 13, 14, 15, 20, 21, 22, 23, 24, 25 }
                ),
                // Mainmast 2 and topmast; stay seats parented to mast_main_1.
                new HalyardWinchGroup(
                    mast: 4,
                    sources: new[] { 4, 16, 17, 18, 19, 26, 27, 28, 29, 30, 31 }
                ),
                new HalyardWinchGroup(
                    mast: 5,
                    sources: new[] { 5, 16, 17, 18, 19, 26, 27, 28, 29, 30, 31 }
                ),
                // Mizzenmast 1; stay seat parented to mast_mizzen_0.
                new HalyardWinchGroup(mast: 6, sources: new[] { 6, 32, 33, 34, 35 }),
                // Mizzenmast 2; stay seat parented to mast_mizzen_1__raked.
                new HalyardWinchGroup(mast: 7, sources: new[] { 7, 36, 37, 38, 39 }),
                // Mizzenmast 3; stay seat parented to mast_mizzen_2.
                new HalyardWinchGroup(mast: 8, sources: new[] { 8, 40, 41, 42, 43 }),
            };

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 0, 1 },
                    sources: new[] { 0, 1, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 10, 11 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 2, 4 },
                    sources: new[]
                    {
                        2,
                        4,
                        3,
                        5,
                        32,
                        33,
                        34,
                        35,
                        36,
                        37,
                        38,
                        39,
                        40,
                        41,
                        42,
                        43,
                        12,
                        13,
                        14,
                        15,
                        16,
                        17,
                        18,
                        19,
                    },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 6, 7, 8 },
                    sources: new[] { 6, 7, 8 },
                    fallback: null
                ),
            };
    }
}
