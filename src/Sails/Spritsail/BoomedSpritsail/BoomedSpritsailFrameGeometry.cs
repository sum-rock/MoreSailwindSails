using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Maps the neutral panel and collision sweep onto the carrying mast's axis.
    internal static class BoomedSpritsailFrameGeometry
    {
        internal const float MastGap = 0.08f;

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
            if (axis.sqrMagnitude < 0.000001f || !SpritsailDeployment.Finite(value: degrees))
                throw new ArgumentException(message: "Expected a finite mast axis and angle.");
            var direction = axis.normalized;
            var relative = point - pivot;
            float radians = degrees * (float)Math.PI / 180f;
            return pivot
                + relative * (float)Math.Cos(d: radians)
                + Vector3.Cross(direction, relative) * (float)Math.Sin(a: radians)
                + direction * Vector3.Dot(direction, relative) * (1 - (float)Math.Cos(d: radians));
        }
    }
}
