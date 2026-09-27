using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal static class Shroud
    {
        internal static readonly BoatRigDefinition Definition = new BoatRigDefinition(
            boatName: "BOAT Shroud Large",
            supports: Supports(),
            stays: Stays(),
            mastParents: MastParents(),
            sheetCategories: SheetCategories(),
            winchClearances: PinClearances()
        );

        // Audited native/SE option groups and paired references: NativeWinchSeats.txt.
        private static SheetWinchCategory[] SheetCategories() =>
            new[]
            {
                new SheetWinchCategory(
                    name: "Foremast",
                    physicalMasts: new[] { 6, 5 },
                    sources: new[] { 6, 5, 16, 17, 24, 13, 14, 15, 20, 21, 22, 23 },
                    fallback: ForemastFallback
                ),
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 8, 7 },
                    sources: new[] { 8, 7, 18, 19, 25 },
                    fallback: null
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 10, 9 },
                    sources: new[] { 10, 9 },
                    fallback: null
                ),
            };

        private static SheetFallbackPair ForemastFallback =>
            new SheetFallbackPair(
                port: new SheetFallbackSeat(
                    contact: new Vector3(-3.465970f, 4.829765f, -3.901223f),
                    normal: Vector3.up,
                    sourceNormal: new Vector3(-0.006981f, 0.999976f, 0f),
                    offset: 0.076685f,
                    templateMast: 24,
                    templateRole: WinchRole.Left,
                    templateIndex: 0
                ),
                starboard: new SheetFallbackSeat(
                    contact: new Vector3(3.465970f, 4.829765f, -3.901223f),
                    normal: Vector3.up,
                    sourceNormal: new Vector3(-0.006981f, 0.999976f, 0f),
                    offset: 0.076685f,
                    templateMast: 24,
                    templateRole: WinchRole.Right,
                    templateIndex: 0
                )
            );

        // Measured side-pin pitch affects clearance only, never seat eligibility.
        private static Dictionary<string, float> PinClearances() =>
            new Dictionary<string, float>
            {
                { "Coil_Mainmast_Reef_1", 0.14f },
                { "Coil_Mainmast_Reef_2", 0.14f },
                { "Coil_Mainmast_Reef_3", 0.14f },
                { "Coil_Mainmast_Reef_4", 0.14f },
                { "Coil_Mainmast_Reef_5", 0.14f },
                { "Coil_Mainmast_Reef_6", 0.14f },
                { "Coil_Mainmast_Jib_Reef_1", 0.14f },
                { "Coil_Mainmast_Jib_Reef_2", 0.14f },
                { "Coil_Mainmast_Jib_Reef_3", 0.14f },
                { "Coil_Mainmast_Jib_Reef_4", 0.14f },
                { "Coil_Mainmast_Jib_Reef_5", 0.14f },
                { "Coil_Mainmast_Jib_Reef_6", 0.14f },
                { "Coil_Mizzenmast_Reef_1", 0.14f },
                { "Coil_Mizzenmast_Reef_2", 0.14f },
                { "Coil_Mizzenmast_Reef_3", 0.14f },
                { "Coil_Mizzenmast_Reef_4", 0.14f },
                { "Coil_Mizzenmast_Reef_5", 0.14f },
                { "Coil_Mizzenmast_Reef_7", 0.14f },
                { "Coil_Mizzenmast_Jib_Reef_1", 0.14f },
                { "Coil_Mizzenmast_Jib_Reef_2", 0.14f },
                { "Coil_Mizzenmast_Jib_Reef_3", 0.14f },
                { "Coil_Mizzenmast_Jib_Reef_4", 0.14f },
                { "Coil_Mizzenmast_Jib_Reef_5", 0.14f },
                { "Coil_Mizzenmast_Jib_Reef_6", 0.14f },
            };

        private static MastSupportDefinition[] Supports() =>
            new[]
            {
                new MastSupportDefinition(
                    sheetControlSource: 25,
                    foreSections: new[] { 7 },
                    aftSections: new[] { 9 }
                ),
            };

        // Installed mast ancestry: -1 marks a physical base section.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 6, -1 },
                { 5, -1 },
                { 8, -1 },
                { 7, -1 },
                { 10, -1 },
                { 9, -1 },
            };

        // Authored from installed native and boat-mod assets.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 24,
                            label: "Foremast 1 / Mainmast 1",
                            fore: 6,
                            forePoint: new Vector3(0.00000f, -0.00682f, -1.26976f),
                            aft: 8,
                            aftPoint: new Vector3(0.00000f, -0.00928f, -0.42400f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 6, 8 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 24,
                            label: "Foremast 1 Tall / Mainmast 1",
                            fore: 5,
                            forePoint: new Vector3(-0.00291f, 0.00006f, -11.85458f),
                            aft: 8,
                            aftPoint: new Vector3(0.00000f, -0.00928f, -0.42400f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 5, 8 },
                            forbidden: new int[0]
                        ),
                        // 42.20 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 24,
                            label: "Foremast 1 / Mainmast 1 Tall",
                            fore: 6,
                            forePoint: new Vector3(0.00000f, -0.00682f, 0.50000f),
                            aft: 7,
                            aftPoint: new Vector3(-0.00537f, -0.00391f, -0.46400f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 6, 7 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 131,
                            donor: 24,
                            label: "Foremast 1 Tall / Mainmast 1 Tall",
                            fore: 5,
                            forePoint: new Vector3(-0.00291f, 0.00006f, -2.44452f),
                            aft: 7,
                            aftPoint: new Vector3(-0.00537f, -0.00391f, -0.46400f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 5, 7 },
                            forbidden: new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Mainmast / mizzenmast",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 132,
                            donor: 25,
                            label: "Mainmast 1 / Mizzen 1",
                            fore: 8,
                            forePoint: new Vector3(0.00000f, -0.00928f, -6.67603f),
                            aft: 10,
                            aftPoint: new Vector3(0.00000f, -0.00365f, -0.51800f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 8, 10 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 133,
                            donor: 25,
                            label: "Mainmast 1 Tall / Mizzen 1",
                            fore: 7,
                            forePoint: new Vector3(-0.00537f, -0.00391f, -16.12609f),
                            aft: 10,
                            aftPoint: new Vector3(0.00000f, -0.00365f, -0.51800f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 10 },
                            forbidden: new int[0]
                        ),
                        // 55.88 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 134,
                            donor: 25,
                            label: "Mainmast 1 / Mizzenmast 1 Tall",
                            fore: 8,
                            forePoint: new Vector3(0.00000f, -0.00928f, 0.50000f),
                            aft: 9,
                            aftPoint: new Vector3(0.00025f, -0.00391f, -0.12789f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 8, 9 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 135,
                            donor: 25,
                            label: "Mainmast 1 Tall / Mizzenmast 1 Tall",
                            fore: 7,
                            forePoint: new Vector3(-0.00537f, -0.00391f, -5.67900f),
                            aft: 9,
                            aftPoint: new Vector3(0.00025f, -0.00391f, -0.12789f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 9 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            };
    }
}
