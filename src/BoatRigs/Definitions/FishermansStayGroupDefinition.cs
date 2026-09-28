using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreSailwindSails.BoatRigs
{
    // Immutable authored FishermansStayGroupDefinition data, copied at the definition boundary.
    internal sealed class FishermansStayGroupDefinition
    {
        internal readonly string Label;
        internal readonly IReadOnlyList<FishermansStayVariantDefinition> Variants;

        internal FishermansStayGroupDefinition(
            string label,
            IEnumerable<FishermansStayVariantDefinition> variants
        )
        {
            var variantsCopy = (
                variants ?? Array.Empty<FishermansStayVariantDefinition>()
            ).ToArray();
            if (string.IsNullOrEmpty(label) || variantsCopy.Length == 0)
                throw new ArgumentException("Empty Fisherman's Stay group.");
            Label = label;
            Variants = Array.AsReadOnly(variantsCopy);
        }
    }
}
