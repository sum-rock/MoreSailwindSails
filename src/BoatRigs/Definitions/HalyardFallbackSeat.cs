using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    // Defines one measured reef-control seat on the group's requested physical mast.
    internal sealed class HalyardFallbackSeat
    {
        internal readonly Vector3 Contact,
            Normal,
            SourceNormal;
        internal readonly float Offset;
        internal readonly int TemplateIndex;

        internal HalyardFallbackSeat(
            Vector3 contact,
            Vector3 normal,
            Vector3 sourceNormal,
            float offset,
            int templateIndex
        )
        {
            Contact = contact;
            Normal = normal;
            SourceNormal = sourceNormal;
            Offset = offset;
            TemplateIndex = templateIndex;
        }

        internal bool Valid =>
            Finite(Contact)
            && Finite(Normal)
            && Finite(SourceNormal)
            && Normal.sqrMagnitude > 0.000001f
            && SourceNormal.sqrMagnitude > 0.000001f
            && !float.IsNaN(Offset)
            && !float.IsInfinity(Offset)
            && TemplateIndex >= 0;

        internal Vector3 Position => Contact + Normal.normalized * Offset;

        internal Quaternion Rotation(Quaternion templateRotation) =>
            Quaternion.FromToRotation(SourceNormal, Normal) * templateRotation;

        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x)
            && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y)
            && !float.IsInfinity(value.y)
            && !float.IsNaN(value.z)
            && !float.IsInfinity(value.z);
    }
}
