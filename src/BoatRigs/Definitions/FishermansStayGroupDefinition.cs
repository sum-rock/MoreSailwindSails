using System;

namespace MoreSailwindSails.BoatRigs
{
    internal sealed class FishermansStayGroupDefinition
    {
        internal readonly string Label;
        internal readonly FishermansStayVariantDefinition[] Variants;

        internal FishermansStayGroupDefinition(
            string label,
            FishermansStayVariantDefinition[] variants
        )
        {
            if (string.IsNullOrEmpty(label) || variants.Length == 0)
                throw new ArgumentException("Empty Fisherman's Stay group.");
            Label = label;
            Variants = variants;
        }
    }
}
