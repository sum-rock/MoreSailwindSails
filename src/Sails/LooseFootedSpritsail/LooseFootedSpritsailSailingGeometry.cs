// Computes mast-centered travel and the aerodynamic frame without Unity scene objects.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal static class LooseFootedSpritsailSailingGeometry
    {
        internal const float PowerMultiplier = 0.80f;

        internal static Matrix4x4 PivotRotation(Vector3 center, Vector3 axis, float degrees)
        {
            float halfAngle = degrees * (float)Math.PI / 360;
            var imaginary = axis.normalized * (float)Math.Sin(halfAngle);
            var rotation = new Quaternion(
                x: imaginary.x,
                y: imaginary.y,
                z: imaginary.z,
                w: (float)Math.Cos(halfAngle)
            );
            return LooseFootedSpritsailTransform.TRS(
                pos: center - rotation * center,
                q: rotation,
                s: Vector3.one
            );
        }

        internal static bool TryWindFrame(
            Vector3 throat,
            Vector3 tack,
            Vector3 peak,
            Vector3 clew,
            out LooseFootedSpritsailWindFrame frame
        )
        {
            frame = default;
            if (!Finite(throat) || !Finite(tack) || !Finite(peak) || !Finite(clew))
                return false;
            var axis = throat - tack;
            if (axis.sqrMagnitude < 1e-8f)
                return false;
            axis.Normalize();
            var chord = (peak + clew - throat - tack) * 0.5f;
            chord -= axis * Vector3.Dot(lhs: chord, rhs: axis);
            if (chord.sqrMagnitude < 1e-8f)
                return false;
            chord.Normalize();
            float first = Vector3.Cross(lhs: tack - throat, rhs: peak - throat).magnitude;
            float second = Vector3.Cross(lhs: tack - peak, rhs: clew - peak).magnitude;
            if (
                !LooseFootedSpritsailFittingGeometry.Finite(value: first + second)
                || first + second < 1e-8f
            )
                return false;
            frame = new LooseFootedSpritsailWindFrame
            {
                Axis = axis,
                Normal = Vector3.Cross(lhs: axis, rhs: chord).normalized,
                Center =
                    ((throat + tack + peak) * first + (peak + tack + clew) * second)
                    / (3 * (first + second)),
            };
            return true;
        }

        internal static Vector3 ForceDirection(Vector3 wind, Vector3 normal) =>
            Vector3.Dot(lhs: wind, rhs: normal) >= 0 ? normal : -normal;

        private static bool Finite(Vector3 point) =>
            LooseFootedSpritsailFittingGeometry.Finite(value: point.x)
            && LooseFootedSpritsailFittingGeometry.Finite(value: point.y)
            && LooseFootedSpritsailFittingGeometry.Finite(value: point.z);
    }

    internal struct LooseFootedSpritsailWindFrame
    {
        internal Vector3 Axis,
            Normal,
            Center;
    }
}
