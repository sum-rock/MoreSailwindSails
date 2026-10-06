using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    // Owns Caelanor's installed mast ancestry, single masthead stay and native controls.
    internal static class Caelanor
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            boatName: "BOAT CAELANOR (192)",
            supports: new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 14,
                    foreSections: new[] { 17, 2, 1 },
                    aftSections: new[] { 18, 6, 5 }
                ),
            },
            stays: new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast",
                    variants: new[]
                    {
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 14,
                            label: "Foremast Top / Mainmast Top",
                            fore: 2,
                            forePoint: new Vector3(0f, 0f, 1.0960133f),
                            aft: 6,
                            aftPoint: new Vector3(0f, 0f, 1.0960107f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 2, 5, 6 },
                            forbidden: new int[0],
                            alignGuideHeightToAftAnchor: true
                        ),
                    }
                ),
            },
            mastParents: new Dictionary<int, int>
            {
                { 1, -1 },
                { 2, 1 },
                { 17, 2 },
                { 3, -1 },
                { 4, 3 },
                { 19, 4 },
                { 5, -1 },
                { 6, 5 },
                { 18, 6 },
            },
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
