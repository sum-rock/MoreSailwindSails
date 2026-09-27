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
        internal readonly float Radius;

        internal WinchSeat(
            object identity,
            object[] aliases,
            Vector3 position,
            Quaternion rotation,
            float radius
        )
        {
            Identity = identity;
            Aliases = aliases ?? Array.Empty<object>();
            Position = position;
            Rotation = rotation;
            Radius = radius;
        }

        internal bool Valid =>
            Identity != null
            && Radius > 0f
            && Finite(Position.x)
            && Finite(Position.y)
            && Finite(Position.z)
            && Finite(Rotation.x)
            && Finite(Rotation.y)
            && Finite(Rotation.z)
            && Finite(Rotation.w)
            && Finite(Radius)
            && Rotation.x * Rotation.x
                + Rotation.y * Rotation.y
                + Rotation.z * Rotation.z
                + Rotation.w * Rotation.w
                > 0.000001f;

        internal bool Conflicts(WinchSeat other) =>
            Equals(Identity, other.Identity)
            || Aliases.Contains(other.Identity)
            || other.Aliases.Contains(Identity)
            || Aliases.Intersect(other.Aliases).Any()
            || WinchReservations.Overlap(Position, Radius, other.Position, other.Radius);

        internal bool SamePose(WinchSeat other) =>
            Equals(Identity, other.Identity)
            && (Position - other.Position).sqrMagnitude <= 0.000001f
            && Math.Abs(Radius - other.Radius) <= 0.00001f
            && Math.Abs(Quaternion.Dot(Rotation, other.Rotation)) >= 0.999999f;

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
