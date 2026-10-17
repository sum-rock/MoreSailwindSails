// Owns Kakam's authored stay variants and attachment measurements.
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration.Boats
{
    internal static class KakamStays
    {
        internal static readonly FishermansStayDefinition Definition = new FishermansStayDefinition(
            boat: BoatConfiguration.Boats.Kakam.Definition,
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
                            foreCollarRadius: 0.10972f,
                            aft: 7,
                            aftPoint: new Vector3(0f, 0f, 1.33141270f),
                            aftCollarRadius: 0.07809f,
                            aftCollarOffset: new Vector3(0.00832f, 0.00000f, 0.00000f),
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
                            foreCollarRadius: 0.10593f,
                            aft: 7,
                            aftPoint: new Vector3(0f, 0f, 1.33141270f),
                            aftCollarRadius: 0.07809f,
                            aftCollarOffset: new Vector3(0.00832f, 0.00000f, 0.00000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 6, 7 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            }
        );
    }
}
