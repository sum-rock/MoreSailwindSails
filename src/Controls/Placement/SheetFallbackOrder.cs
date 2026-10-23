// Selects eligible authored pairs and orders new placements along the boat's aft (-Z) direction.
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls.Configuration;

namespace MoreSailwindSails.Controls.Placement
{
    internal static class SheetFallbackOrder
    {
        // Null means a recognized sail has not supplied a valid fitted span yet.
        internal static float Boundary(float mastZ, float? fittedSpan) =>
            fittedSpan.HasValue
            && !float.IsNaN(fittedSpan.Value)
            && !float.IsInfinity(fittedSpan.Value)
            && fittedSpan.Value >= 0f
                ? mastZ - fittedSpan.Value
                : float.NaN;

        internal static IEnumerable<SheetFallbackDefinition> FromBoundary(
            IEnumerable<SheetFallbackDefinition> fallbacks,
            float aftBoundaryZ
        ) =>
            fallbacks
                .Where(item => Eligible(item: item, aftBoundaryZ: aftBoundaryZ))
                .OrderByDescending(item =>
                    System.Math.Max(item.Fallback.Port.Contact.z, item.Fallback.Starboard.Contact.z)
                );

        internal static SheetFallbackDefinition FindCurrent(
            IReadOnlyList<SheetFallbackDefinition> fallbacks,
            string id,
            float aftBoundaryZ
        )
        {
            for (int i = 0; i < fallbacks.Count; i++)
            {
                var item = fallbacks[i];
                if (item.FallbackName == id)
                    return Eligible(item: item, aftBoundaryZ: aftBoundaryZ) ? item : null;
            }
            return null;
        }

        private static bool Eligible(SheetFallbackDefinition item, float aftBoundaryZ) =>
            item.Fallback != null
            && item.Fallback.InvalidSide == null
            && item.Fallback.Port.Contact.z <= aftBoundaryZ
            && item.Fallback.Starboard.Contact.z <= aftBoundaryZ;
    }
}
