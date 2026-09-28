using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreSailwindSails.BoatRigs
{
    // Immutable authored MastSupportDefinition data, copied at the definition boundary.
    internal sealed class MastSupportDefinition
    {
        // Native rig ID supplying sheet winch assets; it need not be installed.
        internal readonly int SheetControlSource;

        // Sections are ordered from upper to lower. The last aft section
        // identifies the physical support when optional upper sections are absent.
        internal readonly IReadOnlyList<int> ForeSections,
            AftSections;

        internal MastSupportDefinition(
            int sheetControlSource,
            IEnumerable<int> foreSections,
            IEnumerable<int> aftSections
        )
        {
            var foreSectionsCopy = (foreSections ?? Array.Empty<int>()).ToArray();
            var aftSectionsCopy = (aftSections ?? Array.Empty<int>()).ToArray();
            if (
                sheetControlSource < 0
                || foreSectionsCopy.Length == 0
                || aftSectionsCopy.Length == 0
                || foreSectionsCopy.Concat(aftSectionsCopy).Any(id => id < 0)
                || foreSectionsCopy.Intersect(aftSectionsCopy).Any()
            )
                throw new ArgumentException("Invalid mast support or control source.");
            SheetControlSource = sheetControlSource;
            ForeSections = Array.AsReadOnly(foreSectionsCopy);
            AftSections = Array.AsReadOnly(aftSectionsCopy);
        }
    }
}
