// Owns Cog's authored stay variants and attachment measurements.
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration.Boats
{
    internal static class CogStays
    {
        internal static readonly FishermansStayDefinition Definition = new FishermansStayDefinition(
            boat: BoatConfiguration.Boats.Cog.Definition,
            stays: Stays()
        );

        // Authored from installed native and boat-mod assets.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Mainmast / mizzenmast",
                    variants: new[]
                    {
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 51,
                            label: "main mast 2 / mizzen mast",
                            fore: 5,
                            forePoint: new Vector3(-0.00000f, 0.00000f, -4.77700f),
                            foreCollarRadius: 0.16636f,
                            aft: 8,
                            aftPoint: new Vector3(-0.00000f, -0.00391f, 1.14600f),
                            aftCollarRadius: 0.11170f,
                            aftCollarOffset: new Vector3(0.00000f, 0.00391f, 0.00000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 5, 8 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 65,
                            label: "main mast 1 / mizzen mast",
                            fore: 6,
                            forePoint: new Vector3(-0.00000f, 0.00000f, -3.33567f),
                            foreCollarRadius: 0.15940f,
                            aft: 8,
                            aftPoint: new Vector3(-0.00000f, -0.00391f, 1.14600f),
                            aftCollarRadius: 0.11170f,
                            aftCollarOffset: new Vector3(0.00000f, 0.00391f, 0.00000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 6, 8 },
                            forbidden: new int[0]
                        ),
                        // 70.00 degrees at aft mast.
                        new FishermansStayVariantDefinition(
                            mountIndex: 130,
                            donor: 58,
                            label: "main mast 2 / mizzen mast 2",
                            fore: 5,
                            forePoint: new Vector3(0.00000f, 0.00000f, -4.25708f),
                            foreCollarRadius: 0.16385f,
                            aft: 57,
                            aftPoint: new Vector3(-0.00000f, -0.00391f, 1.14600f),
                            aftCollarRadius: 0.11170f,
                            aftCollarOffset: new Vector3(0.00000f, 0.00391f, 0.00000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 5, 57 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            };
    }
}
