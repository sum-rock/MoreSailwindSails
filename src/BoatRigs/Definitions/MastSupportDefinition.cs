using System;
using System.Linq;

namespace MoreSailwindSails.BoatRigs
{
    internal sealed class MastSupportDefinition
    {
        // Native rig ID supplying sheet winch assets; it need not be installed.
        internal readonly int SheetControlSource;

        // Sections are ordered from upper to lower. The last aft section
        // identifies the physical support when optional upper sections are absent.
        internal readonly int[] ForeSections,
            AftSections;

        internal MastSupportDefinition(
            int sheetControlSource,
            int[] foreSections,
            int[] aftSections
        )
        {
            if (
                sheetControlSource < 0
                || foreSections.Length == 0
                || aftSections.Length == 0
                || foreSections.Concat(aftSections).Any(id => id < 0)
                || foreSections.Intersect(aftSections).Any()
            )
                throw new ArgumentException("Invalid mast support or control source.");
            SheetControlSource = sheetControlSource;
            ForeSections = foreSections;
            AftSections = aftSections;
        }
    }
}
