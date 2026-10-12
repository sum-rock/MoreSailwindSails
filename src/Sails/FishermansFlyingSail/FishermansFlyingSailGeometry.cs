// Scales the baked Flying Sail and supplies its existing camber, skinning and Cloth rig.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansFlyingSail
{
    internal static class FishermansFlyingSailGeometry
    {
        internal const int Columns = 24;
        internal const int Rows = 32;
        internal const int ShapeColumns = 12;
        internal const int ShapeStride = Columns / ShapeColumns;
        internal const int BoneCount = (ShapeColumns + 1) * (Rows + 1);
        internal const float ForeDepthRatio = 2f;
        internal static readonly float EdgeSlope = (float)Math.Tan(20 * Math.PI / 180);

        // Sail mount frame: +X is up, -Z points toward the forward mast.
        // Corner order: top fore, top aft, bottom fore, bottom aft.
        internal static FishermansFlyingSailMeshData Create(float width)
        {
            if (float.IsNaN(width) || float.IsInfinity(width) || width < 0.25f || width > 100f)
                throw new ArgumentException(
                    "Expected a finite sail width between 0.25 and 100 metres."
                );
            var data = FishermansFlyingSailBakedMesh.Create();
            data.Weights = new BoneWeight[data.Vertices.Length];
            data.Constraints = new ClothSkinningCoefficient[data.Vertices.Length];
            for (int row = 0; row <= Rows; row++)
            for (int col = 0; col <= Columns; col++)
            {
                float u = (float)col / Columns,
                    v = (float)row / Rows;
                int i = row * (Columns + 1) + col;
                // The authored flat cut supplies the surface; preserve the live rig's spare fabric.
                data.Vertices[i] =
                    data.Vertices[i] * width
                    + Vector3.up * RestCamber(width: width, u: u, v: v)
                    + Vector3.back * RestLuffBow(width: width, u: u, v: v);
                int left = Math.Min(ShapeColumns - 1, col / ShapeStride);
                float blend = (float)(col - left * ShapeStride) / ShapeStride;
                data.Weights[i] = ShapeWeights(
                    ShapeBone(row, left),
                    ShapeBone(row, left + 1),
                    blend
                );
                // Only the four corners are attached. Intermediate edge
                // vertices can flex around their tension-fitted skin targets.
                bool pinned = (col == 0 || col == Columns) && (row == 0 || row == Rows);
                data.Constraints[i] = new ClothSkinningCoefficient
                {
                    maxDistance = pinned ? 0 : FishermansFlyingSailBillow.ClothTravel(width, u, v),
                    collisionSphereDistance = 0,
                };
            }
            data.Corners = new[]
            {
                data.Vertices[0],
                data.Vertices[Columns],
                data.Vertices[Rows * (Columns + 1)],
                data.Vertices[data.Vertices.Length - 1],
            };
            data.BonePositions = new Vector3[BoneCount];
            for (int row = 0; row <= Rows; row++)
            for (int column = 0; column <= ShapeColumns; column++)
                data.BonePositions[ShapeBone(row: row, column: column)] = data.Vertices[
                    row * (Columns + 1) + column * ShapeStride
                ];
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

        // A circular section carries spare fabric through the head and foot.
        // Sample on the bone grid so translation-only skinning reproduces it.
        internal static float RestCamber(float width, float u, float v)
        {
            if (u < 0 || u >= 1)
                return 0;
            int left = Math.Min(ShapeColumns - 1, (int)(u * ShapeColumns));
            float blend = u * ShapeColumns - left;
            return CamberSample(width, (float)left / ShapeColumns, v) * (1 - blend)
                + CamberSample(width, (float)(left + 1) / ShapeColumns, v) * blend;
        }

        private static float CamberSample(float width, float u, float v)
        {
            if (u <= 0 || u >= 1)
                return 0;
            const float depth = 0.20f;
            const float radius = 1 / (8 * depth) + depth / 2;
            float offset = u - 0.5f;
            return width * ((float)Math.Sqrt(radius * radius - offset * offset) - (radius - depth));
        }

        // The luff's spare fabric bends toward the mast on both tacks. Sample
        // on the shaping grid so translation-only skinning reconstructs it exactly.
        internal static float RestLuffBow(float width, float u, float v)
        {
            if (u < 0 || u >= 1 || v <= 0 || v >= 1)
                return 0;
            int left = Math.Min(ShapeColumns - 1, (int)(u * ShapeColumns));
            float blend = u * ShapeColumns - left;
            float taper = (float)(
                Math.Pow(1 - (float)left / ShapeColumns, 4) * (1 - blend)
                + Math.Pow(1 - (float)(left + 1) / ShapeColumns, 4) * blend
            );
            return Math.Min(width * 0.06f, FishermansFlyingSailFrameGeometry.TieLength * 0.5f)
                * taper
                * (float)Math.Sin(Math.PI * v);
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

        // 0 = hidden, 1 = procedural hoisting, 2 = fully deployed cloth.
        internal static int RenderState(float unroll) =>
            unroll <= 0.02f ? 0
            : unroll < 0.98f ? 1
            : 2;
    }
}
