// Owns Jong's shared boat identity and physical mast ancestry.
using System.Collections.Generic;

namespace MoreSailwindSails.BoatConfiguration.Boats
{
    internal static class Jong
    {
        internal static readonly BoatDefinition Definition = new BoatDefinition(
            boatName: "BOAT junk large (70)",
            mastParents: MastParents()
        );

        // Installed mast ancestry: -1 marks a physical base section.
        private static Dictionary<int, int> MastParents() =>
            new Dictionary<int, int>
            {
                { 0, -1 },
                { 1, -1 },
                { 67, -1 },
                { 2, -1 },
                { 51, -1 },
                { 3, -1 },
                { 52, -1 },
                { 4, -1 },
                { 53, -1 },
            };
    }
}
