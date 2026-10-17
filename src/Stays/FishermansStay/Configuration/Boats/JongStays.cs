// Owns Jong's authored stay variants and attachment measurements.
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration.Boats
{
    internal static class JongStays
    {
        internal static readonly FishermansStayDefinition Definition = new FishermansStayDefinition(
            boat: BoatConfiguration.Boats.Jong.Definition,
            stays: Stays()
        );

        // Authored from installed native and boat-mod assets.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast 1",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 10,
                            label: "foremast / main mast 1",
                            fore: 1,
                            forePoint: new Vector3(0.00000f, -0.00000f, 1.13012f),
                            foreCollarRadius: 0.16053f,
                            aft: 2,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.05000f),
                            aftCollarRadius: 0.16492f,
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 2 },
                            forbidden: new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Mainmast 1 / mainmast 2",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 12,
                            label: "main mast 1 / main mast 2",
                            fore: 2,
                            forePoint: new Vector3(0.00000f, -0.00000f, -2.22414f),
                            foreCollarRadius: 0.17343f,
                            aft: 3,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.09200f),
                            aftCollarRadius: 0.16576f,
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 3 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 55,
                            label: "main mast 1 fore / main mast 2 fore",
                            fore: 51,
                            forePoint: new Vector3(0.00000f, -0.00000f, -2.22315f),
                            foreCollarRadius: 0.17343f,
                            aft: 52,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.09200f),
                            aftCollarRadius: 0.16576f,
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 51, 52 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 131,
                            donor: 58,
                            label: "main mast 1 fore / main mast 2",
                            fore: 51,
                            forePoint: new Vector3(0.00000f, -0.00000f, -3.69245f),
                            foreCollarRadius: 0.17892f,
                            aft: 3,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.09200f),
                            aftCollarRadius: 0.16576f,
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 3, 51 },
                            forbidden: new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast 2",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 132,
                            donor: 73,
                            label: "foremast / main mast 2 fore",
                            fore: 1,
                            forePoint: new Vector3(-0.00000f, -0.00000f, 0.32528f),
                            foreCollarRadius: 0.16371f,
                            aft: 52,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.09200f),
                            aftCollarRadius: 0.16576f,
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 1, 52 },
                            forbidden: new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Mainmast 2 / mizzenmast",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 133,
                            donor: 13,
                            label: "main mast 2 / mizzen mast",
                            fore: 3,
                            forePoint: new Vector3(0.00000f, -0.00000f, -7.77488f),
                            foreCollarRadius: 0.19355f,
                            aft: 4,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.15000f),
                            aftCollarRadius: 0.16280f,
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 3, 4 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 134,
                            donor: 56,
                            label: "main mast 2 fore / mizzen mast 2",
                            fore: 52,
                            forePoint: new Vector3(0.00000f, -0.00000f, -7.77379f),
                            foreCollarRadius: 0.19355f,
                            aft: 53,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.15000f),
                            aftCollarRadius: 0.16280f,
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 52, 53 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 135,
                            donor: 71,
                            label: "main mast 2 fore / mizzen mast",
                            fore: 52,
                            forePoint: new Vector3(0.00000f, -0.00000f, -9.24418f),
                            foreCollarRadius: 0.19874f,
                            aft: 4,
                            aftPoint: new Vector3(-0.00000f, -0.00000f, 0.15000f),
                            aftCollarRadius: 0.16280f,
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 52 },
                            forbidden: new int[0]
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Mainmast 1 / mizzenmast",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 136,
                            donor: 75,
                            label: "main mast 1 / mizzen mast 2",
                            fore: 2,
                            forePoint: new Vector3(0.00000f, -0.00000f, -8.62064f),
                            foreCollarRadius: 0.19735f,
                            aft: 53,
                            aftPoint: new Vector3(0.00000f, -0.00000f, 0.15000f),
                            aftCollarRadius: 0.16280f,
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 53 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            };
    }
}
