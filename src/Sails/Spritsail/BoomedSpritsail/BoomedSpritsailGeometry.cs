using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Generates the four-corner spritsail panel and its immutable rest skin.

    internal static class BoomedSpritsailGeometry
    {
        internal const int Columns = 24;
        internal const int Rows = 32;
        internal const int ShapeColumns = 12;
        internal const int ShapeStride = Columns / ShapeColumns;
        internal const int BoneCount = (ShapeColumns + 1) * (Rows + 1);

        // Sail mount frame: +X is up, -Z points toward the forward mast.
        // Corner order: top fore, top aft, bottom fore, bottom aft.
        internal static BoomedSpritsailMeshData Create(
            float width,
            BoomedSpritsailDefinition definition = null
        )
        {
            if (
                float.IsNaN(f: width)
                || float.IsInfinity(f: width)
                || width < 0.25f
                || width > 100f
            )
                throw new ArgumentException(
                    message: "Expected a finite sail width between 0.25 and 100 metres."
                );
            definition = definition ?? MkA.BoomedSpritsailMkA.Definition;
            var data = new BoomedSpritsailMeshData
            {
                Vertices = new Vector3[(Columns + 1) * (Rows + 1)],
                UV = new Vector2[(Columns + 1) * (Rows + 1)],
                Weights = new BoneWeight[(Columns + 1) * (Rows + 1)],
                Constraints = new ClothSkinningCoefficient[(Columns + 1) * (Rows + 1)],
                Triangles = new int[Columns * Rows * 6],
                Corners = definition.Corners(width: width),
            };
            width = data.Corners[3].z - data.Corners[2].z;
            data.BonePositions = new Vector3[BoneCount];
            for (int row = 0; row <= Rows; row++)
            for (int column = 0; column <= ShapeColumns; column++)
            {
                float u = (float)column / ShapeColumns,
                    v = (float)row / Rows;
                data.BonePositions[ShapeBone(row: row, column: column)] = RestPoint(
                    corners: data.Corners,
                    width: width,
                    u: u,
                    v: v
                );
            }
            for (int row = 0; row <= Rows; row++)
            for (int col = 0; col <= Columns; col++)
            {
                float u = (float)col / Columns,
                    v = (float)row / Rows;
                int i = row * (Columns + 1) + col;
                data.Vertices[i] = RestPoint(corners: data.Corners, width: width, u: u, v: v);
                data.UV[i] = new Vector2(u, 1 - v);
                int left = Math.Min(val1: ShapeColumns - 1, val2: col / ShapeStride);
                float blend = (float)(col - left * ShapeStride) / ShapeStride;
                data.Weights[i] = ShapeWeights(
                    left: ShapeBone(row: row, column: left),
                    right: ShapeBone(row: row, column: left + 1),
                    blend: blend
                );
                // Pin the complete luff and boom-supported foot, plus the peak.
                bool pinned = col == 0 || row == Rows || (col == Columns && row == 0);
                data.Constraints[i] = new ClothSkinningCoefficient
                {
                    maxDistance = pinned
                        ? 0
                        : BoomedSpritsailBillow.ClothTravel(width: width, u: u, v: v),
                    collisionSphereDistance = 0,
                };
                if (row == Rows || col == Columns)
                    continue;
                int t = (row * Columns + col) * 6;
                data.Triangles[t] = i;
                data.Triangles[t + 1] = i + Columns + 1;
                data.Triangles[t + 2] = i + 1;
                data.Triangles[t + 3] = i + 1;
                data.Triangles[t + 4] = i + Columns + 1;
                data.Triangles[t + 5] = i + Columns + 2;
            }
            // Area-weighted centroid, also used for the aerodynamic force point.
            float area = 0;
            for (int i = 0; i < data.Triangles.Length; i += 3)
            {
                var a = data.Vertices[data.Triangles[i]];
                var b = data.Vertices[data.Triangles[i + 1]];
                var c = data.Vertices[data.Triangles[i + 2]];
                float weight = Vector3.Cross(b - a, c - a).magnitude;
                data.Center += (a + b + c) * (weight / 3);
                area += weight;
            }
            data.Center /= area;
            return data;
        }

        private static Vector3 RestPoint(Vector3[] corners, float width, float u, float v) =>
            Vector3.Lerp(
                Vector3.Lerp(corners[0], corners[2], v),
                Vector3.Lerp(corners[1], corners[3], v),
                u
            )
            + Vector3.up
                * RestCamber(
                    width: width,
                    u: u,
                    v: v,
                    headReach: (corners[1].z - corners[0].z) / width
                );

        // Spare head fabric tapers to zero at the rigid foot.
        // Sample on the bone grid so translation-only skinning reproduces it.
        internal static float RestCamber(float width, float u, float v, float headReach = 1)
        {
            if (u < 0 || u >= 1)
                return 0;
            int left = Math.Min(val1: ShapeColumns - 1, val2: (int)(u * ShapeColumns));
            float blend = u * ShapeColumns - left;
            return CamberSample(
                    width: width,
                    u: (float)left / ShapeColumns,
                    v: v,
                    headReach: headReach
                ) * (1 - blend)
                + CamberSample(
                    width: width,
                    u: (float)(left + 1) / ShapeColumns,
                    v: v,
                    headReach: headReach
                ) * blend;
        }

        private static float CamberSample(float width, float u, float v, float headReach)
        {
            if (u <= 0 || u >= 1)
                return 0;
            const float depth = 0.12f;
            const float radius = 1 / (8 * depth) + depth / 2;
            float offset = u - 0.5f;
            return width
                * (1 - v)
                * (headReach + (1 - headReach) * v)
                * ((float)Math.Sqrt(d: radius * radius - offset * offset) - (radius - depth));
        }

        // Unity requires influences in descending order, with indices moving
        // alongside weights. In particular, a fully pinned corner must put its
        // one nonzero influence first rather than behind three zero weights.
        internal static int LeechBone(int row) =>
            row == 0 ? 1
            : row == Rows ? 3
            : row + 3;

        // Keep the original corner and leech indices for ropes and tension.
        internal static int ShapeBone(int row, int column) =>
            column == ShapeColumns ? LeechBone(row: row)
            : column == 0
                ? (
                    row == 0 ? 0
                    : row == Rows ? 2
                    : Rows + 3 + row - 1
                )
            : Rows + 3 + Rows - 1 + (column - 1) * (Rows + 1) + row;

        private static BoneWeight ShapeWeights(int left, int right, float blend) =>
            new BoneWeight
            {
                weight0 = Math.Max(val1: blend, val2: 1 - blend),
                weight1 = Math.Min(val1: blend, val2: 1 - blend),
                boneIndex0 = blend > 0.5f ? right : left,
                boneIndex1 = blend > 0.5f ? left : right,
            };

        // 0 = struck bundle (zero force), 1 = procedural reefing, 2 = fully deployed cloth.
        internal static int RenderState(float unroll) =>
            !SpritsailDeployment.Finite(value: unroll) || unroll <= 0.02f ? 0
            : unroll < 0.98f ? 1
            : 2;
    }
}
