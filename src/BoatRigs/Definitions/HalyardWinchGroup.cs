using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreSailwindSails.BoatRigs
{
    // Ordered native reef sources and an optional measured seat for one requested mast ID.
    internal sealed class HalyardWinchGroup
    {
        internal readonly int Mast;
        internal readonly IReadOnlyList<int> Sources;
        internal readonly HalyardFallbackSeat Fallback;

        internal HalyardWinchGroup(
            int mast,
            IEnumerable<int> sources,
            HalyardFallbackSeat fallback = null
        )
        {
            var sourcesCopy = (sources ?? Array.Empty<int>()).ToArray();
            if (
                mast < 0
                || mast >= 128
                || sourcesCopy.Length == 0
                || sourcesCopy.Any(id => id < 0 || id >= 128)
                || sourcesCopy.Distinct().Count() != sourcesCopy.Length
            )
                throw new ArgumentException("Invalid halyard winch group.");
            Mast = mast;
            Sources = Array.AsReadOnly(sourcesCopy);
            Fallback = fallback;
        }
    }
}
