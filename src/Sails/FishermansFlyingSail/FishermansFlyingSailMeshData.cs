// Owns the Flying Sail mesh arrays and rest rig data.
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansFlyingSail
{
    internal sealed class FishermansFlyingSailMeshData
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
