// Owns Jong's authored shared winch sources and fallback placements.

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class JongControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Jong.Definition,
            sheetCategories: SheetCategories(),
            halyardGroups: HalyardGroups()
        );

        // Installed rig identities and support audit: Controls/HalyardMounts.txt.
        private static HalyardWinchGroup[] HalyardGroups() =>
            new[]
            {
                // Upright foremast; upper and lower native forestay controls.
                new HalyardWinchGroup(mast: 1, sources: new[] { 1, 5, 6 }),
                // Mainmast 1 aft position; front-stay controls at that position.
                new HalyardWinchGroup(mast: 2, sources: new[] { 2, 7, 8, 9, 10, 65, 68, 72 }),
                // Mainmast 1 forward position; front-stay controls at that position.
                new HalyardWinchGroup(mast: 51, sources: new[] { 51, 60, 62, 64, 66 }),
                // Mainmast 2 aft position; middle-stay controls at that position.
                new HalyardWinchGroup(mast: 3, sources: new[] { 3, 11, 12, 58, 59 }),
                // Mainmast 2 forward position; middle-stay controls at that position.
                new HalyardWinchGroup(mast: 52, sources: new[] { 52, 54, 55, 73, 74 }),
                // Mizzenmast 1; back-stay controls at the aft position.
                new HalyardWinchGroup(mast: 4, sources: new[] { 4, 13, 14, 70, 71 }),
                // Mizzenmast 2; back-stay controls at the forward position.
                new HalyardWinchGroup(mast: 53, sources: new[] { 53, 56, 57, 75, 76 }),
            };

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 1, 67, 79 },
                    sources: new[] { 1, 67, 79, 7, 10, 73, 74, 5, 6, 81 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "MainmastA",
                    physicalMasts: new[] { 2, 51, 78 },
                    sources: new[]
                    {
                        2,
                        51,
                        78,
                        11,
                        12,
                        54,
                        55,
                        58,
                        59,
                        75,
                        76,
                        8,
                        9,
                        60,
                        61,
                        62,
                        64,
                        65,
                        66,
                        68,
                        69,
                        72,
                        82,
                        83,
                        84,
                    },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "MainmastB",
                    physicalMasts: new[] { 3, 52, 77 },
                    sources: new[] { 3, 52, 77, 13, 14, 56, 57, 70, 71 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 4, 53, 80 },
                    sources: new[] { 4, 53, 80 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Bowsprit",
                    physicalMasts: new[] { 0 },
                    sources: new[] { 0 },
                    fallback: null
                ),
            };
    }
}
