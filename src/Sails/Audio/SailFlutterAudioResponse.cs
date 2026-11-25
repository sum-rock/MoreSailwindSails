// Maps strong visual flutter and deployed cloth to a bounded audio volume.
using System;

namespace MoreSailwindSails.Sails.Audio
{
    internal static class SailFlutterAudioResponse
    {
        internal static float Volume(float strength, float deployment)
        {
            if (!Finite(value: strength) || !Finite(value: deployment))
                return 0;
            float t = Math.Max(0, Math.Min(1, (strength - 0.5f) * 2));
            return t * t * (3 - 2 * t) * Math.Max(0, Math.Min(1, deployment));
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
