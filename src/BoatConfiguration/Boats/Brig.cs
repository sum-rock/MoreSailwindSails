// Owns Brig's shared boat identity and physical mast ancestry.
using System.Collections.Generic;

namespace MoreSailwindSails.BoatConfiguration.Boats
{
    internal static class Brig
    {
        internal static readonly BoatDefinition Definition = new BoatDefinition(
            boatName: "BOAT medi medium (50)",
            mastParents: MastParents()
        );

        // Installed mast ancestry: -1 marks a physical base section.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 3, -1 },
                { 2, -1 },
                { 67, -1 },
                { 79, -1 },
                { 5, -1 },
                { 4, -1 },
                { 7, -1 },
                { 6, -1 },
                { 53, -1 },
                { 54, -1 },
                { 55, 3 },
                { 57, 2 },
                { 56, 5 },
                { 58, 4 },
                { 59, 7 },
                { 60, 6 },
            };
    }
}
