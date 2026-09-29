using System;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay
{
    // Seats collars against the rendered spar, including tips that fall below native capsule bounds.
    internal sealed class FishermansStaySparSurface
    {
        private readonly Vector3[] vertices;
        private readonly int[] triangles;
        private readonly Vector3 axis;
        private readonly float bottom,
            top;

        internal FishermansStaySparSurface(Vector3[] vertices, int[] triangles, Vector3 axis)
        {
            FishermansStayMeshRegions.Validate(vertices: vertices, triangles: triangles);
            this.vertices = vertices;
            this.triangles = triangles;
            this.axis = axis;
            bottom = triangles.Min(i => vertices[i].z);
            top = triangles.Max(i => vertices[i].z);
        }

        internal static FishermansStaySparSurface Taper(
            float bottom,
            float top,
            float bottomRadius,
            float topRadius,
            Vector3 axis
        )
        {
            var vertices = new Vector3[16];
            var triangles = new int[48];
            for (int i = 0; i < 8; i++)
            {
                double angle = i * Math.PI / 4;
                var radial = new Vector3((float)Math.Cos(angle), (float)Math.Sin(angle), 0);
                var center = new Vector3(axis.x, axis.y, 0);
                vertices[i] = center + radial * bottomRadius + Vector3.forward * bottom;
                vertices[i + 8] = center + radial * topRadius + Vector3.forward * top;
                int next = (i + 1) % 8;
                int[] face = { i, next, i + 8, next, next + 8, i + 8 };
                Array.Copy(face, 0, triangles, i * 6, face.Length);
            }
            return new FishermansStaySparSurface(
                vertices: vertices,
                triangles: triangles,
                axis: axis
            );
        }

        internal Vector3 Seat(Vector3 attachment, float halfHeight, out float radius)
        {
            const float inset = 0.005f;
            if (
                float.IsNaN(halfHeight)
                || float.IsInfinity(halfHeight)
                || halfHeight <= 0
                || top - bottom <= 2 * (halfHeight + inset)
                || float.IsNaN(attachment.sqrMagnitude)
                || float.IsInfinity(attachment.sqrMagnitude)
            )
                throw new ArgumentException("Collar cannot be seated on the rendered spar.");
            float height = Math.Max(
                bottom + halfHeight + inset,
                Math.Min(top - halfHeight - inset, attachment.z)
            );
            float low = height - halfHeight,
                high = height + halfHeight;
            float maximum = 0;
            void Include(Vector3 point)
            {
                var radial = new Vector3(point.x - axis.x, point.y - axis.y, 0);
                maximum = Math.Max(maximum, radial.magnitude);
            }
            for (int i = 0; i < triangles.Length; i += 3)
            for (int edge = 0; edge < 3; edge++)
            {
                var a = vertices[triangles[i + edge]];
                var b = vertices[triangles[i + (edge + 1) % 3]];
                if (a.z >= low && a.z <= high)
                    Include(a);
                if (Math.Abs(b.z - a.z) < 1e-8f)
                    continue;
                for (int planeIndex = 0; planeIndex < 2; planeIndex++)
                {
                    float t = ((planeIndex == 0 ? low : high) - a.z) / (b.z - a.z);
                    if (t >= 0 && t <= 1)
                        Include(a + (b - a) * t);
                }
            }
            if (maximum < 0.001f)
                throw new ArgumentException("Rendered spar has no surface at the collar height.");
            radius = maximum;
            return new Vector3(axis.x, axis.y, height);
        }
    }
}
