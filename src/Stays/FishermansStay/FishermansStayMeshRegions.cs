using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay
{
    // Identifies the rope and both native collars without relying on donor vertex ordering.
    internal static class FishermansStayMeshRegions
    {
        internal const int Rope = 0,
            Fore = 1,
            Aft = 2;

        internal static int[] Partition(Vector3[] vertices, int[] triangles)
        {
            Validate(vertices: vertices, triangles: triangles);
            var parents = Enumerable.Range(0, vertices.Length).ToArray();
            var used = new bool[vertices.Length];
            foreach (int index in triangles)
                used[index] = true;
            // Weld only for connectivity. Original indices, normals and UV seams survive.
            for (int i = 0; i < vertices.Length; i++)
                if (used[i])
                    for (int j = 0; j < i; j++)
                        if (used[j] && (vertices[i] - vertices[j]).sqrMagnitude <= 1e-8f)
                            parents[Find(parents, i)] = Find(parents, j);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int root = Find(parents, triangles[i]);
                parents[Find(parents, triangles[i + 1])] = root;
                parents[Find(parents, triangles[i + 2])] = root;
            }
            var groups = Enumerable
                .Range(0, vertices.Length)
                .Where(i => used[i])
                .GroupBy(i => Find(parents, i))
                .ToArray();
            var body = groups
                .OrderByDescending(g => g.Max(i => vertices[i].z) - g.Min(i => vertices[i].z))
                .First();
            float min = body.Min(i => vertices[i].z),
                max = body.Max(i => vertices[i].z);
            if (max - min < 0.25f)
                throw new ArgumentException("Stay has no longitudinal rope body.");
            var result = Enumerable.Repeat(-1, vertices.Length).ToArray();
            foreach (var group in groups)
            {
                float low = group.Min(i => vertices[i].z),
                    high = group.Max(i => vertices[i].z);
                int region = Rope;
                if (group.Key != body.Key)
                {
                    if (high - low > (max - min) * 0.25f)
                        throw new ArgumentException(
                            "Stay has ambiguous longitudinal mesh regions."
                        );
                    float center = (low + high) * 0.5f;
                    if (center > min + (max - min) * 0.25f && center < max - (max - min) * 0.25f)
                        throw new ArgumentException(
                            "Stay has unexpected geometry between its collars."
                        );
                    region = center < (min + max) * 0.5f ? Fore : Aft;
                }
                foreach (int index in group)
                    result[index] = region;
            }
            if (!result.Contains(Fore) || !result.Contains(Aft))
                throw new ArgumentException("Combined stay mesh must contain both collars.");
            return result;
        }

        internal static int[] Select(int[] triangles, int[] regions, int region)
        {
            if (triangles.Length % 3 != 0)
                throw new ArgumentException("Stay mesh must contain triangles.");
            var selected = new List<int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i],
                    b = triangles[i + 1],
                    c = triangles[i + 2];
                if (
                    a < 0
                    || b < 0
                    || c < 0
                    || a >= regions.Length
                    || b >= regions.Length
                    || c >= regions.Length
                )
                    throw new ArgumentException("Stay mesh has an invalid index.");
                if (regions[a] != regions[b] || regions[a] != regions[c])
                    throw new ArgumentException("A stay triangle crosses attachment regions.");
                if (regions[a] == region)
                {
                    selected.Add(a);
                    selected.Add(b);
                    selected.Add(c);
                }
            }
            return selected.ToArray();
        }

        internal static void Validate(Vector3[] vertices, int[] triangles)
        {
            if (
                vertices.Length == 0
                || triangles.Length == 0
                || triangles.Length % 3 != 0
                || vertices.Any(v => !Finite(v.x) || !Finite(v.y) || !Finite(v.z))
                || triangles.Any(i => i < 0 || i >= vertices.Length)
            )
                throw new ArgumentException("Invalid native stay mesh.");
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static int Find(int[] parents, int index)
        {
            while (parents[index] != index)
            {
                parents[index] = parents[parents[index]];
                index = parents[index];
            }
            return index;
        }
    }
}
