// Owns Sh'ba's shared boat identity and physical mast ancestry.
using System.Collections.Generic;

namespace MoreSailwindSails.BoatConfiguration.Boats
{
    internal static class ShBa
    {
        internal static readonly BoatDefinition Definition = new BoatDefinition(
            boatName: "BOAT Shroud Small (160)",
            mastParents: MastParents()
        );

        // Installed mast ancestry: -1 marks a physical base section.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 0, -1 },
                { 1, -1 },
                { 2, -1 },
                { 3, -1 },
                { 4, -1 },
                { 5, -1 },
                { 25, 0 },
                { 26, 1 },
                { 27, 2 },
                { 28, 3 },
            };
    }
}
