using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Shapes spare fabric between a mast-pinned luff and the boom-supported foot and free leech.
    internal static class BoomedSpritsailBillow
    {
        internal static float ClothTravel(float width, float u, float v) =>
            width
            * (1 - v)
            * (
                0.035f * (float)Math.Sin(a: Math.PI * u)
                + 0.015f * u * (float)Math.Sin(a: Math.PI * v)
            );

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
