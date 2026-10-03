using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail
{
    // Carries the generated mesh, skin, constraints and corner data.
    internal sealed class LooseFootedSpritsailMeshData
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
}
