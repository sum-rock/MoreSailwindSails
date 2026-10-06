using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal sealed class FishermansStaysailMeshData
    {
        internal Vector3[] Vertices;
        internal Vector2[] UV;
        internal int[] Triangles;
        internal BoneWeight[] Weights;
        internal ClothSkinningCoefficient[] Constraints;
        internal Vector3[] Corners;
        internal Vector3[] BonePositions;
        internal Vector3 Center;
    }

    internal static class FishermansStaysailGeometry
    {
        internal const int Columns = 24;
        internal const int Rows = 32;
        internal const int ShapeColumns = 6;
        internal const int ShapeStride = Columns / ShapeColumns;
        internal const int BoneCount = (ShapeColumns + 1) * (Rows + 1);

        // Sail mount frame: +X is up, -Z points toward the forward mast.
        // Corner order: top fore, top aft, bottom fore, bottom aft.
        internal static FishermansStaysailMeshData Create(float width, Vector3[] corners)
        {
            if (float.IsNaN(width) || float.IsInfinity(width) || width < 0.25f || width > 100f)
                throw new ArgumentException(
                    "Expected a finite sail width between 0.25 and 100 metres."
                );
            if (corners[0].x <= corners[2].x || corners[1].x <= corners[3].x)
                throw new ArgumentException("Stay slope leaves no positive luff or leech.");
            var data = new FishermansStaysailMeshData
            {
                Vertices = new Vector3[(Columns + 1) * (Rows + 1)],
                UV = new Vector2[(Columns + 1) * (Rows + 1)],
                Weights = new BoneWeight[(Columns + 1) * (Rows + 1)],
                Constraints = new ClothSkinningCoefficient[(Columns + 1) * (Rows + 1)],
                Triangles = new int[Columns * Rows * 6],
                Corners = (Vector3[])corners.Clone(),
            };
            data.BonePositions = new Vector3[BoneCount];
            for (int row = 0; row <= Rows; row++)
            for (int column = 0; column <= ShapeColumns; column++)
            {
                float u = (float)column / ShapeColumns,
                    v = (float)row / Rows;
                data.BonePositions[ShapeBone(row, column)] = new Vector3(
                    Vector3
                        .Lerp(
                            Vector3.Lerp(corners[0], corners[1], u),
                            Vector3.Lerp(corners[2], corners[3], u),
                            v
                        )
                        .x,
                    RestCamber(width, u, v),
                    -width * (1 - u)
                );
            }
            for (int row = 0; row <= Rows; row++)
            for (int col = 0; col <= Columns; col++)
            {
                float u = (float)col / Columns,
                    v = (float)row / Rows;
                int i = row * (Columns + 1) + col;
                data.Vertices[i] = new Vector3(
                    Vector3
                        .Lerp(
                            Vector3.Lerp(corners[0], corners[1], u),
                            Vector3.Lerp(corners[2], corners[3], u),
                            v
                        )
                        .x,
                    RestCamber(width, u, v),
                    -width * (1 - u)
                );
                data.UV[i] = new Vector2(u, 1 - v);
                int left = Math.Min(ShapeColumns - 1, col / ShapeStride);
                float blend = (float)(col - left * ShapeStride) / ShapeStride;
                data.Weights[i] = ShapeWeights(
                    ShapeBone(row, left),
                    ShapeBone(row, left + 1),
                    blend
                );
                // The entire luff follows the fore mast. Aft corner targets are
                // controlled by sheets, never attached to the stay.
                bool pinned = col == 0 || (col == Columns && (row == 0 || row == Rows));
                data.Constraints[i] = new ClothSkinningCoefficient
                {
                    maxDistance = pinned ? 0 : FishermansStaysailBillow.ClothTravel(width, u, v),
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

        // Extra top-edge cloth is part of the rest mesh, rather than simulated
        // by stretching a straight panel between two fully separated corners.
        internal static float RestCamber(float width, float u, float v)
        {
            if (u < 0 || u >= 1 || v >= 1)
                return 0;
            int left = Math.Min(ShapeColumns - 1, (int)(u * ShapeColumns));
            float blend = u * ShapeColumns - left;
            return CamberSample(width, (float)left / ShapeColumns, v) * (1 - blend)
                + CamberSample(width, (float)(left + 1) / ShapeColumns, v) * blend;
        }

        private static float CamberSample(float width, float u, float v)
        {
            if (u >= 1 || v >= 1)
                return 0;
            float curve = (float)Math.Sin(Math.PI * u);
            // Rounded shoulders and a fuller belly, tapering to the foot.
            // The rest cut and both signed skin targets share this profile;
            // keep the luff and leech endpoints on their controlled curves.
            return width * (0.12f * curve * (1 - v) * (1 + 0.75f * v));
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
            column == ShapeColumns ? LeechBone(row)
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
                weight0 = Math.Max(blend, 1 - blend),
                weight1 = Math.Min(blend, 1 - blend),
                boneIndex0 = blend > 0.5f ? right : left,
                boneIndex1 = blend > 0.5f ? left : right,
            };

        // 0 = native resting bundle, 1 = animated panel, 2 = deployed Cloth.
        internal static int RenderState(float unroll) =>
            unroll < 0.04f ? 0
            : unroll < 0.98f ? 1
            : 2;
    }
}
