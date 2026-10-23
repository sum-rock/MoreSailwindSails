// Supplies neutral fitting geometry and mast-relative transformations for staysails.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal static class FishermansStaysailFrameGeometry
    {
        internal static Vector3 ProjectOntoMast(Vector3 point, Vector3 forePoint, Vector3 axis) =>
            forePoint + axis * Vector3.Dot(point - forePoint, axis);

        internal static Vector3 ModelOffset(Vector3 pivot, Vector3 alignedHead) =>
            pivot - alignedHead;

        internal static bool PositionChanged(Vector3 a, Vector3 b) =>
            (a - b).sqrMagnitude > 0.000004f;

        internal static Vector3 RotateAroundMast(
            Vector3 point,
            Vector3 pivot,
            Vector3 axis,
            float degrees
        )
        {
            if (axis.sqrMagnitude < 0.000001f || float.IsNaN(degrees) || float.IsInfinity(degrees))
                throw new ArgumentException("Expected a mast axis and a finite sheet angle.");
            var direction = axis.normalized;
            var relative = point - pivot;
            float radians = degrees * (float)Math.PI / 180f;
            float cosine = (float)Math.Cos(radians),
                sine = (float)Math.Sin(radians);
            return pivot
                + relative * cosine
                + Vector3.Cross(direction, relative) * sine
                + direction * Vector3.Dot(direction, relative) * (1 - cosine);
        }
    }
}
