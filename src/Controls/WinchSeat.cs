using System;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Controls
{
    // Pure, immutable allocation datum. Identities are native instance IDs or
    // authored fallback names; aliases include every native control at this seat.
    internal sealed class WinchSeat
    {
        internal readonly object Identity;
        internal readonly object[] Aliases;
        internal readonly Vector3 Position;
        internal readonly Quaternion Rotation;

        internal WinchSeat(object identity, object[] aliases, Vector3 position, Quaternion rotation)
        {
            Identity = identity;
            Aliases = aliases ?? Array.Empty<object>();
            Position = position;
            Rotation = rotation;
        }

        internal bool Valid =>
            Identity != null
            && Finite(Position.x)
            && Finite(Position.y)
            && Finite(Position.z)
            && Finite(Rotation.x)
            && Finite(Rotation.y)
            && Finite(Rotation.z)
            && Finite(Rotation.w)
            && Rotation.x * Rotation.x
                + Rotation.y * Rotation.y
                + Rotation.z * Rotation.z
                + Rotation.w * Rotation.w
                > 0.000001f;

        internal bool Conflicts(WinchSeat other) =>
            Equals(Identity, other.Identity)
            || Aliases.Contains(other.Identity)
            || other.Aliases.Contains(Identity)
            || Aliases.Intersect(other.Aliases).Any();

        internal bool SamePose(WinchSeat other) =>
            Equals(Identity, other.Identity)
            && (Position - other.Position).sqrMagnitude <= 0.000001f
            && Math.Abs(Quaternion.Dot(Rotation, other.Rotation)) >= 0.999999f;

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
