// Owns Kakam's authored shared winch sources and fallback placements.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class KakamControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Kakam.Definition,
            sheetCategories: new[]
            {
                new SheetWinchCategory(
                    name: "Mainmast",
                    physicalMasts: new[] { 5, 6 },
                    // Use the captured aft trim pair instead of the forward native seats.
                    sources: new int[0],
                    fallback: MainmastFallback
                ),
                new SheetWinchCategory(
                    name: "Mizzenmast",
                    physicalMasts: new[] { 7 },
                    sources: new[] { 7 },
                    fallback: null
                ),
            },
            halyardGroups: new[]
            {
                new HalyardWinchGroup(
                    mast: 7,
                    sources: new[] { 7 },
                    fallback: new HalyardFallbackSeat(
                        contact: new Vector3(-0.000950f, 2.005890f, -4.232710f),
                        normal: new Vector3(-0.000001f, 0.036369f, -0.999338f),
                        sourceNormal: new Vector3(-1f, 0f, 0f),
                        // Installed reef template rear Z bound 0.063862 at scale 0.792.
                        offset: 0.05057871f,
                        templateIndex: 0
                    )
                ),
            }
        );

        // F9 captures #2/#3 centered across the boat, with a shared normal for parallel winches.
        // Both installed angle templates have the same rotation and rear Z bound -0.044704.
        private static SheetFallbackPair MainmastFallback =>
            new SheetFallbackPair(
                port: new SheetFallbackSeat(
                    contact: new Vector3(-0.8067125f, 2.7384885f, -6.0352715f),
                    normal: new Vector3(0f, 0.984682f, 0.150002f),
                    sourceNormal: new Vector3(0.00020555f, 0.99987845f, 0.01559755f),
                    offset: 0.044704f,
                    templateMast: 5,
                    templateRole: WinchRole.Left,
                    templateIndex: 0
                ),
                starboard: new SheetFallbackSeat(
                    contact: new Vector3(0.8067125f, 2.7384885f, -6.0352715f),
                    normal: new Vector3(0f, 0.984682f, 0.150002f),
                    sourceNormal: new Vector3(0.00020555f, 0.99987845f, 0.01559755f),
                    offset: 0.044704f,
                    templateMast: 5,
                    templateRole: WinchRole.Right,
                    templateIndex: 0
                )
            );
    }
}
