using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.Utils
{
    // Pure grouping and line geometry for the overlay, independently testable without Unity.
    internal static class WinchMountOverlayGeometry
    {
        internal static WinchMountStatus Classify(
            bool owned,
            bool visible,
            bool occupied,
            bool mounted
        ) =>
            owned && !visible ? WinchMountStatus.Hidden
            : occupied || owned ? WinchMountStatus.Occupied
            : mounted ? WinchMountStatus.Unused
            : WinchMountStatus.Unfitted;

        // Each connected group uses the native inventory's one-millimetre tolerance.
        // Scratch buffers are reused by the overlay; the output is a representative index.
        internal static void Group(
            Vector3[] positions,
            WinchMountStatus[] states,
            int[] groups,
            int[] best
        )
        {
            for (int i = 0; i < positions.Length; i++)
            {
                groups[i] = i;
                best[i] = -1;
                if (states[i] == WinchMountStatus.Hidden || !Finite(positions[i]))
                    continue;
                for (int j = 0; j < i; j++)
                    if (
                        states[j] != WinchMountStatus.Hidden
                        && (positions[i] - positions[j]).sqrMagnitude <= 0.000001f
                    )
                        groups[Root(groups: groups, index: i)] = Root(groups: groups, index: j);
            }
            for (int i = 0; i < positions.Length; i++)
            {
                if (states[i] == WinchMountStatus.Hidden || !Finite(positions[i]))
                    continue;
                int root = Root(groups: groups, index: i);
                if (best[root] < 0 || states[i] > states[best[root]])
                    best[root] = i;
            }
            // Resolve roots before replacing the union tree with representative indices.
            for (int i = 0; i < positions.Length; i++)
                groups[i] = Root(groups: groups, index: i);
            for (int i = 0; i < positions.Length; i++)
                groups[i] = best[groups[i]];
        }

        private static int Root(int[] groups, int index)
        {
            while (groups[index] != index)
                index = groups[index];
            return index;
        }

        private static bool Finite(Vector3 point) =>
            !float.IsNaN(point.x)
            && !float.IsInfinity(point.x)
            && !float.IsNaN(point.y)
            && !float.IsInfinity(point.y)
            && !float.IsNaN(point.z)
            && !float.IsInfinity(point.z);

        internal static Vector3[] MeshEdges(Vector3[] vertices, int[] triangles)
        {
            var lines = new List<Vector3>();
            var seen = new HashSet<long>();
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            for (int edge = 0; edge < 3; edge++)
            {
                int a = triangles[i + edge];
                int b = triangles[i + (edge + 1) % 3];
                if (a < 0 || b < 0 || a >= vertices.Length || b >= vertices.Length || a == b)
                    continue;
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (!seen.Add(key))
                    continue;
                lines.Add(vertices[a]);
                lines.Add(vertices[b]);
            }
            return lines.ToArray();
        }

        internal static Vector3[] BoundsEdges(Bounds bounds)
        {
            var lines = new List<Vector3>();
            var min = bounds.min;
            var max = bounds.max;
            for (int corner = 0; corner < 8; corner++)
            {
                var start = new Vector3(
                    (corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y,
                    (corner & 4) == 0 ? min.z : max.z
                );
                for (int axis = 0; axis < 3; axis++)
                {
                    if ((corner & (1 << axis)) != 0)
                        continue;
                    var end = start;
                    end[axis] = max[axis];
                    lines.Add(start);
                    lines.Add(end);
                }
            }
            return lines.ToArray();
        }
    }
}
