// Owns Cog's shared boat identity and physical mast ancestry.
using System.Collections.Generic;

namespace MoreSailwindSails.BoatConfiguration.Boats
{
    internal static class Cog
    {
        internal static readonly BoatDefinition Definition = new BoatDefinition(
            boatName: "BOAT medi small (40)",
            mastParents: MastParents()
        );

        // Installed mast ancestry: -1 marks a physical base section.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 6, -1 },
                { 5, -1 },
                { 8, -1 },
                { 57, -1 },
                { 56, -1 },
                { 68, -1 },
            };
    }
}
