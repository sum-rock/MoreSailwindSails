using System.Collections.Generic;
using System.Linq;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Models native indexed slots and identity conflicts without allocating new controls.
    internal static class SpritsailControlSlots
    {
        internal static T At<T>(T[] items, int index)
            where T : class =>
            items != null && index >= 0 && index < items.Length ? items[index] : null;

        internal static int SheetIndex(bool square, int order) => square ? 0 : order;

        internal static bool Conflict<T>(T[] requested, IEnumerable<T> others)
            where T : class =>
            requested.Distinct().Count() != requested.Length || requested.Intersect(others).Any();
    }
}
