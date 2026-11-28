// Owns Sh'ba's authored stay variants and installed mast attachment measurements.
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration.Boats
{
    internal static class ShBaStays
    {
        internal static readonly FishermansStayDefinition Definition = new FishermansStayDefinition(
            boat: BoatConfiguration.Boats.ShBa.Definition,
            stays: Stays()
        );

        // Measured from the installed Shattered Seas Small/shroud small bundle, 2026-11-27.
        // Native stay 46 supplies material, pricing and control templates only;
        // construction uses the shared Fisherman's Stay rope and collar meshes.
        // Masthead guides retain their native radial offset. The aft bands sit
        // 5 cm below the visible tips so their full height remains on the spars.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Mainmast / mizzenmast",
                    variants: new[]
                    {
                        // 70 degrees to the aft spar's downward axis.
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 46,
                            label: "Mainmast 1 / Mizzenmast 2",
                            fore: 2,
                            forePoint: new Vector3(0.00323725f, -0.00390578f, -6.47364670f),
                            foreCollarRadius: 0.11900130f,
                            foreCollarOffset: new Vector3(-0.00048810f, 0.00390578f, 0.00000000f),
                            aft: 5,
                            aftPoint: new Vector3(0.00888132f, -0.00390619f, 0.16524315f),
                            aftCollarRadius: 0.09369782f,
                            aftCollarOffset: new Vector3(-0.00910024f, 0.00390619f, -0.05000000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 5 },
                            forbidden: new int[0],
                            alignGuideHeightToAftAnchor: true
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Foremast / mizzenmast",
                    variants: new[]
                    {
                        // 70 degrees to the aft spar's downward axis.
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 46,
                            label: "Foremast 2 / Mizzenmast 1",
                            fore: 1,
                            forePoint: new Vector3(0.00008081f, 0.00000170f, -2.41761794f),
                            foreCollarRadius: 0.10724754f,
                            foreCollarOffset: new Vector3(-0.00048704f, -0.00000170f, 0.00000000f),
                            aft: 4,
                            aftPoint: new Vector3(0.00230201f, 0.00000024f, 0.16200638f),
                            aftCollarRadius: 0.09726744f,
                            aftCollarOffset: new Vector3(0.00000067f, -0.00000024f, -0.05000000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 4 },
                            forbidden: new int[0],
                            alignGuideHeightToAftAnchor: true
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast",
                    variants: new[]
                    {
                        // 70 degrees to the aft spar's downward axis.
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 46,
                            label: "Foremast 1 / Mainmast 2",
                            fore: 0,
                            forePoint: new Vector3(0.00384366f, 0.00000010f, -1.20944093f),
                            foreCollarRadius: 0.10255618f,
                            foreCollarOffset: new Vector3(-0.00048765f, -0.00000010f, 0.00000000f),
                            aft: 3,
                            aftPoint: new Vector3(0.00150366f, 0.00000186f, 0.17302990f),
                            aftCollarRadius: 0.10130936f,
                            aftCollarOffset: new Vector3(-0.00048701f, -0.00000186f, -0.05000000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 0, 3 },
                            forbidden: new int[0],
                            alignGuideHeightToAftAnchor: true
                        ),
                        // 70 degrees to the aft spar's downward axis.
                        new FishermansStayVariantDefinition(
                            mountIndex: 131,
                            donor: 46,
                            label: "Foremast 1 Top / Mainmast 2 Top",
                            fore: 25,
                            forePoint: new Vector3(0.00000005f, -0.00390615f, -1.04624157f),
                            foreCollarRadius: 0.07969557f,
                            foreCollarOffset: new Vector3(-0.00237695f, 0.00390615f, 0.00000000f),
                            aft: 28,
                            aftPoint: new Vector3(-0.00000081f, 0.00000021f, 0.25215626f),
                            aftCollarRadius: 0.07940975f,
                            aftCollarOffset: new Vector3(-0.00087882f, -0.00000021f, -0.05000000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 0, 25, 3, 28 },
                            forbidden: new int[0],
                            alignGuideHeightToAftAnchor: true
                        ),
                        // 70 degrees to the aft spar's downward axis.
                        new FishermansStayVariantDefinition(
                            mountIndex: 132,
                            donor: 46,
                            label: "Foremast 2 / Mainmast 2",
                            fore: 1,
                            forePoint: new Vector3(0.00008081f, 0.00000170f, -0.37579147f),
                            foreCollarRadius: 0.09959532f,
                            foreCollarOffset: new Vector3(-0.00048704f, -0.00000170f, 0.00000000f),
                            aft: 3,
                            aftPoint: new Vector3(0.00150366f, 0.00000186f, 0.17302990f),
                            aftCollarRadius: 0.10130936f,
                            aftCollarOffset: new Vector3(-0.00048701f, -0.00000186f, -0.05000000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 3 },
                            forbidden: new int[0],
                            alignGuideHeightToAftAnchor: true
                        ),
                        // 70 degrees to the aft spar's downward axis.
                        new FishermansStayVariantDefinition(
                            mountIndex: 133,
                            donor: 46,
                            label: "Foremast 2 Top / Mainmast 2 Top",
                            fore: 26,
                            forePoint: new Vector3(-0.00000136f, 0.00000044f, -0.28184592f),
                            foreCollarRadius: 0.07951897f,
                            foreCollarOffset: new Vector3(-0.00250953f, -0.00000044f, 0.00000000f),
                            aft: 28,
                            aftPoint: new Vector3(-0.00000081f, 0.00000021f, 0.25215626f),
                            aftCollarRadius: 0.07940975f,
                            aftCollarOffset: new Vector3(-0.00087882f, -0.00000021f, -0.05000000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 26, 3, 28 },
                            forbidden: new int[0],
                            alignGuideHeightToAftAnchor: true
                        ),
                    }
                ),
            };
    }
}
