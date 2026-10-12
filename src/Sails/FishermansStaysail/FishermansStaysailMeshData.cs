// Owns the fitted mesh arrays and rig data for one staysail instance.
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
}
