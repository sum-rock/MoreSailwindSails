// Orders complete authored pairs along the boat's fore-to-aft (-Z) direction.
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls.Configuration;

namespace MoreSailwindSails.Controls
{
    internal static class SheetFallbackOrder
    {
        internal static IEnumerable<SheetFallbackDefinition> FromMast(
            IEnumerable<SheetFallbackDefinition> fallbacks,
            float mastZ
        ) =>
            fallbacks
                .Where(item => item.Fallback != null && item.Fallback.InvalidSide == null)
                .Where(item =>
                    item.Fallback.Port.Contact.z <= mastZ
                    && item.Fallback.Starboard.Contact.z <= mastZ
                )
                .OrderByDescending(item =>
                    System.Math.Max(item.Fallback.Port.Contact.z, item.Fallback.Starboard.Contact.z)
                );
    }
}
