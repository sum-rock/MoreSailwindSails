// Holds independent authored mesh parts and attachment metadata for any spritsail cut.
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal sealed class LooseFootedSpritsailAssetData
    {
        // Five rest-local cloth poses at Blender frames 0, 60, 120, 180 and 240.
        internal LooseFootedSpritsailReefPose[] ReefPoses;
        internal LooseFootedSpritsailMeshPart Sail;
        internal LooseFootedSpritsailMeshPart Struck;
        internal Vector3 StruckReefLineAttachment;
        internal Vector3 StruckSheetLineAttachment;
        internal LooseFootedSpritsailMeshPart Sprit;
        internal LooseFootedSpritsailMeshPart SnotterAssembly;
        internal LooseFootedSpritsailMeshPart RotationPlate;

        // Authored inner radius about the mast axis, measured from the snotter without its rod.
        internal float CollarInnerRadius;

        // Attachment positions are relative to their corresponding mesh part's origin.
        internal Vector3 ReefLineAttachment;
        internal Vector3 SheetLineAttachment;

        // These indices address Sail.Vertices, retaining the Blender cloth vertex order.
        internal int Tack;
        internal int Throat;
        internal int Peak;
        internal int Clew;
        internal int SheetVertex;
    }

    internal struct LooseFootedSpritsailReefPose
    {
        internal Vector3[] Vertices;
        internal Quaternion SpritRotation;
    }

    internal struct LooseFootedSpritsailMeshPart
    {
        // Converts part-local positions into the authored assembly frame, in metres.
        internal Matrix4x4 RestTransform;
        internal Vector3 BoundsMin;
        internal Vector3 BoundsMax;
        internal Vector3[] Vertices;
        internal Vector3[] Normals;
        internal Vector2[] UV;

        // Materials and triangle arrays have matching submesh indices.
        internal LooseFootedSpritsailMaterial[] Materials;
        internal int[][] SubmeshTriangles;
    }

    internal enum LooseFootedSpritsailMaterial
    {
        Cloth,
        Metal,
        DarkWood,
        Black,
    }
}
