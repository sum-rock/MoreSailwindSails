// Computes mast-centered travel and authored sensor fitting without Unity scene objects.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal static class LooseFootedSpritsailSailingGeometry
    {
        internal const float PowerMultiplier = 0.80f;

        // Local +X airflow comes from port and presses cloth across the sprit.
        // The fitted frame's mirrored X axis reverses the bad tack when flipped.
        internal static float TackPowerMultiplier(Vector3 wind, Vector3 spritSide) =>
            Vector3.Dot(lhs: wind, rhs: spritSide) > 0 ? 0.90f : 1f;

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

        // Fit once after installation, resizing or flipping. Reflected frames retain
        // a proper sensor rotation: right follows the chord, forward follows the luff.
        internal static LooseFootedSpritsailWindCenter FitWindCenter(
            LooseFootedSpritsailWindCenter authored,
            Matrix4x4 sail
        )
        {
            var forward = sail.MultiplyVector(
                vector: authored.Rotation * Vector3.forward
            ).normalized;
            var right = sail.MultiplyVector(vector: authored.Rotation * Vector3.right);
            right = (right - forward * Vector3.Dot(lhs: right, rhs: forward)).normalized;
            var up = Vector3.Cross(lhs: forward, rhs: right);
            return new LooseFootedSpritsailWindCenter
            {
                Position = sail.MultiplyPoint3x4(point: authored.Position),
                Rotation = LooseFootedSpritsailTransform.Rotation(
                    matrix: new Matrix4x4(
                        new Vector4(x: right.x, y: right.y, z: right.z, w: 0),
                        new Vector4(x: up.x, y: up.y, z: up.z, w: 0),
                        new Vector4(x: forward.x, y: forward.y, z: forward.z, w: 0),
                        new Vector4(x: 0, y: 0, z: 0, w: 1)
                    )
                ),
            };
        }

        internal static Vector3 ForceDirection(Vector3 wind, Vector3 normal) =>
            Vector3.Dot(lhs: wind, rhs: normal) >= 0 ? normal : -normal;
    }
}
