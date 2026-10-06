using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail.MkA
{
    internal static class FishermansStaysailMkAGeometry
    {
        internal const float NominalSlope = 20f;
        internal const float FixedUpperHeadAngle = 14f;

        internal static FishermansStaysailMeshData Create(
            float width,
            float headSlope = NominalSlope
        )
        {
            if (
                float.IsNaN(headSlope)
                || float.IsInfinity(headSlope)
                || headSlope < 0
                || headSlope > 80
            )
                throw new ArgumentException(
                    "Expected a finite stay slope between 0 and 80 degrees."
                );
            float rise = width * (float)Math.Tan(headSlope * Math.PI / 180);
            float fall = width * (float)Math.Tan(NominalSlope * Math.PI / 180);
            return FishermansStaysailGeometry.Create(
                width,
                new[]
                {
                    new Vector3(0, 0, -width),
                    new Vector3(rise, 0, 0),
                    new Vector3(-width, 0, -width),
                    new Vector3(-width - fall, 0, 0),
                }
            );
        }
    }
}
