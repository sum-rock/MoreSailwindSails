// Discovers registered native stays and tracks their controls, supports and occupancy.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls.Configuration;
using UnityEngine;

namespace MoreSailwindSails.Controls
{
    // One inventory per coordinator. Never activate source rigs or register masts.
    internal sealed class NativeWinchSeats
    {
        internal readonly BoatRefs Boat;
        internal readonly BoatControlDefinition Profile;
        private readonly Func<GPButtonRopeWinch, bool> owned;
        private readonly Dictionary<int, Quaternion> wheelFrames =
            new Dictionary<int, Quaternion>();
        private readonly HashSet<string> diagnostics = new HashSet<string>();
        private readonly WinchDiscoverySchedule discovery = new WinchDiscoverySchedule();
        private readonly Dictionary<int, Mast> mastsById = new Dictionary<int, Mast>();
        private readonly Dictionary<int, GPButtonRopeWinch> controlsById =
            new Dictionary<int, GPButtonRopeWinch>();
        private readonly Dictionary<int, WinchSeat> seatsById = new Dictionary<int, WinchSeat>();
        private readonly Dictionary<Mast, GPButtonRopeWinch[][]> arrays =
            new Dictionary<Mast, GPButtonRopeWinch[][]>();
        private BoatPartOption[] options = Array.Empty<BoatPartOption>();
        private int liveFrame = -1;
        internal long HierarchyScans { get; private set; }
        internal long CandidateBuilds { get; private set; }
        internal Mast[] Masts { get; private set; } = Array.Empty<Mast>();
        internal GPButtonRopeWinch[] Controls { get; private set; } =
            Array.Empty<GPButtonRopeWinch>();

        internal NativeWinchSeats(BoatRefs boat, Func<GPButtonRopeWinch, bool> owned)
        {
            Boat = boat;
            Profile = BoatControlCatalog.Find(boatName: boat.name);
            this.owned = owned;
        }

        internal void Invalidate() => discovery.Invalidate();

        // The coordinator calls this only while it owns controls. Bootstrap calls
        // it explicitly because native binding needs arrays before the first tick.
        internal void Prepare()
        {
            if (ArraysChanged() || Controls.Any(c => !c))
                discovery.Invalidate();
            if (discovery.Due(now: Time.unscaledTime))
                Refresh();
            if (liveFrame == Time.frameCount)
                return;
            liveFrame = Time.frameCount;
            if (
                Controls.Any(c =>
                    !c
                    || !seatsById.TryGetValue(c.GetInstanceID(), out var seat)
                    || !seat.MatchesPose(position: Position(c), rotation: Rotation(c))
                )
            )
                RebuildSeats();
        }

        internal void Refresh()
        {
            HierarchyScans += 2;
            Masts = Boat.GetComponentsInChildren<Mast>(true)
                .Where(m => m && m.orderIndex < 128)
                .ToArray();
            Controls = Boat.GetComponentsInChildren<GPButtonRopeWinch>(true)
                .Where(c => c && !owned(c))
                .ToArray();
            mastsById.Clear();
            foreach (var mast in Masts)
                if (
                    !mastsById.TryGetValue(mast.orderIndex, out var previous)
                    || (
                        !previous.GetComponent<BoatPartOption>()
                        && mast.GetComponent<BoatPartOption>()
                    )
                )
                    mastsById[mast.orderIndex] = mast;
            var parts = Boat.GetComponent<BoatCustomParts>();
            options = parts
                ? parts
                    .availableParts.SelectMany(part => part.partOptions)
                    .Where(option => option)
                    .ToArray()
                : Array.Empty<BoatPartOption>();
            controlsById.Clear();
            foreach (var control in Controls)
                controlsById[control.GetInstanceID()] = control;
            arrays.Clear();
            foreach (var mast in Masts)
                arrays[mast] = new[]
                {
                    Copy(mast.reefWinch),
                    Copy(mast.leftAngleWinch),
                    Copy(mast.rightAngleWinch),
                    Copy(mast.midAngleWinch),
                };
            foreach (
                int id in wheelFrames.Keys.Where(id => !controlsById.ContainsKey(id)).ToArray()
            )
                wheelFrames.Remove(id);
            RebuildSeats();
            liveFrame = Time.frameCount;
            discovery.Discovered(now: Time.unscaledTime);
        }

        private static GPButtonRopeWinch[] Copy(GPButtonRopeWinch[] controls) =>
            controls?.ToArray() ?? Array.Empty<GPButtonRopeWinch>();

        private static bool SameArray(GPButtonRopeWinch[] live, GPButtonRopeWinch[] snapshot) =>
            live == null ? snapshot.Length == 0 : live.SequenceEqual(snapshot);

        private bool ArraysChanged() =>
            arrays.Any(entry =>
                !entry.Key
                || !SameArray(entry.Key.reefWinch, entry.Value[0])
                || !SameArray(entry.Key.leftAngleWinch, entry.Value[1])
                || !SameArray(entry.Key.rightAngleWinch, entry.Value[2])
                || !SameArray(entry.Key.midAngleWinch, entry.Value[3])
            );

