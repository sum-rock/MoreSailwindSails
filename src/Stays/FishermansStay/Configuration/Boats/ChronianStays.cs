// Owns Chronian's authored stay variants and attachment measurements.
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration.Boats
{
    internal static class ChronianStays
    {
        internal static readonly FishermansStayDefinition Definition = new FishermansStayDefinition(
            boat: BoatConfiguration.Boats.Chronian.Definition,
            stays: Stays()
        );

        // Fore/main joins the Top-section mastheads; main/mizzen follows the native stay line.
        // Optional T'gallants neither replace nor disable these options.
        private static FishermansStayGroupDefinition[] Stays() =>
            new[]
            {
                new FishermansStayGroupDefinition(
                    label: "Foremast / mainmast",
                    variants: new[]
                    {
                        new FishermansStayVariantDefinition(
                            mountIndex: 128,
                            donor: 12,
                            label: "Foremast Top / Mainmast Top",
                            fore: 3,
                            forePoint: new Vector3(0f, 0f, 1.76902924f),
                            foreCollarRadius: 0.14626f,
                            foreCollarOffset: new Vector3(0.00000f, 0.00000f, -0.03103f),
                            aft: 5,
                            aftPoint: new Vector3(0f, 0f, 1.83004532f),
                            aftCollarRadius: 0.14623f,
                            aftCollarOffset: new Vector3(0.00000f, 0.00000f, -0.03105f),
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 2, 3, 4, 5 },
                            forbidden: new int[0],
                            alignGuideHeightToAftAnchor: true
                        ),
                    }
                ),
                new FishermansStayGroupDefinition(
                    label: "Mainmast / mizzenmast",
                    variants: new[]
                    {
                        new FishermansStayVariantDefinition(
                            mountIndex: 129,
                            donor: 12,
                            label: "Mainmast / Mizzenmast Top",
                            fore: 4,
                            forePoint: new Vector3(0f, 0f, 3.2056313f),
                            foreCollarRadius: 0.23996f,
                            aft: 7,
                            aftPoint: new Vector3(0f, 0f, 0.8296186f),
                            aftCollarRadius: 0.15055f,
                            extendedGuide: false,
                            guideIndex: 0,
                            required: new[] { 4, 5, 6, 7 },
                            forbidden: new[] { 14 },
                            // Use the existing guide-height alignment at this authored attachment.
                            alignGuideHeightToAftAnchor: true
                        ),
                    }
                ),
            };
    }
}
