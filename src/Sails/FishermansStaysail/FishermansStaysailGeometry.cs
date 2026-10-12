// Supplies shared skinning, constraints, camber and centroids for fitted baked cuts.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal static class FishermansStaysailGeometry
    {
        internal const int Columns = 24;
        internal const int Rows = 32;
        internal const int ShapeColumns = 6;
        internal const int ShapeStride = Columns / ShapeColumns;
        internal const int BoneCount = (ShapeColumns + 1) * (Rows + 1);

        internal static void ValidateWidth(float width)
        {
            if (float.IsNaN(width) || float.IsInfinity(width) || width < 0.25f || width > 100f)
                throw new ArgumentException(
                    "Expected a finite sail width between 0.25 and 100 metres."
                );
        }

        // Fitted baked cuts arrive with their final rest surface.
        internal static FishermansStaysailMeshData Finish(
            float width,
            FishermansStaysailMeshData data
        )
        {
            var corners = data.Corners;
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
                    RestCamber(width: width, u: u, v: v),
                    -width * (1 - u)
                );
            }
            data.Weights = new BoneWeight[data.Vertices.Length];
            data.Constraints = new ClothSkinningCoefficient[data.Vertices.Length];
            for (int row = 0; row <= Rows; row++)
            for (int col = 0; col <= Columns; col++)
            {
                float u = (float)col / Columns,
                    v = (float)row / Rows;
                int i = row * (Columns + 1) + col;
                int left = Math.Min(ShapeColumns - 1, col / ShapeStride);
                float blend = (float)(col - left * ShapeStride) / ShapeStride;
                data.Weights[i] = ShapeWeights(
                    left: ShapeBone(row, left),
                    right: ShapeBone(row, left + 1),
                    blend: blend
                );
                // The entire luff and the two sheet-controlled aft corners are pinned.
                bool pinned = col == 0 || (col == Columns && (row == 0 || row == Rows));
                data.Constraints[i] = new ClothSkinningCoefficient
                {
                    maxDistance = pinned ? 0 : FishermansStaysailBillow.ClothTravel(width, u, v),
                    collisionSphereDistance = 0,
                };
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
