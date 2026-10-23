// Owns Dhow's authored stay variants and attachment measurements.
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration.Boats
{
    internal static class DhowStays
    {
        internal static readonly FishermansStayDefinition Definition = new FishermansStayDefinition(
            boat: BoatConfiguration.Boats.Dhow.Definition,
            // Both mast pairs share one physical stay and its menu/mount identity.
            stays: new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Mainmast / mizzenmast",
                    variants: new[]
                    {
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 54,
                            label: "mainmast / mizzenmast",
                            fore: 6,
                            forePoint: new Vector3(0f, 0f, -0.80807668f),
                            foreCollarRadius: 0.10801f,
                            foreCollarOffset: new Vector3(0.01086f, -0.00351f, 0.00000f),
                            aft: 51,
                            aftPoint: new Vector3(0f, 0f, 0.61600000f),
                            aftCollarRadius: 0.08083f,
                            aftCollarOffset: new Vector3(0.01019f, -0.00352f, 0.00000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 6, 51 },
                            forbidden: new int[0]
                        ),
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 54,
                            label: "mainmast / mizzenmast",
                            fore: 7,
                            forePoint: new Vector3(0f, 0f, -4.80807567f),
                            foreCollarRadius: 0.11605f,
                            foreCollarOffset: new Vector3(0.01129f, -0.00351f, 0.00000f),
                            aft: 51,
                            aftPoint: new Vector3(0f, 0f, 0.61600000f),
                            aftCollarRadius: 0.08083f,
                            aftCollarOffset: new Vector3(0.01019f, -0.00352f, 0.00000f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 7, 51 },
                            forbidden: new int[0]
                        ),
                    }
                ),
            }
        );
    }
}
