using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    // Owns Chronian's installed OldChronian mast, stay and native-control profile.
    internal static class Chronian
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            boatName: "BOAT CHRONIAN (187)",
            supports: new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 12,
                    foreSections: new[] { 15, 3, 2 },
                    aftSections: new[] { 16, 5, 4 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 14,
                    foreSections: new[] { 16, 5, 4 },
                    aftSections: new[] { 17, 7, 6 }
                ),
            },
            stays: Stays(),
            mastParents: new Dictionary<int, int>
            {
                { 2, -1 },
                { 3, 2 },
                { 15, 3 },
                { 4, -1 },
                { 5, 4 },
                { 16, 5 },
                { 6, -1 },
                { 7, 6 },
                { 17, 7 },
            },
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
            // Native stay guides identify the physical section owning each reef seat.
            // T'gallants retain their own reef arrays.
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

        // Fore/main joins the Top-section mastheads; main/mizzen follows the native stay line.
        // Optional T'gallants neither replace nor disable these options.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast",
                    variants: new[]
                    {
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 12,
                            label: "Foremast Top / Mainmast Top",
                            fore: 3,
                            forePoint: new Vector3(0f, 0f, 1.76902924f),
                            aft: 5,
                            aftPoint: new Vector3(0f, 0f, 1.83004532f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 3, 4, 5 },
                            forbidden: new int[0],
                            alignGuideHeightToAftAnchor: true
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Mainmast / mizzenmast",
                    variants: new[]
                    {
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 12,
                            label: "Mainmast / Mizzenmast Top",
                            fore: 4,
                            forePoint: new Vector3(0f, 0f, 3.2056313f),
                            aft: 7,
                            aftPoint: new Vector3(0f, 0f, 0.8296186f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 5, 6, 7 },
                            forbidden: new[] { 14 },
                            // Use the existing guide-height alignment at this authored attachment.
                            alignGuideHeightToAftAnchor: true
                        ),
                    }
                ),
            };
    }
}
