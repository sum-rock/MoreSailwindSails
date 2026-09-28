using System;
using System.Collections.Generic;

namespace MoreSailwindSails.Controls
{
    internal static class NativeSheetPairing
    {
        // Only matching array indices are pairs.
        // A missing side never borrows the next usable control from that side.
        internal static IEnumerable<int[]> Indices<T>(
            T[] port,
            T[] starboard,
            Func<T, bool> usable,
            Action<string> invalid
        )
            where T : class
        {
            port = port ?? Array.Empty<T>();
            starboard = starboard ?? Array.Empty<T>();
            for (int i = 0; i < Math.Max(port.Length, starboard.Length); i++)
            {
                var pair = new[] { i, i };
                if (
                    i >= port.Length
                    || i >= starboard.Length
                    || !usable(port[i])
                    || !usable(starboard[i])
                )
                {
                    invalid(string.Join(",", pair));
                    continue;
                }
                yield return pair;
            }
        }
    }
}
