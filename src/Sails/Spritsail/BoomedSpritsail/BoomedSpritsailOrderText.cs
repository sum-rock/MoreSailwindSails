using System;
using System.Collections.Generic;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Wraps the spritsail shipyard description iteratively.
    internal static class BoomedSpritsailOrderText
    {
        internal const int LineWidth = 45;

        internal static bool NeedsWrapping(string line) =>
            line != null
            && line.Length > LineWidth
            && line.IndexOf(value: "Boomed Spritsail", comparisonType: StringComparison.Ordinal)
                >= 0;

        internal static IEnumerable<string> Wrap(string line)
        {
            foreach (
                var paragraph in line.Replace(oldValue: "\r\n", newValue: "\n")
                    .Split(separator: '\n')
            )
            {
                string remaining = paragraph;
                while (remaining.Length > LineWidth)
                {
                    int split = remaining.LastIndexOf(value: ' ', startIndex: LineWidth);
                    if (split <= 0)
                        split = LineWidth;
                    yield return remaining.Substring(startIndex: 0, length: split);
                    remaining = remaining.Substring(startIndex: split).TrimStart(' ');
                }
                yield return remaining;
            }
        }
    }
}
