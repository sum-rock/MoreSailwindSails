using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreSailwindSails.BoatRigs
{
    // Immutable ordered native reef sources for one exact requested mast ID.
    internal sealed class HalyardWinchGroup
    {
        internal readonly int Mast;
        internal readonly IReadOnlyList<int> Sources;

        internal HalyardWinchGroup(int mast, IEnumerable<int> sources)
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
        }
    }
}
