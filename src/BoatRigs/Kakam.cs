using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    // Owns Kakam's physical main/mizzen stay configurations and native control sources.
    internal static class Kakam
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            boatName: "BOAT junk small singleroof(90)",
            supports: new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 62,
                    foreSections: new[] { 5 },
                    aftSections: new[] { 7 }
                ),
                new MastSupportDefinition(
                    sheetControlSource: 70,
                    foreSections: new[] { 6 },
                    aftSections: new[] { 7 }
                ),
            },
            stays: new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Mainmast / mizzenmast",
                    variants: new[]
                    {
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 62,
                            label: "main mast 1 / mizzen mast",
                            fore: 5,
                            forePoint: new Vector3(0f, 0f, -2.45784888f),
                            aft: 7,
                            aftPoint: new Vector3(0f, 0f, 1.33141270f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 5, 7 },
                            forbidden: new int[0]
                        ),
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 70,
                            label: "main mast 2 / mizzen mast",
                            fore: 6,
                            forePoint: new Vector3(0f, 0f, -1.42321188f),
                            aft: 7,
                            aftPoint: new Vector3(0f, 0f, 1.33141270f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 6, 7 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            },
            mastParents: new Dictionary<int, int>
            {
                { 5, -1 },
                { 6, -1 },
                { 7, -1 },
            },
            sheetCategories: new[]
            {
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 5, 6 },
                    // Use the captured aft trim pair instead of the forward native seats.
                    sources: new int[0],
                    fallback: MainmastFallback
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 7 },
                    sources: new[] { 7 },
                    fallback: null
                ),
            },
            halyardGroups: new[]
            {
                new HalyardWinchGroup(
                    mast: 7,
                    sources: new[] { 7 },
                    fallback: new HalyardFallbackSeat(
                        contact: new Vector3(-0.000950f, 2.005890f, -4.232710f),
                        normal: new Vector3(-0.000001f, 0.036369f, -0.999338f),
                        sourceNormal: new Vector3(-1f, 0f, 0f),
                        // Installed reef template rear Z bound 0.063862 at scale 0.792.
                        offset: 0.05057871f,
                        templateIndex: 0
                    )
                ),
            }
        );

        // F9 captures #2/#3 centered across the boat, with a shared normal for parallel winches.
        // Both installed angle templates have the same rotation and rear Z bound -0.044704.
        private static SheetFallbackPair MainmastFallback =>
            new SheetFallbackPair(
                port: new SheetFallbackSeat(
                    contact: new Vector3(-0.8067125f, 2.7384885f, -6.0352715f),
                    normal: new Vector3(0f, 0.984682f, 0.150002f),
                    sourceNormal: new Vector3(0.00020555f, 0.99987845f, 0.01559755f),
                    offset: 0.044704f,
                    templateMast: 5,
                    templateRole: WinchRole.Left,
                    templateIndex: 0
                ),
                starboard: new SheetFallbackSeat(
                    contact: new Vector3(0.8067125f, 2.7384885f, -6.0352715f),
                    normal: new Vector3(0f, 0.984682f, 0.150002f),
                    sourceNormal: new Vector3(0.00020555f, 0.99987845f, 0.01559755f),
                    offset: 0.044704f,
                    templateMast: 5,
                    templateRole: WinchRole.Right,
                    templateIndex: 0
                )
            );
    }
}
