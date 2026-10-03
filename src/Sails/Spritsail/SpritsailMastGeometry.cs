using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Finds the rendered mast surface along a ray from its axis, including native taper.
    internal static class SpritsailMastGeometry
    {
        internal static bool TryDistance(
            Vector3[] vertices,
            int[] triangles,
            Vector3 origin,
            Vector3 direction,
            out float distance
        )
        {
            distance = float.PositiveInfinity;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var edge1 = vertices[triangles[i + 1]] - a;
                var edge2 = vertices[triangles[i + 2]] - a;
                var cross = Vector3.Cross(direction, edge2);
                float determinant = Vector3.Dot(edge1, cross);
                if (Math.Abs(determinant) < 1e-10f)
                    continue;
                var offset = origin - a;
                float u = Vector3.Dot(offset, cross) / determinant;
                if (u < -1e-5f || u > 1.00001f)
                    continue;
                var q = Vector3.Cross(offset, edge1);
                float v = Vector3.Dot(direction, q) / determinant;
                if (v < -1e-5f || u + v > 1.00001f)
                    continue;
                float t = Vector3.Dot(edge2, q) / determinant;
                if (t > 0)
                    distance = Math.Min(distance, t);
            }
            return SpritsailDeployment.Finite(value: distance);
        }
    }
}
