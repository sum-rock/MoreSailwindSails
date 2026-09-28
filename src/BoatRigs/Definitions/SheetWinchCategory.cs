using System.Linq;

namespace MoreSailwindSails.BoatRigs
{
    // Physical variants and sheet-source rigs are deliberately separate. Labels
    // describe audited shipyard groups; runtime lookup never parses display names.
    internal sealed class SheetWinchCategory
    {
        internal readonly string Name;
        internal readonly int[] PhysicalMasts;
        internal readonly NativeSheetSource[] Sources;
        internal readonly string FallbackName;
        internal readonly SheetFallbackPair Fallback;

        internal SheetWinchCategory(
            string name,
            int[] physicalMasts,
            int[] sources,
            SheetFallbackPair fallback = null
        )
        {
            Name = name;
            PhysicalMasts = physicalMasts;
            Sources = sources.Select(id => new NativeSheetSource(id)).ToArray();
            FallbackName = name + "Fallback";
            Fallback = fallback;
        }
    }
}
