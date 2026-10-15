// Owns Junk's shared boat identity and physical mast ancestry.
using System.Collections.Generic;

namespace MoreSailwindSails.BoatConfiguration.Boats
{
    internal static class Junk
    {
        internal static readonly BoatDefinition Definition = new BoatDefinition(
            boatName: "BOAT junk medium (80)",
            mastParents: MastParents()
        );

        // Installed mast ancestry: -1 marks a physical base section.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 8, -1 },
                { 9, -1 },
                { 58, -1 },
                { 10, -1 },
                { 11, -1 },
                { 12, -1 },
                { 13, -1 },
                { 57, -1 },
                { 53, 12 },
                { 62, 9 },
            };
    }
}
