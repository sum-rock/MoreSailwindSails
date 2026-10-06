using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    // Owns Gloriana's installed OldChronian 0.6.0 mast, stay and native-control profile.
    internal static class Gloriana
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            boatName: "BOAT GLORIANA (182)",
            supports: new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 5,
                    foreSections: new[] { 1 },
                    aftSections: new[] { 2 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 6,
                    foreSections: new[] { 2 },
                    aftSections: new[] { 4, 3 }
                ),
            },
            stays: Stays(),
            mastParents: new Dictionary<int, int>
            {
                { 1, -1 },
                { 2, -1 },
                { 3, -1 },
                { 4, 3 },
            },
            sheetCategories: new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 1 },
                    sources: new[] { 1, 5 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 2 },
                    sources: new[] { 2, 6 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 3 },
                    sources: new[] { 3, 4 },
                    fallback: null
                ),
            },
            // Lower mizzen can use the upper mizzen and mizzenstay seats on its shared rail.
            // Mainmast retains its own native array before the measured fallback.
            halyardGroups: new[]
            {
                new HalyardWinchGroup(mast: 1, sources: new[] { 1, 5 }),
                new HalyardWinchGroup(
                    mast: 2,
                    sources: new[] { 2 },
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #2, MAST_MAIN walking surface, 2026-10-04.
                        contact: new Vector3(-0.005373f, 1.887422f, 0.791344f),
                        normal: new Vector3(-0.098015f, 0.003737f, 0.995178f),
                        // halyard_mainmast2 local +Z in the installed boat frame.
                        sourceNormal: new Vector3(0f, 0.98480785f, -0.17364828f),
                        // Installed mesh rear bound -0.13091201, native scale 0.7.
                        offset: 0.09163841f,
                        templateIndex: 1
                    )
                ),
                new HalyardWinchGroup(
                    mast: 3,
                    sources: new[] { 3, 4, 6 },
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #1, FIFE_MIZEN walking surface, 2026-10-05.
                        contact: new Vector3(0.801387f, 4.769459f, -9.351812f),
                        normal: Vector3.right,
                        // halyard_mizenmast1 local +Z in the installed boat frame.
                        sourceNormal: new Vector3(-0.17364817f, 0.96984630f, 0.17101012f),
                        // Installed mesh rear bound -0.13091201, native scale 0.7.
                        offset: 0.09163841f,
                        templateIndex: 0
                    )
                ),
                new HalyardWinchGroup(mast: 4, sources: new[] { 4, 6 }),
            }
        );

        // Endpoints use installed spar frames and native gaff-guide heights.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast",
                    variants: new[]
                    {
                        // 67.195 degrees; meets the rendered foremast tip.
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 5,
                            label: "Foremast / Mainmast",
                            fore: 1,
                            forePoint: new Vector3(0f, 0f, 1.60888767f),
                            aft: 2,
                            aftPoint: new Vector3(0f, 0f, 0.865f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 2 },
                            forbidden: new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Mainmast / mizzenmast",
                    variants: new[]
                    {
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 6,
                            label: "Mainmast / Mizenmast Main",
                            fore: 2,
                            forePoint: new Vector3(0f, 0f, -15.65632243f),
                            aft: 3,
                            aftPoint: new Vector3(0f, 0f, 1.269f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 3 },
                            forbidden: new[] { 4 }
                        ),
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 6,
                            label: "Mainmast / Mizenmast Top",
                            fore: 2,
                            forePoint: new Vector3(0f, 0f, -9.09545912f),
                            aft: 4,
                            aftPoint: new Vector3(0f, 0f, 0.517f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 3, 4 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            };
    }
}
