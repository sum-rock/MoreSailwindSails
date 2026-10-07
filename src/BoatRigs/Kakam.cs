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
                    sources: new[] { 5, 6, 62, 70 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 7 },
                    sources: new[] { 7 },
                    fallback: null
                ),
            }
        );
    }
}
