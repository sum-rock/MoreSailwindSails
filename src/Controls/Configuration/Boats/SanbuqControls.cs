// Owns Sanbuq's authored fallback placements.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class SanbuqControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Sanbuq.Definition,
            halyardFallbacks: HalyardFallbacks()
        );

        private static HalyardFallbackDefinition[] HalyardFallbacks() =>
            new HalyardFallbackDefinition[]
            {
                new HalyardFallbackDefinition(
                    mast: 10,
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #5, structure/mast, 2026-10-07.
                        contact: new Vector3(-0.002824f, 3.861896f, 4.554796f),
                        normal: new Vector3(-0.195081f, 0.008149f, -0.980753f),
                        sourceNormal: new Vector3(0f, 0f, -1f),
                        // Installed reef mesh rear bound -0.063862, scale 1.282723.
                        offset: 0.08191725f,
                        templateIndex: 0
                    )
                ),
                new HalyardFallbackDefinition(
                    mast: 11,
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #3, structure/mast_1, 2026-10-07.
                        contact: new Vector3(-0.000560f, 4.040192f, -0.299039f),
                        normal: new Vector3(-0.195080f, 0.008149f, -0.980753f),
                        sourceNormal: new Vector3(0f, 0f, -1f),
                        offset: 0.08191725f,
                        templateIndex: 0
                    )
                ),
                new HalyardFallbackDefinition(
                    mast: 55,
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #2 supersedes #1 on SE_cols_sanbuq/mizzen_mast2.
                        contact: new Vector3(0.008465f, 4.537066f, -8.221054f),
                        normal: new Vector3(0.195080f, 0.007966f, -0.980755f),
                        sourceNormal: new Vector3(0f, 0f, -1f),
                        // Installed SE reef mesh rear bound -0.063862, scale 1.282720.
                        offset: 0.08191708f,
                        templateIndex: 0
                    )
                ),
                new HalyardFallbackDefinition(
                    mast: 69,
                    fallback: new HalyardFallbackSeat(
                        // F9 capture #4, SE_cols_sanbuq/mizzen_mast, 2026-10-07.
                        contact: new Vector3(-0.006912f, 4.239334f, -5.220303f),
                        normal: new Vector3(-0.195082f, 0.008149f, -0.980753f),
                        sourceNormal: new Vector3(0f, 0f, -1f),
                        offset: 0.08191708f,
                        templateIndex: 0
                    )
                ),
            };
    }
}
