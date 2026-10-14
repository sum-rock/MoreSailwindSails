// Owns Junk's authored stay variants and attachment measurements.
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration.Boats
{
    internal static class JunkStays
    {
        internal static readonly FishermansStayDefinition Definition = new FishermansStayDefinition(
            boat: BoatConfiguration.Boats.Junk.Definition,
            stays: Stays()
        );

        // Authored from installed native and boat-mod assets.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast",
                    variants: new[]
                    {
                        // 59.94 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 16,
                            label: "foremast / main mast 1",
                            fore: 9,
                            forePoint: new Vector3(0.00000f, 0.00049f, 2.69000f),
                            aft: 10,
                            aftPoint: new Vector3(0.00248f, -0.00146f, 2.30206f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 9, 10 },
                            forbidden: new[] { 62 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 16,
                            label: "fore topmast / main mast 1",
                            fore: 62,
                            forePoint: new Vector3(-0.00000f, -0.00000f, -0.75865f),
                            aft: 10,
                            aftPoint: new Vector3(0.00248f, -0.00146f, 2.30206f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 9, 10, 62 },
                            forbidden: new int[0]
                        ),
                        // 57.86 degrees at aft mast; forward masthead fallback.
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 61,
                            label: "raked foremast / main mast 2",
                            fore: 58,
                            forePoint: new Vector3(0.00000f, 0.00049f, 2.69000f),
                            aft: 11,
                            aftPoint: new Vector3(0.00248f, -0.00146f, 2.30206f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 58 },
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
                            mountIndex: 131,
                            donor: 5,
                            label: "main mast 1 / mizzen mast 1",
                            fore: 10,
                            forePoint: new Vector3(0.00248f, -0.00146f, -7.52300f),
                            aft: 12,
                            aftPoint: new Vector3(0.00000f, 0.00049f, 1.57183f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 10, 12 },
                            forbidden: new[] { 53 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 132,
                            donor: 5,
                            label: "main mast 1 / mizzen topmast",
                            fore: 10,
                            forePoint: new Vector3(0.00248f, -0.00146f, -2.59085f),
                            aft: 53,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.84800f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 10, 12, 53 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 133,
                            donor: 6,
                            label: "main mast 2 / mizzen mast 1",
                            fore: 11,
                            forePoint: new Vector3(0.00248f, -0.00146f, -8.89858f),
                            aft: 12,
                            aftPoint: new Vector3(0.00000f, 0.00049f, 1.57183f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 12 },
                            forbidden: new[] { 53 }
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 134,
                            donor: 6,
                            label: "main mast 2 / mizzen topmast",
                            fore: 11,
                            forePoint: new Vector3(0.00248f, -0.00146f, -3.96643f),
                            aft: 53,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.84800f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 12, 53 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 135,
                            donor: 7,
                            label: "main mast 2 / mizzen mast 2",
                            fore: 11,
                            forePoint: new Vector3(0.00248f, -0.00146f, -7.08233f),
                            aft: 13,
                            aftPoint: new Vector3(0.00000f, 0.00049f, 1.57200f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 13 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 136,
                            donor: 52,
                            label: "main mast 2 / mizzen mast 3",
                            fore: 11,
                            forePoint: new Vector3(0.00248f, -0.00146f, -2.19715f),
                            aft: 57,
                            aftPoint: new Vector3(0.00248f, -0.00146f, 2.30206f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 11, 57 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            };
    }
}
