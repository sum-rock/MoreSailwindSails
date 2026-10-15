// Owns Sanbuq's authored shared winch sources and fallback placements.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class SanbuqControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Sanbuq.Definition,
            sheetCategories: SheetCategories(),
            halyardGroups: HalyardGroups()
        );

        // Installed rig identities and support audit: Controls/HalyardMounts.txt.
        private static HalyardWinchGroup[] HalyardGroups() =>
            new[]
            {
                // Mainmast 1; jib and flying forestay controls on its lower support.
                new HalyardWinchGroup(
                    mast: 10,
                    sources: new[] { 10, 2, 4, 6, 7, 15, 73 },
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #5, structure/mast, 2026-10-07.
                        contact: new Vector3(-0.002824f, 3.861896f, 4.554796f),
                        normal: new Vector3(-0.195081f, 0.008149f, -0.980753f),
                        sourceNormal: new Vector3(0f, 0f, -1f),
                        // Installed reef mesh rear bound -0.063862, scale 1.282723.
                        offset: 0.08191725f,
                        templateIndex: 0
                    )
                ),
                new HalyardWinchGroup(mast: 13, sources: new[] { 13, 2, 4, 6, 7, 15, 73 }),
                // Mainmast 2; jib and fore-to-main stay controls.
                new HalyardWinchGroup(
                    mast: 11,
                    sources: new[] { 11, 3, 5, 8, 16, 17, 57, 58, 68 },
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #3, structure/mast_1, 2026-10-07.
                        contact: new Vector3(-0.000560f, 4.040192f, -0.299039f),
                        normal: new Vector3(-0.195080f, 0.008149f, -0.980753f),
                        sourceNormal: new Vector3(0f, 0f, -1f),
                        offset: 0.08191725f,
                        templateIndex: 0
                    )
                ),
                new HalyardWinchGroup(mast: 14, sources: new[] { 14, 3, 5, 8, 16, 17, 57, 58, 68 }),
                // Original mizzen; midstay and matching topmast-stay controls.
                new HalyardWinchGroup(mast: 12, sources: new[] { 12, 9, 56, 81, 82 }),
                new HalyardWinchGroup(mast: 80, sources: new[] { 80, 9, 56, 81, 82 }),
                // Mizzenmast 2; mz2 stays.
                new HalyardWinchGroup(
                    mast: 55,
                    sources: new[] { 55, 54, 60, 66, 67 },
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #2 supersedes #1 on SE_cols_sanbuq/mizzen_mast2.
                        contact: new Vector3(0.008465f, 4.537066f, -8.221054f),
                        normal: new Vector3(0.195080f, 0.007966f, -0.980755f),
                        sourceNormal: new Vector3(0f, 0f, -1f),
                        // Installed SE reef mesh rear bound -0.063862, scale 1.282720.
                        offset: 0.08191708f,
                        templateIndex: 0
                    )
                ),
                new HalyardWinchGroup(mast: 59, sources: new[] { 59, 54, 60, 66, 67 }),
                // Mizzenmast 3; mz1 stays.
                new HalyardWinchGroup(
                    mast: 69,
                    sources: new[] { 69, 61, 71 },
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #4, SE_cols_sanbuq/mizzen_mast, 2026-10-07.
                        contact: new Vector3(-0.006912f, 4.239334f, -5.220303f),
                        normal: new Vector3(-0.195082f, 0.008149f, -0.980753f),
                        sourceNormal: new Vector3(0f, 0f, -1f),
                        offset: 0.08191708f,
                        templateIndex: 0
                    )
                ),
                new HalyardWinchGroup(mast: 70, sources: new[] { 70, 61, 71 }),
                // Upright foremast; lower, topmast and flying forestay controls.
                new HalyardWinchGroup(mast: 51, sources: new[] { 51, 52, 53, 65, 72 }),
                new HalyardWinchGroup(mast: 64, sources: new[] { 64, 52, 53, 65, 72 }),
                // Raked foremast; reef seat parented to raked_foremast.
                new HalyardWinchGroup(mast: 62, sources: new[] { 62, 63, 78 }),
            };

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 51, 62 },
                    sources: new[] { 51, 62, 64, 57, 58, 68, 79, 52, 53, 63, 65, 72, 78 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 10, 11, 75 },
                    sources: new[]
                    {
                        10,
                        11,
                        13,
                        14,
                        75,
                        9,
                        54,
                        56,
                        60,
                        61,
                        66,
                        67,
                        71,
                        81,
                        82,
                        2,
                        3,
                        4,
                        5,
                        6,
                        7,
                        8,
                        15,
                        16,
                        17,
                        73,
                        76,
                        77,
                    },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 12, 55, 69, 74 },
                    sources: new[] { 12, 55, 69, 59, 70, 80, 74 },
                    fallback: null
                ),
            };
    }
}
