using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    // Owns Dhow's physical main/mizzen stay configurations and native control sources.
    internal static class Dhow
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            boatName: "BOAT dhow small (10)",
            supports: new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 54,
                    foreSections: new[] { 6, 7 },
                    aftSections: new[] { 51 }
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
                            donor: 54,
                            label: "short mast / mizzen mast",
                            fore: 6,
                            forePoint: new Vector3(0f, 0f, -0.80807668f),
                            aft: 51,
                            aftPoint: new Vector3(0f, 0f, 0.61600000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 6, 51 },
                            forbidden: new int[0]
                        ),
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 54,
                            label: "tall mast / mizzen mast",
                            fore: 7,
                            forePoint: new Vector3(0f, 0f, -4.80807567f),
                            aft: 51,
                            aftPoint: new Vector3(0f, 0f, 0.61600000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 51 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            },
            mastParents: new Dictionary<int, int>
            {
                { 6, -1 },
                { 7, -1 },
                { 51, -1 },
            },
            sheetCategories: new[]
            {
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 6, 7 },
                    sources: new[] { 6, 7, 54 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 51 },
                    sources: new[] { 51 },
                    fallback: null
                ),
            }
        );
    }
}
