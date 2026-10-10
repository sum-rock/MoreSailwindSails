using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Snapshots triangle edges once, then finds the nearest rendered surface without query allocations.
    internal sealed class SpritsailMastGeometry
    {
        private readonly Triangle[] triangles;

        internal SpritsailMastGeometry(Vector3[] vertices, int[] triangles)
        {
            this.triangles = new Triangle[triangles.Length / 3];
            for (int i = 0; i < this.triangles.Length; i++)
            {
                var a = vertices[triangles[3 * i]];
                this.triangles[i] = new Triangle(
                    a: a,
                    edge1: vertices[triangles[3 * i + 1]] - a,
                    edge2: vertices[triangles[3 * i + 2]] - a
                );
            }
        }

        internal bool TryDistance(Vector3 origin, Vector3 direction, out float distance)
        {
            distance = float.PositiveInfinity;
            for (int i = 0; i < triangles.Length; i++)
            {
                ref readonly var triangle = ref triangles[i];
                // Keep the original Moller-Trumbore operation order, tolerances and
                // two-sided tests. Directions retain their transformed length.
                float crossX = direction.y * triangle.Edge2.z - direction.z * triangle.Edge2.y;
                float crossY = direction.z * triangle.Edge2.x - direction.x * triangle.Edge2.z;
                float crossZ = direction.x * triangle.Edge2.y - direction.y * triangle.Edge2.x;
                float determinant =
                    triangle.Edge1.x * crossX
                    + triangle.Edge1.y * crossY
                    + triangle.Edge1.z * crossZ;
                if (Math.Abs(determinant) < 1e-10f)
                    continue;
                float offsetX = origin.x - triangle.A.x;
                float offsetY = origin.y - triangle.A.y;
                float offsetZ = origin.z - triangle.A.z;
                float u = (offsetX * crossX + offsetY * crossY + offsetZ * crossZ) / determinant;
                if (u < -1e-5f || u > 1.00001f)
                    continue;
                float qX = offsetY * triangle.Edge1.z - offsetZ * triangle.Edge1.y;
                float qY = offsetZ * triangle.Edge1.x - offsetX * triangle.Edge1.z;
                float qZ = offsetX * triangle.Edge1.y - offsetY * triangle.Edge1.x;
                float v = (direction.x * qX + direction.y * qY + direction.z * qZ) / determinant;
                if (v < -1e-5f || u + v > 1.00001f)
                    continue;
                float t =
                    (triangle.Edge2.x * qX + triangle.Edge2.y * qY + triangle.Edge2.z * qZ)
                    / determinant;
                if (t > 0)
                    distance = Math.Min(distance, t);
            }
            return SpritsailDeployment.Finite(value: distance);
        }

        private readonly struct Triangle
        {
            internal readonly Vector3 A;
            internal readonly Vector3 Edge1;
            internal readonly Vector3 Edge2;

            internal Triangle(Vector3 a, Vector3 edge1, Vector3 edge2)
            {
                A = a;
                Edge1 = edge1;
                Edge2 = edge2;
            }
        }
    }
}
