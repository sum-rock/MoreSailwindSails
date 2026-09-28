using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreSailwindSails.Controls
{
    // Selects a complete startup template pair without consulting seat vacancy.
    internal static class WinchBootstrapPolicy
    {
        internal static T[] Sheets<T>(
            IEnumerable<int> sources,
            Func<int, T[]> port,
            Func<int, T[]> starboard,
            Func<T, bool> usable,
            Func<T[]> fallback
        )
            where T : class
        {
            foreach (int source in sources)
            {
                var left = port(source);
                var right = starboard(source);
                var pair = NativeSheetPairing
                    .Indices(port: left, starboard: right, usable: usable, invalid: _ => { })
                    .FirstOrDefault();
                if (pair != null)
                    return new[] { left[pair[0]], right[pair[1]] };
            }
            var manual = fallback();
            return manual != null && manual.Length == 2 && manual.All(usable) ? manual : null;
        }
    }
}
