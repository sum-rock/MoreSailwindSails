using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail
{
    // Fits the luff and clips collision strips to each mark's convex four-corner panel.
    internal static class LooseFootedSpritsailMastInstallationGeometry
    {
        internal static Vector3 AtHeight(Vector3 bottom, Vector3 top, float height)
        {
            if (!SpritsailDeployment.Finite(value: height) || top.y - bottom.y < 0.01f)
                throw new ArgumentException(
                    message: "Expected an upward mast section and finite height."
                );
            var point = bottom + (top - bottom) * ((height - bottom.y) / (top.y - bottom.y));
            point.y = height;
            return point;
        }

        internal static bool CollisionStrip(
            float width,
            int column,
            out Vector3 center,
            out Vector3 size,
            Vector3[] corners = null
        )
        {
            float start = (column + 0.05f) / LooseFootedSpritsailGeometry.Columns;
            float end = (column + 0.95f) / LooseFootedSpritsailGeometry.Columns;
            corners = corners ?? MkA.LooseFootedSpritsailMkA.Definition.Corners(width: width);
            float z0 = corners[0].z + width * start;
            float z1 = corners[0].z + width * end;
            Extent(corners: corners, z: z0, low: out float low0, high: out float high0);
            Extent(corners: corners, z: z1, low: out float low1, high: out float high1);
            float head = Math.Min(high0, high1);
            float foot = Math.Max(low0, low1);
            center = new Vector3((head + foot) * 0.5f, 0, (z0 + z1) * 0.5f);
            size = new Vector3(Math.Max(0.01f, head - foot - 0.1f), 0.05f, z1 - z0);
            return head - foot > 0.11f;
        }

        private static void Extent(Vector3[] corners, float z, out float low, out float high)
        {
            low = float.PositiveInfinity;
            high = float.NegativeInfinity;
            int[] order = { 0, 1, 3, 2, 0 };
            for (int i = 0; i < 4; i++)
            {
                var a = corners[order[i]];
                var b = corners[order[i + 1]];
                if (Math.Abs(b.z - a.z) < 1e-8f || z < Math.Min(a.z, b.z) || z > Math.Max(a.z, b.z))
                    continue;
                float x = a.x + (b.x - a.x) * ((z - a.z) / (b.z - a.z));
                low = Math.Min(low, x);
                high = Math.Max(high, x);
            }
        }
    }
}
