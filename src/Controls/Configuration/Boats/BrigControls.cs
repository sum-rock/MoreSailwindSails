// Owns Brig's authored shared winch sources and fallback placements.

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class BrigControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Brig.Definition,
            sheetCategories: SheetCategories(),
            halyardGroups: HalyardGroups()
        );

        // Installed rig identities and support audit: Controls/HalyardMounts.txt.
        private static HalyardWinchGroup[] HalyardGroups() =>
            new[]
            {
                // Foremast 1; F0 jib seats and matching topmast stays.
                new HalyardWinchGroup(mast: 3, sources: new[] { 3, 8, 9, 10, 11, 68, 72, 75, 77 }),
                new HalyardWinchGroup(
                    mast: 55,
                    sources: new[] { 55, 8, 9, 10, 11, 68, 72, 75, 77 }
                ),
                // Foremast 2; F1 jib seats; excludes unregistered duplicate 74.
                new HalyardWinchGroup(mast: 2, sources: new[] { 2, 12, 13, 26, 69, 76, 78 }),
                new HalyardWinchGroup(mast: 57, sources: new[] { 57, 12, 13, 26, 69, 76, 78 }),
                // Mainmast 1; B0 stays and matching topmast stays.
                new HalyardWinchGroup(mast: 5, sources: new[] { 5, 14, 15, 17, 18, 61, 63 }),
                new HalyardWinchGroup(mast: 56, sources: new[] { 56, 14, 15, 17, 18, 61, 63 }),
                // Mainmast 2; B1 stays; registered 70 has B1 reef seats.
                new HalyardWinchGroup(
                    mast: 4,
                    sources: new[] { 4, 16, 19, 20, 25, 51, 52, 62, 64, 70, 71, 73 }
                ),
                new HalyardWinchGroup(
                    mast: 58,
                    sources: new[] { 58, 16, 19, 20, 25, 51, 52, 62, 64, 70, 71, 73 }
                ),
                // Mizzenmast 1; mizzen0 jib and topmast stay seats.
                new HalyardWinchGroup(mast: 7, sources: new[] { 7, 21, 22, 65 }),
                new HalyardWinchGroup(mast: 59, sources: new[] { 59, 21, 22, 65 }),
                // Mizzenmast 2; mizzen1 jib and topmast stay seats.
                new HalyardWinchGroup(mast: 6, sources: new[] { 6, 23, 24, 66 }),
                new HalyardWinchGroup(mast: 60, sources: new[] { 60, 23, 24, 66 }),
                // Raked foremast 1; F2 jib seat.
                new HalyardWinchGroup(mast: 67, sources: new[] { 67, 80 }),
                // Raked foremast 2; F3 jib seats.
                new HalyardWinchGroup(mast: 79, sources: new[] { 79, 81 }),
            };

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 3, 2, 67, 79, 82 },
                    sources: new[]
                    {
                        3,
                        2,
                        55,
                        57,
                        67,
                        79,
                        82,
                        14,
                        15,
                        25,
                        16,
                        17,
                        18,
                        19,
                        20,
                        61,
                        62,
                        63,
                        64,
                        8,
                        9,
                        10,
                        11,
                        12,
                        13,
                        26,
                        68,
                        69,
                        70,
                        71,
                        72,
                        74,
                        75,
                        76,
                        77,
                        78,
                        80,
                        81,
                        85,
                        86,
                    },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 5, 4, 83 },
                    sources: new[] { 5, 4, 56, 58, 83, 21, 22, 23, 24, 65, 66, 51, 52, 73 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 7, 6, 84 },
                    sources: new[] { 7, 6, 59, 60, 84 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Bowsprit",
                    physicalMasts: new[] { 53, 54 },
                    sources: new[] { 53, 54 },
                    fallback: null
                ),
            };
    }
}
