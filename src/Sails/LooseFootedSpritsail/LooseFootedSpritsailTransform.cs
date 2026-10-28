// Provides managed affine math so fitting geometry can be executed without Unity's native engine.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal static class LooseFootedSpritsailTransform
    {
        internal static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s) =>
            new Matrix4x4(
                new Vector4(
                    x: (1 - 2 * (q.y * q.y + q.z * q.z)) * s.x,
                    y: 2 * (q.x * q.y + q.w * q.z) * s.x,
                    z: 2 * (q.x * q.z - q.w * q.y) * s.x,
                    w: 0
                ),
                new Vector4(
                    x: 2 * (q.x * q.y - q.w * q.z) * s.y,
                    y: (1 - 2 * (q.x * q.x + q.z * q.z)) * s.y,
                    z: 2 * (q.y * q.z + q.w * q.x) * s.y,
                    w: 0
                ),
                new Vector4(
                    x: 2 * (q.x * q.z + q.w * q.y) * s.z,
                    y: 2 * (q.y * q.z - q.w * q.x) * s.z,
                    z: (1 - 2 * (q.x * q.x + q.y * q.y)) * s.z,
                    w: 0
                ),
                new Vector4(x: pos.x, y: pos.y, z: pos.z, w: 1)
            );

        internal static Vector3 InversePoint(Matrix4x4 matrix, Vector3 point)
        {
            var a = new Vector3(x: matrix.m00, y: matrix.m10, z: matrix.m20);
            var b = new Vector3(x: matrix.m01, y: matrix.m11, z: matrix.m21);
            var c = new Vector3(x: matrix.m02, y: matrix.m12, z: matrix.m22);
            var v = point - new Vector3(x: matrix.m03, y: matrix.m13, z: matrix.m23);
            var bc = Vector3.Cross(lhs: b, rhs: c);
            float determinant = Vector3.Dot(lhs: a, rhs: bc);
            if (Math.Abs(determinant) < 1e-12f)
                throw new ArgumentException("Expected an invertible fitting transform.");
            return new Vector3(
                x: Vector3.Dot(lhs: v, rhs: bc) / determinant,
                y: Vector3.Dot(lhs: v, rhs: Vector3.Cross(lhs: c, rhs: a)) / determinant,
                z: Vector3.Dot(lhs: v, rhs: Vector3.Cross(lhs: a, rhs: b)) / determinant
            );
        }

        internal static Quaternion Rotation(Matrix4x4 matrix)
        {
            var s = Scale(matrix: matrix);
            float m00 = matrix.m00 / s.x,
                m10 = matrix.m10 / s.x,
                m20 = matrix.m20 / s.x;
            float m01 = matrix.m01 / s.y,
                m11 = matrix.m11 / s.y,
                m21 = matrix.m21 / s.y;
            float m02 = matrix.m02 / s.z,
                m12 = matrix.m12 / s.z,
                m22 = matrix.m22 / s.z;
            float trace = m00 + m11 + m22;
            if (trace > 0)
            {
                float t = (float)Math.Sqrt(trace + 1) * 2;
                return new Quaternion(
                    x: (m21 - m12) / t,
                    y: (m02 - m20) / t,
                    z: (m10 - m01) / t,
                    w: t / 4
                );
            }
            if (m00 > m11 && m00 > m22)
            {
                float t = (float)Math.Sqrt(1 + m00 - m11 - m22) * 2;
                return new Quaternion(
                    x: t / 4,
                    y: (m01 + m10) / t,
                    z: (m02 + m20) / t,
                    w: (m21 - m12) / t
                );
            }
            if (m11 > m22)
            {
                float t = (float)Math.Sqrt(1 + m11 - m00 - m22) * 2;
                return new Quaternion(
                    x: (m01 + m10) / t,
                    y: t / 4,
                    z: (m12 + m21) / t,
                    w: (m02 - m20) / t
                );
            }
            else
            {
                float t = (float)Math.Sqrt(1 + m22 - m00 - m11) * 2;
                return new Quaternion(
                    x: (m02 + m20) / t,
                    y: (m12 + m21) / t,
                    z: t / 4,
                    w: (m10 - m01) / t
                );
            }
        }

        internal static Vector3 Scale(Matrix4x4 matrix) =>
            new Vector3(
                x: new Vector3(x: matrix.m00, y: matrix.m10, z: matrix.m20).magnitude,
                y: new Vector3(x: matrix.m01, y: matrix.m11, z: matrix.m21).magnitude,
                z: new Vector3(x: matrix.m02, y: matrix.m12, z: matrix.m22).magnitude
            );

        internal static Quaternion FromTo(Vector3 from, Vector3 to)
        {
            from.Normalize();
            to.Normalize();
            float dot = Vector3.Dot(lhs: from, rhs: to);
            Vector3 cross;
            if (dot < -0.999999f)
            {
                cross = Vector3
                    .Cross(lhs: from, rhs: Math.Abs(from.x) < 0.8f ? Vector3.right : Vector3.up)
                    .normalized;
                return new Quaternion(x: cross.x, y: cross.y, z: cross.z, w: 0);
            }
            cross = Vector3.Cross(lhs: from, rhs: to);
            float w = 1 + dot;
            float length = (float)Math.Sqrt(cross.sqrMagnitude + w * w);
            return new Quaternion(
                x: cross.x / length,
                y: cross.y / length,
                z: cross.z / length,
                w: w / length
            );
        }
    }
}
