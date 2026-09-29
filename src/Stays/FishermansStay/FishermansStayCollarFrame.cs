using System;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay
{
    // Measures a native coil's plane and opening so its original shape can surround a different spar.
    internal sealed class FishermansStayCollarFrame
    {
        internal readonly Vector3 Center,
            Right,
            Up,
            Axis;
        internal readonly float InnerRadius,
            HalfHeight;

        internal FishermansStayCollarFrame(Vector3[] points)
        {
            if (
                points.Length < 6
                || points.Any(p => float.IsNaN(p.sqrMagnitude) || float.IsInfinity(p.sqrMagnitude))
            )
                throw new ArgumentException("Collar has no finite ring geometry.");
            // Uniformly weight positions rather than the split vertices at material/normal seams.
            var unique = points.Distinct().ToArray();
            var mean = unique.Aggregate(Vector3.zero, (sum, point) => sum + point) / unique.Length;
            var covariance = new double[3, 3];
            foreach (var point in unique)
            {
                var d = point - mean;
                for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    covariance[i, j] += d[i] * d[j];
            }
            var basis = new double[3, 3];
            for (int i = 0; i < 3; i++)
                basis[i, i] = 1;
            // Jacobi diagonalization of the symmetric covariance matrix.
            for (int iteration = 0; iteration < 24; iteration++)
            {
                int p = 0,
                    q = 1;
                for (int i = 0; i < 3; i++)
                for (int j = i + 1; j < 3; j++)
                    if (Math.Abs(covariance[i, j]) > Math.Abs(covariance[p, q]))
                    {
                        p = i;
                        q = j;
                    }
                if (Math.Abs(covariance[p, q]) < 1e-12)
                    break;
                double angle =
                    0.5 * Math.Atan2(2 * covariance[p, q], covariance[q, q] - covariance[p, p]);
                double c = Math.Cos(angle),
                    s = Math.Sin(angle);
                for (int i = 0; i < 3; i++)
                {
                    double a = covariance[i, p],
                        b = covariance[i, q];
                    covariance[i, p] = c * a - s * b;
                    covariance[i, q] = s * a + c * b;
                    a = basis[i, p];
                    b = basis[i, q];
                    basis[i, p] = c * a - s * b;
                    basis[i, q] = s * a + c * b;
                }
                for (int i = 0; i < 3; i++)
                {
                    double a = covariance[p, i],
                        b = covariance[q, i];
                    covariance[p, i] = c * a - s * b;
                    covariance[q, i] = s * a + c * b;
                }
            }
            int smallest = Enumerable.Range(0, 3).OrderBy(i => covariance[i, i]).First();
            Axis = new Vector3(
                (float)basis[0, smallest],
                (float)basis[1, smallest],
                (float)basis[2, smallest]
            ).normalized;
            if (Axis.z < 0)
                Axis = -Axis;
            var reference = Math.Abs(Axis.x) < 0.9f ? Vector3.right : Vector3.up;
            Right = (reference - Axis * Vector3.Dot(reference, Axis)).normalized;
            Up = Vector3.Cross(Axis, Right).normalized;
            var projected = points.Select(p => Direction(p - mean)).ToArray();
            var low = new Vector3(
                projected.Min(p => p.x),
                projected.Min(p => p.y),
                projected.Min(p => p.z)
            );
            var high = new Vector3(
                projected.Max(p => p.x),
                projected.Max(p => p.y),
                projected.Max(p => p.z)
            );
            var middle = (low + high) * 0.5f;
            Center = mean + Right * middle.x + Up * middle.y + Axis * middle.z;
            HalfHeight = (high.z - low.z) * 0.5f;
            InnerRadius = points.Min(p =>
            {
                var local = Point(p);
                return (float)Math.Sqrt(local.x * local.x + local.y * local.y);
            });
            if (InnerRadius < 0.001f || HalfHeight < 0.001f || HalfHeight > (high.x - low.x))
                throw new ArgumentException("Native collar has no usable opening or axis.");
        }

        internal static Vector3 Contact(Vector3[] points, Matrix4x4 localToOwner, Vector3 toward)
        {
            if (points.Length == 0)
                throw new ArgumentException("Collar has no contact surface.");
            var center = localToOwner.MultiplyPoint3x4(Vector3.zero);
            var direction = (toward - center).normalized;
            var best = localToOwner.MultiplyPoint3x4(points[0]);
            float furthest = Vector3.Dot(best - center, direction);
            foreach (var point in points)
            {
                var candidate = localToOwner.MultiplyPoint3x4(point);
                float distance = Vector3.Dot(candidate - center, direction);
                if (distance > furthest)
                {
                    best = candidate;
                    furthest = distance;
                }
            }
            return best;
        }

        internal Vector3 Point(Vector3 point) => Direction(point - Center);

        internal Vector3 Direction(Vector3 direction) =>
            new Vector3(
                Vector3.Dot(direction, Right),
                Vector3.Dot(direction, Up),
                Vector3.Dot(direction, Axis)
            );
    }
}
