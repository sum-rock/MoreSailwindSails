using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using UnityEngine;

namespace MoreSailwindSails.Controls
{
    internal sealed class NativeWinchCandidate : WinchCandidate
    {
        internal GPButtonRopeWinch[] Templates;
        internal string Context;
    }

    // One inventory per coordinator. Never activate source rigs or register masts.
    internal sealed class NativeWinchSeats
    {
        internal readonly BoatRefs Boat;
        internal readonly BoatRigDefinition Profile;
        private readonly Func<GPButtonRopeWinch, bool> owned;
        private readonly Dictionary<int, Quaternion> wheelFrames =
            new Dictionary<int, Quaternion>();
        private readonly HashSet<string> diagnostics = new HashSet<string>();
        internal Mast[] Masts { get; private set; }
        internal GPButtonRopeWinch[] Controls { get; private set; }

        internal NativeWinchSeats(BoatRefs boat, Func<GPButtonRopeWinch, bool> owned)
        {
            Boat = boat;
            Profile = BoatRigCatalog.Find(boat.name);
            this.owned = owned;
            Refresh();
        }

        internal void Refresh()
        {
            Masts = Boat.GetComponentsInChildren<Mast>(true)
                .Where(m => m && m.orderIndex < 128)
                .ToArray();
            Controls = Boat.GetComponentsInChildren<GPButtonRopeWinch>(true)
                .Where(c => c && !owned(c))
                .ToArray();
        }

        internal Mast Mast(int id) =>
            Masts.FirstOrDefault(m => m.orderIndex == id && m.GetComponent<BoatPartOption>())
            ?? Masts.FirstOrDefault(m => m.orderIndex == id);

        internal static GPButtonRopeWinch[] Sources(Mast mast, WinchRole role) =>
            !mast ? null
            : role == WinchRole.Reef ? mast.reefWinch
            : role == WinchRole.Left ? mast.leftAngleWinch
            : role == WinchRole.Right ? mast.rightAngleWinch
            : mast.midAngleWinch;

        internal static bool Usable(GPButtonRopeWinch c) =>
            c && c.GetComponent<Renderer>() && c.GetComponent<Collider>();

        internal static bool Occupied(GPButtonRopeWinch c) =>
            c
            && (
                c.rope
                || (
                    c.gameObject.activeInHierarchy
                    && (
                        (c.GetComponent<Renderer>()?.enabled ?? false)
                        || (c.GetComponent<Collider>()?.enabled ?? false)
                    )
                )
            );

        internal Vector3 Position(GPButtonRopeWinch c) =>
            Boat.transform.InverseTransformPoint(c.transform.position);

        internal Quaternion Rotation(GPButtonRopeWinch c)
        {
            int id = c.GetInstanceID();
            if (!wheelFrames.TryGetValue(id, out var local))
                wheelFrames[id] = local = c.transform.localRotation;
            return Quaternion.Inverse(Boat.transform.rotation)
                * (c.transform.parent ? c.transform.parent.rotation : Quaternion.identity)
                * local;
        }

        internal float Radius(GPButtonRopeWinch c)
        {
            // Only previously measured Shroud side pins get the pin-pitch radius.
            var pin = Profile?.WinchMounts.FirstOrDefault(d =>
                d.PinNames != null && d.PinNames.Contains(c.name)
            );
            if (pin != null)
                return pin.PinRadius;
            var scale = c.transform.lossyScale;
            var bs = Boat.transform.lossyScale;
            float largest = Math.Max(
                Math.Abs(scale.x),
                Math.Max(Math.Abs(scale.y), Math.Abs(scale.z))
            );
            float smallest = Math.Min(Math.Abs(bs.x), Math.Min(Math.Abs(bs.y), Math.Abs(bs.z)));
            var sphere = c.GetComponent<SphereCollider>();
            if (sphere)
                return sphere.radius * largest / smallest + 0.01f;
            var mesh = c.GetComponent<MeshFilter>();
            return mesh && mesh.sharedMesh
                ? Vector3.Scale(mesh.sharedMesh.bounds.extents, scale).magnitude / smallest + 0.01f
                : 0.16f;
        }

        internal bool ActiveSupport(Mast mast)
        {
            if (!mast || !mast.gameObject.activeInHierarchy)
                return false;
            if (Profile == null || !Profile.MastParents.ContainsKey(mast.orderIndex))
                return true;
            return Profile
                .Sections(mast.orderIndex)
                .All(id =>
                {
                    var support = Mast(id);
                    return support && support.gameObject.activeInHierarchy;
                });
        }

        private bool Mounted(GPButtonRopeWinch c)
        {
            if (!c || !c.transform.parent || !c.transform.parent.gameObject.activeInHierarchy)
                return false;
            var mast = c.GetComponentInParent<Mast>();
            if (mast && !ActiveSupport(mast))
                return false;
            var option = c.GetComponentInParent<BoatPartOption>();
            return !option || option.gameObject.activeInHierarchy;
        }

        internal WinchSeat Seat(GPButtonRopeWinch template)
        {
            var position = Position(template);
            var aliases = Controls
                .Where(c => (Position(c) - position).sqrMagnitude <= 0.000001f)
                .Select(c => (object)c.GetInstanceID())
                .ToArray();
            return new WinchSeat(
                template.GetInstanceID(),
                aliases,
                position,
                Rotation(template),
                Radius(template)
            );
        }

        internal bool Clear(WinchSeat seat, bool borrow)
        {
            foreach (var c in Controls)
            {
                bool alias = seat.Aliases.Contains((object)c.GetInstanceID());
                if (borrow && alias)
                {
                    if (Occupied(c))
                        return false;
                    continue;
                }
                if (!c.gameObject.activeInHierarchy && !c.rope)
                    continue;
                if (WinchReservations.Overlap(seat.Position, seat.Radius, Position(c), Radius(c)))
                    return false;
            }
            return true;
        }

        internal NativeWinchCandidate Candidate(
            string id,
            string context,
            params GPButtonRopeWinch[] templates
        )
        {
            if (templates.Any(t => !Usable(t) || owned(t)))
                return null;
            var seats = templates.Select(Seat).ToArray();
            return new NativeWinchCandidate
            {
                Id = id,
                Context = context,
                Templates = templates,
                Seats = seats,
                Supported = templates.All(Mounted),
                Vacant = seats.All(s => Clear(s, true)),
            };
        }

        internal void Diagnose(string key, string message, bool error = false)
        {
            if (!diagnostics.Add(key))
                return;
            if (error)
                Plugin.Log.LogError(message);
            else
                Plugin.Log.LogWarning(message);
        }
    }
}
