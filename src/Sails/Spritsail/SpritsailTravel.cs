using System;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Bounds sheet and sway travel while retaining tighter collision limits.
    internal static class SpritsailTravel
    {
        // Matches the installed native collision checker default, measured
        // either side of the neutral, fore-and-aft mast frame.
        internal const int MaximumAngle = 89;

        internal static float Clamp(float angle) =>
            Math.Max(val1: -MaximumAngle, val2: Math.Min(val1: MaximumAngle, val2: angle));

        internal static void ConstrainHinge(
            ref float min,
            ref float max,
            float allowedMin,
            float allowedMax
        )
        {
            allowedMin = Clamp(angle: allowedMin);
            allowedMax = Clamp(angle: allowedMax);
            min = Math.Max(val1: allowedMin, val2: Math.Min(val1: allowedMax, val2: min));
            max = Math.Max(val1: allowedMin, val2: Math.Min(val1: allowedMax, val2: max));
            // Tight opposing sheets plus negative native sway can cross the
            // endpoints. Meet halfway instead of assigning an inverted joint range.
            if (min > max)
                min = max = (min + max) * 0.5f;
        }
    }
}
