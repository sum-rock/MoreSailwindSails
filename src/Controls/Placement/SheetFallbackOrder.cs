// Selects eligible authored pairs and orders new placements along the boat's aft (-Z) direction.
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls.Configuration;

namespace MoreSailwindSails.Controls.Placement
{
    internal static class SheetFallbackOrder
    {
        internal static IEnumerable<SheetFallbackDefinition> FromMast(
            IEnumerable<SheetFallbackDefinition> fallbacks,
            float mastZ
        ) =>
            fallbacks
                .Where(item => Eligible(item: item, mastZ: mastZ))
                .OrderByDescending(item =>
                    System.Math.Max(item.Fallback.Port.Contact.z, item.Fallback.Starboard.Contact.z)
                );

        internal static SheetFallbackDefinition FindCurrent(
            IReadOnlyList<SheetFallbackDefinition> fallbacks,
            string id,
            float mastZ
        )
        {
            for (int i = 0; i < fallbacks.Count; i++)
            {
                var item = fallbacks[i];
                if (item.FallbackName == id)
                    return Eligible(item: item, mastZ: mastZ) ? item : null;
            }
            return null;
        }

        private static bool Eligible(SheetFallbackDefinition item, float mastZ) =>
            item.Fallback != null
            && item.Fallback.InvalidSide == null
            && item.Fallback.Port.Contact.z <= mastZ
            && item.Fallback.Starboard.Contact.z <= mastZ;
    }
}
