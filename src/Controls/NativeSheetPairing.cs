using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreSailwindSails.Controls
{
    internal static class NativeSheetPairing
    {
        // Only authored correspondences or matching array indices are pairs.
        // A missing side never borrows the next usable control from that side.
        internal static IEnumerable<int[]> Indices<T>(
            T[] port,
            T[] starboard,
            int[][] authored,
            Func<T, bool> usable,
            Action<string> invalid
        )
            where T : class
        {
            port = port ?? Array.Empty<T>();
            starboard = starboard ?? Array.Empty<T>();
            var pairs =
                authored
                ?? Enumerable
                    .Range(0, Math.Max(port.Length, starboard.Length))
                    .Select(i => new[] { i, i })
                    .ToArray();
            foreach (var pair in pairs)
            {
                if (
                    pair == null
                    || pair.Length != 2
                    || pair[0] < 0
                    || pair[1] < 0
                    || pair[0] >= port.Length
                    || pair[1] >= starboard.Length
                    || !usable(port[pair[0]])
                    || !usable(starboard[pair[1]])
                )
                {
                    invalid(pair == null ? "null" : string.Join(",", pair));
                    continue;
                }
                yield return pair;
            }
        }
    }
}
