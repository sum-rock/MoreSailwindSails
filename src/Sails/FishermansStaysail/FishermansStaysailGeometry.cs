// Supplies attachment bones and identity skinning for fitted baked cuts.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal static class FishermansStaysailGeometry
    {
        internal const int Columns = 24;
        internal const int Rows = 32;
        internal const int BoneCount = 4;

        // Every positive deployment uses the authored panel, however small.
        internal static bool IsFurled(float unroll) => unroll <= 0f;

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
            data.BonePositions = (Vector3[])data.Corners.Clone();
            data.Weights = new BoneWeight[data.Vertices.Length];
            data.Constraints = new ClothSkinningCoefficient[data.Vertices.Length];
            for (int i = 0; i < data.Vertices.Length; i++)
                data.Weights[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1 };
            return data;
        }
    }
}
