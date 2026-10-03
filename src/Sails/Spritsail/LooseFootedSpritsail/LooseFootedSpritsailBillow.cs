using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail
{
    // Shapes spare fabric between a mast-pinned luff and the free foot/leech.
    internal static class LooseFootedSpritsailBillow
    {
        internal static float ClothTravel(float width, float u, float v) =>
            width
            * (
                0.035f * (float)Math.Sin(a: Math.PI * u)
                + 0.015f * u * (float)Math.Sin(a: Math.PI * v)
            );

        internal static Vector3 SupportPoint(Vector3 clew, Vector3 head, Vector3 bow, float t) =>
            clew + (head - clew) * t + bow * (4 * t * (1 - t));

        internal static float SmoothLoad(float previous, float target, float seconds)
        {
            if (!SpritsailDeployment.Finite(value: target))
                target = 0;
            return previous
                + (Math.Max(val1: -1, val2: Math.Min(val1: 1, val2: target)) - previous)
                    * (
                        1
                        - (float)
                            Math.Exp(
                                d: -3 * Math.Max(val1: 0, val2: Math.Min(val1: 0.1f, val2: seconds))
                            )
                    );
        }
    }
}
