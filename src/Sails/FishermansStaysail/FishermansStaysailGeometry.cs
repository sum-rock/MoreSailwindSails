// Supplies attachment bones, identity skinning and centroids for fitted baked cuts.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal static class FishermansStaysailGeometry
    {
        internal const int Columns = 24;
        internal const int Rows = 32;
        internal const int BoneCount = 4;

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
    }
}
