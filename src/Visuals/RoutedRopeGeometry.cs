using UnityEngine;

namespace MoreSailwindSails.Visuals
{
    // Builds a round rope surface along an already posed route; owns no sail or control mechanics.
    internal static class RoutedRopeGeometry
    {
        internal const int Sides = 6;
        private static readonly Vector2[] crossSection = CrossSection();

        private static Vector2[] CrossSection()
        {
            var circle = new Vector2[Sides];
            for (int side = 0; side < Sides; side++)
            {
                float angle = side * (2 * Mathf.PI / Sides);
                circle[side] = new Vector2(x: Mathf.Cos(f: angle), y: Mathf.Sin(f: angle));
            }
            return circle;
        }

        internal static int[] Triangles(int count)
        {
            var triangles = new int[(count - 1) * Sides * 6];
            int index = 0;
            for (int ring = 0; ring < count - 1; ring++)
            for (int side = 0; side < Sides; side++)
            {
                int a = ring * Sides + side;
                int b = ring * Sides + (side + 1) % Sides;
                triangles[index++] = a;
                triangles[index++] = b;
                triangles[index++] = a + Sides;
                triangles[index++] = b;
                triangles[index++] = b + Sides;
                triangles[index++] = a + Sides;
            }
            return triangles;
        }

        internal static bool Pose(
            Vector3[] points,
            float diameter,
            Vector3[] vertices,
            Vector3[] normals
        )
        {
            if (points.Length < 2 || !Finite(value: diameter) || diameter <= 0)
                return false;
            foreach (var point in points)
                if (!Finite(value: (point - points[0]).sqrMagnitude))
                    return false;

            var previous = Vector3.zero;
            for (int i = 1; i < points.Length && previous.sqrMagnitude < 1e-10f; i++)
                previous = points[i] - points[0];
            if (previous.sqrMagnitude < 1e-10f)
                return false;
            previous.Normalize();
            var normal = Perpendicular(direction: previous);
            for (int ring = 0; ring < points.Length; ring++)
            {
                var before = ring > 0 ? points[ring] - points[ring - 1] : Vector3.zero;
                var after =
                    ring + 1 < points.Length ? points[ring + 1] - points[ring] : Vector3.zero;
                var tangent = before.normalized + after.normalized;
                if (tangent.sqrMagnitude < 1e-10f)
                    tangent = after.sqrMagnitude > 1e-10f ? after : previous;
                tangent.Normalize();
                // Parallel transport without engine-only Quaternion calls; an exact reversal
                // keeps the normal as its half-turn axis instead of dividing by zero.
                var turn = Vector3.Cross(lhs: previous, rhs: tangent);
                float cosine = Vector3.Dot(lhs: previous, rhs: tangent);
                if (cosine > -0.9999f)
                    normal +=
                        Vector3.Cross(lhs: turn, rhs: normal)
                        + Vector3.Cross(lhs: turn, rhs: Vector3.Cross(lhs: turn, rhs: normal))
                            / (1 + cosine);
                normal = Vector3.ProjectOnPlane(vector: normal, planeNormal: tangent).normalized;
                if (normal.sqrMagnitude < 1e-10f)
                    normal = Perpendicular(direction: tangent);
                var binormal = Vector3.Cross(lhs: tangent, rhs: normal);
                for (int side = 0; side < Sides; side++)
                {
                    var radial = normal * crossSection[side].x + binormal * crossSection[side].y;
                    int index = ring * Sides + side;
                    // Keep mesh coordinates near the origin even far from the starting island.
                    vertices[index] = points[ring] - points[0] + radial * (diameter * 0.5f);
                    normals[index] = radial;
                }
                previous = tangent;
            }
            return true;
        }

        private static Vector3 Perpendicular(Vector3 direction) =>
            Vector3
                .Cross(
                    lhs: direction,
                    rhs: Mathf.Abs(f: direction.y) < 0.9f ? Vector3.up : Vector3.right
                )
                .normalized;

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