        private void RebuildSeats()
        {
            seatsById.Clear();
            var positions = Controls.Where(c => c).ToDictionary(c => c.GetInstanceID(), Position);
            foreach (var control in Controls)
            {
                if (!control)
                    continue;
                int id = control.GetInstanceID();
                var position = positions[id];
                var aliases = positions
                    .Where(p => (p.Value - position).sqrMagnitude <= 0.000001f)
                    .Select(p => (object)p.Key)
                    .ToArray();
                seatsById[id] = new WinchSeat(
                    identity: id,
                    aliases: aliases,
                    position: position,
                    rotation: Rotation(control)
                );
            }
        }

        internal Mast Mast(int id) => mastsById.TryGetValue(id, out var mast) ? mast : null;

        internal Mast PhysicalMast(Mast section) =>
            section && Profile != null && Profile.Boat.MastParents.ContainsKey(section.orderIndex)
                ? Mast(id: Profile.Boat.Base(section: section.orderIndex))
                : section;

        internal IEnumerable<Mast> StaySources(Mast requested)
        {
            if (!requested)
                yield break;
            foreach (var option in options)
            {
                if (!option)
                    continue;
                var source = option.GetComponent<Mast>();
                if (
                    !source
                    || !source.onlyStaysails
                    || Stays.FishermansStay.FishermansStayRegistry.TryFind(
                        mount: source,
                        stay: out _
                    )
                    || (source.sails != null && source.sails.Any(sail => sail))
                )
                    continue;
                if (
                    StayDependencies.Reaches(
                        option: option,
                        requires: current => current ? current.requires : null,
                        matches: dependency =>
                            dependency && dependency.GetComponent<Mast>() == requested
                    )
                )
                    yield return source;
            }
        }

        internal static GPButtonRopeWinch[] Sources(Mast mast, WinchRole role)
        {
            switch (role)
            {
                case WinchRole.Reef:
                    return mast ? mast.reefWinch : null;
                case WinchRole.Left:
                    return mast ? mast.leftAngleWinch : null;
                case WinchRole.Right:
                    return mast ? mast.rightAngleWinch : null;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(role),
                        role,
                        "Unsupported winch role."
                    );
            }
        }

        internal static bool Usable(GPButtonRopeWinch c) =>
            c && c.GetComponent<Renderer>() && c.GetComponent<Collider>();

        internal bool TemplateUsable(GPButtonRopeWinch control) =>
            Usable(control) && !owned(control);

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

        internal bool ActiveSupport(Mast mast)
        {
            if (!mast || !mast.gameObject.activeInHierarchy)
                return false;
            if (Profile == null || !Profile.Boat.MastParents.ContainsKey(mast.orderIndex))
                return true;
            return Profile
                .Boat.Sections(mast.orderIndex)
                .All(id =>
                {
                    var support = Mast(id);
                    return support && support.gameObject.activeInHierarchy;
                });
        }

        internal bool Mounted(GPButtonRopeWinch c)
        {
            if (!c || !c.transform.parent || !c.transform.parent.gameObject.activeInHierarchy)
                return false;
            var mast = c.GetComponentInParent<Mast>();
            if (mast && !ActiveSupport(mast))
                return false;
            var option = c.GetComponentInParent<BoatPartOption>();
            return !option || option.gameObject.activeInHierarchy;
        }

        internal WinchSeat Seat(GPButtonRopeWinch template) =>
            template && seatsById.TryGetValue(template.GetInstanceID(), out var seat) ? seat : null;

        // Native seats use cached aliases; authored positions check coincident native controls.
        internal bool Available(WinchSeat seat)
        {
            if (seat.Aliases.Length == 0)
                return !Controls.Any(control =>
                    control
                    && (Position(c: control) - seat.Position).sqrMagnitude <= 0.000001f
                    && Occupied(c: control)
                );
            foreach (var alias in seat.Aliases)
                if (
                    alias is int id
                    && controlsById.TryGetValue(id, out var control)
                    && Occupied(control)
                )
                    return false;
            return true;
        }

        internal bool Current(NativeWinchCandidate candidate)
        {
            for (int i = 0; i < candidate.Templates.Length; i++)
            {
                var template = candidate.Templates[i];
                if (!TemplateUsable(template) || !Mounted(template))
                    return false;
                var seat = Seat(template);
                if (
                    seat == null
                    || !seat.SamePose(candidate.Seats[i])
                    || !seat.Aliases.SequenceEqual(candidate.Seats[i].Aliases)
                    || !Available(seat)
                )
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
            CandidateBuilds++;
            if (templates.Any(t => !TemplateUsable(t) || Seat(t) == null))
                return null;
            var seats = templates.Select(Seat).ToArray();
            return new NativeWinchCandidate
            {
                Id = id,
                Context = context,
                Templates = templates,
                Seats = seats,
                Supported = templates.All(Mounted),
                Vacant = seats.All(Available),
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
