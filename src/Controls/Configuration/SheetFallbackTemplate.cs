// Defines the shared native sheet model and its fixed mounting frame.
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration
{
    internal static class SheetFallbackTemplate
    {
        internal static readonly string BoatName = BoatConfiguration
            .Boats
            .Kakam
            .Definition
            .BoatName;
        internal const int Mast = 5;
        internal const WinchRole Role = WinchRole.Left;
        internal const int Index = 0;

        // Installed Kakam angle winch, in boat coordinates before wheel input.
        // Its local +Z faces out from the mounting surface. The mesh rear depth
        // at unit scale is 0.044704; keep size and depth independent of the donor's motion.
        internal static readonly Quaternion Rotation = new Quaternion(
            -0.7015601f,
            -0.003845582f,
            -0.004052444f,
            0.7125886f
        );
        internal static readonly Vector3 Normal = Rotation * Vector3.forward;
        internal static readonly Vector3 Scale = Vector3.one;
        internal const float Offset = 0.044704f;
    }
}
