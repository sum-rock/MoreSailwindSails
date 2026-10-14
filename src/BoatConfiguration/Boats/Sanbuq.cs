// Owns Sanbuq's shared boat identity and physical mast ancestry.
using System.Collections.Generic;

namespace MoreSailwindSails.BoatConfiguration.Boats
{
    internal static class Sanbuq
    {
        internal static readonly BoatDefinition Definition = new BoatDefinition(
            boatName: "BOAT dhow medium (20)",
            mastParents: MastParents()
        );

        // Installed mast ancestry: -1 marks a physical base section.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 10, -1 },
                { 11, -1 },
                { 12, -1 },
                { 55, -1 },
                { 69, -1 },
                { 13, 10 },
                { 14, 11 },
                { 51, -1 },
                { 62, -1 },
                { 64, 51 },
                { 59, 55 },
                { 70, 69 },
                { 80, 12 },
            };
    }
}
