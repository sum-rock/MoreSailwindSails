using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreSailwindSails.BoatRigs
{
    // Physical variants and sheet-source rigs are deliberately separate. Labels
    // describe audited shipyard groups; runtime lookup never parses display names.
    // Immutable authored SheetWinchCategory data, copied at the definition boundary.
    internal sealed class SheetWinchCategory
    {
        internal readonly string Name;
        internal readonly IReadOnlyList<int> PhysicalMasts;
        internal readonly IReadOnlyList<int> Sources;
        internal readonly string FallbackName;
        internal readonly SheetFallbackPair Fallback;

        internal SheetWinchCategory(
            string name,
            IEnumerable<int> physicalMasts,
            IEnumerable<int> sources,
            SheetFallbackPair fallback = null
        )
        {
            var physicalMastsCopy = (physicalMasts ?? Array.Empty<int>()).ToArray();
            var sourcesCopy = (sources ?? Array.Empty<int>()).ToArray();
            Name = name;
            PhysicalMasts = Array.AsReadOnly(physicalMastsCopy);
            Sources = Array.AsReadOnly(sourcesCopy);
            FallbackName = name + "Fallback";
            Fallback = fallback;
        }
    }
}
