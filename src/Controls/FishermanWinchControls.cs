using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Sails;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Controls
{
    // One coordinator per boat; inactive clones never reserve a position.
    internal sealed class FishermanWinchControls : MonoBehaviour
    {
        private readonly WinchReservations reservations = new WinchReservations();
        private readonly List<OwnedWinch> controls = new List<OwnedWinch>();
        private BoatRefs boat;

        internal static OwnedWinch Create(
            BoatRefs boat,
            GameObject owner,
            Transform parent,
            Mast donorMast,
            WinchRole role,
            string label
        )
        {
            var manager =
                boat.GetComponent<FishermanWinchControls>()
                ?? boat.gameObject.AddComponent<FishermanWinchControls>();
            manager.boat = boat;
            var source = Source(boat, donorMast, role);
            if (!source)
                throw new InvalidOperationException("No usable source winch: " + label);
            var definition =
                BoatRigCatalog.Find(boat.name)?.WinchMount(donorMast.orderIndex, role)
                ?? throw new InvalidOperationException(
                    $"No authored winch mounting direction: {boat.name}/{donorMast.orderIndex}/{role}."
                );
            var control = new OwnedWinch(manager, owner, parent, source, definition, label);
            manager.controls.Add(control);
            return control;
        }

        internal static GPButtonRopeWinch Source(BoatRefs boat, Mast mast, WinchRole role)
        {
            var sources = Sources(mast, role);
            // Optional native roles (for example a stay's middle sheet) can have
            // no control and no mounting definition. Keep those probes harmless.
            var first = sources?.FirstOrDefault(Usable);
            if (!first)
                return null;
            var definition = BoatRigCatalog.Find(boat.name)?.WinchMount(mast.orderIndex, role);
            if (definition != null && definition.SourceMast >= 0)
            {
                // Stay construction can precede the donor's Awake/RegisterMast.
                // Include inactive native options without activating them or
                // relying on the live boat.masts array being populated yet.
                var sourceMast = boat.GetComponentsInChildren<Mast>(true)
                    .FirstOrDefault(m => m && m.orderIndex == definition.SourceMast);
                sources = Sources(sourceMast, role);
                first = sources?.FirstOrDefault(Usable);
            }
            int index = definition?.SourceIndex ?? -1;
            if (index < 0)
                return first;
            // Do not silently switch back to a blocked row if an authored donor
            // is missing. Creation reports the unusable source and can be retried.
            var source = sources != null && index < sources.Length ? sources[index] : null;
            return Usable(source) ? source : null;
        }

        private static GPButtonRopeWinch[] Sources(Mast mast, WinchRole role) =>
            !mast ? null
            : role == WinchRole.Reef ? mast.reefWinch
            : role == WinchRole.Left ? mast.leftAngleWinch
            : role == WinchRole.Right ? mast.rightAngleWinch
            : mast.midAngleWinch;

        private static bool Usable(GPButtonRopeWinch winch) =>
            winch && winch.GetComponent<Renderer>() && winch.GetComponent<Collider>();

        internal static void Reconcile(
            ref OwnedWinch[] current,
            BoatRefs boat,
            GameObject owner,
            Transform parent,
            Mast[] donors,
            string[] labels
        )
        {
            var roles = new[] { WinchRole.Reef, WinchRole.Left, WinchRole.Right };
            var next = new OwnedWinch[roles.Length];
            try
            {
                for (int i = 0; i < next.Length; i++)
                    next[i] =
                        current != null
                        && !current[i].IsDisposed
                        && current[i].Winch
                        && current[i].Source == Source(boat, donors[i], roles[i])
                            ? current[i]
                            : Create(boat, owner, parent, donors[i], roles[i], labels[i]);
            }
            catch
            {
                foreach (var control in next)
                    if (control != null && (current == null || !current.Contains(control)))
                        control.Dispose();
                throw;
            }
            if (current != null)
                foreach (var control in current)
                    if (!next.Contains(control))
                        control.Dispose();
            current = next;
        }

        private void LateUpdate()
        {
            ReleaseUnused();
            foreach (var control in controls)
                if (control.Wanted)
                    control.Refresh();
        }

        private void ReleaseUnused()
        {
            // Release first so inactive variants cannot block newly installed controls.
            foreach (var control in controls.ToArray())
                if (!control.Owner || !control.Winch || !control.Source)
                    control.Dispose();
                else if (!control.Wanted)
                    control.Suspend();
        }

        private bool Obstructed(
            Vector3 position,
            float radius,
            GPButtonRopeWinch donor,
            GPButtonRopeWinch[] pinTargets = null
        )
        {
            // Include unused native fittings on active parts: fitting another native
            // sail must not put its controls through ours. Include the donor even if
            // its source stay is not installed. Authored pin banks alone can borrow
            // hidden native coils and use clearance matching the existing pin pitch.
            foreach (var native in boat.GetComponentsInChildren<GPButtonRopeWinch>(true))
            {
                if (
                    !native
                    || controls.Any(c => c.Winch == native)
                    || (native != donor && !native.gameObject.activeInHierarchy)
                )
                    continue;
                bool bankPin = pinTargets != null && pinTargets.Contains(native);
                if (bankPin && !PinOccupied(native))
                    continue;
                if (
                    WinchReservations.Overlap(
                        position,
                        radius,
                        boat.transform.InverseTransformPoint(native.transform.position),
                        bankPin ? radius : Radius(native)
                    )
                )
                    return true;
            }
            return false;
        }

        private static bool PinOccupied(GPButtonRopeWinch pin) =>
            pin
            && pin.gameObject.activeInHierarchy
            && (pin.GetComponent<Renderer>().enabled || pin.GetComponent<Collider>().enabled);

        private float Radius(GPButtonRopeWinch winch)
        {
            // The native interaction sphere covers the rotating face. Mesh-AABB
            // diagonals overestimate a wheel and unnecessarily exhaust mast positions.
            var scale = winch.transform.lossyScale;
            var boatScale = boat.transform.lossyScale;
            float largest = Math.Max(
                Math.Abs(scale.x),
                Math.Max(Math.Abs(scale.y), Math.Abs(scale.z))
            );
            float smallest = Math.Min(
                Math.Abs(boatScale.x),
                Math.Min(Math.Abs(boatScale.y), Math.Abs(boatScale.z))
            );
            var sphere = winch.GetComponent<SphereCollider>();
            if (sphere)
                return sphere.radius * largest / smallest + 0.01f;
            var mesh = winch.GetComponent<MeshFilter>();
            if (mesh && mesh.sharedMesh)
                return Vector3.Scale(mesh.sharedMesh.bounds.extents, scale).magnitude / smallest
                    + 0.01f;
            return 0.16f;
        }

        private void OnDestroy()
        {
            foreach (var control in controls.ToArray())
                control.Dispose();
        }

        internal sealed class OwnedWinch : IDisposable
        {
            internal readonly GameObject Owner;
            internal readonly GPButtonRopeWinch Source;
            internal GPButtonRopeWinch Winch { get; private set; }
            private readonly FishermanWinchControls manager;
            private readonly WinchMountDefinition definition;
            private readonly Transform mount,
                sourceFrame;
            private readonly Quaternion frameRotation;
            private readonly GPButtonRopeWinch[] pinTargets;
            private float radius;
            private WinchReservations.Reservation reservation;
            private bool disposed,
                warned,
                hasLoggedPlacement,
                placementFailed;
            private float retryAfter;
            private Vector3 lastSourcePosition;
            private Quaternion placementRotation = Quaternion.identity;
            internal bool IsDisposed => disposed;

            internal bool Wanted =>
                !disposed && Owner && Owner.activeInHierarchy && Winch && Winch.rope;

            internal OwnedWinch(
                FishermanWinchControls manager,
                GameObject owner,
                Transform parent,
                GPButtonRopeWinch source,
                WinchMountDefinition definition,
                string label
            )
            {
                this.manager = manager;
                this.definition = definition;
                Owner = owner;
                Source = source;
                if (definition.PinNames != null)
                {
                    var natives = manager.boat.GetComponentsInChildren<GPButtonRopeWinch>(true);
                    pinTargets = definition
                        .PinNames.Select(name =>
                            natives.FirstOrDefault(n => n.name == name && Usable(n))
                        )
                        .Where(n => n)
                        .ToArray();
                }
                sourceFrame = source.transform.parent;
                frameRotation = source.transform.localRotation;
                radius = manager.Radius(source);
                var root = new GameObject(label + " mount");
                root.SetActive(false);
                mount = root.transform;
                mount.SetParent(parent, false);
                try
                {
                    var clone = Object.Instantiate(source.gameObject, mount, false);
                    clone.name = label;
                    Winch = clone.GetComponent<GPButtonRopeWinch>();
                    // Awake must see an owned rope, never the donor's live controller.
                    Winch.rope = null;
                    FishermanWinchVisuals.ResetClonedOutline(Winch);
                    clone.transform.localPosition = Vector3.zero;
                    clone.transform.localRotation = Quaternion.identity;
                    if (Winch.rotHandle && !Winch.rotHandle.transform.IsChildOf(clone.transform))
                    {
                        Winch.rotHandle = Object.Instantiate(
                            Winch.rotHandle,
                            clone.transform,
                            false
                        );
                        Winch.rotHandle.transform.localPosition = Vector3.zero;
                    }
                    if (Winch.rotHandle)
                        Winch.rotHandle.rotatable = clone.transform;
                    clone.SetActive(true);
                    Position(source.transform.position);
                }
                catch
                {
                    Object.Destroy(root);
                    throw;
                }
            }

            internal void Bind(RopeController controller)
            {
                Winch.AttachToController(controller);
                manager.ReleaseUnused();
                // A part refresh may enable a nearby native fitting while keeping
                // our donor. Keep stable reservations unless that fitting obstructs one.
                if (
                    reservation != null
                    && manager.Obstructed(reservation.Position, radius, Source, pinTargets)
                )
                {
                    Suspend();
                    if (pinTargets != null)
                        hasLoggedPlacement = false;
                }
                retryAfter = 0f;
                Refresh();
            }

            internal void Refresh()
            {
                if (!Wanted)
                {
                    Suspend();
                    return;
                }
                // Native sails take precedence over borrowed pins. Check the
                // selected native coil each refresh, including after shipyard changes.
                if (
                    reservation != null
                    && pinTargets != null
                    && (!pinTargets[reservation.Slot] || PinOccupied(pinTargets[reservation.Slot]))
                )
                {
                    Suspend();
                    retryAfter = 0f;
                    hasLoggedPlacement = false;
                }
                var origin = manager.boat.transform.InverseTransformPoint(
                    Source.transform.position
                );
                float currentRadius =
                    pinTargets == null ? manager.Radius(Source) : definition.PinRadius;
                if (
                    reservation != null
                    && (
                        (origin - lastSourcePosition).sqrMagnitude > 0.000001f
                        || Math.Abs(currentRadius - radius) > 0.00001f
                    )
                )
                {
                    manager.reservations.Release(this);
                    reservation = null;
                }
                radius = currentRadius;
                if (reservation == null)
                {
                    if (warned && Time.unscaledTime < retryAfter)
                        return;
                    var axisPoint = origin;
                    if (definition.OnMast)
                    {
                        var mast =
                            definition.Support < manager.boat.masts.Length
                                ? manager.boat.masts[definition.Support]
                                : null;
                        var support = mast ? mast.GetComponent<CapsuleCollider>() : null;
                        if (!support)
                        {
                            Suspend();
                            placementFailed = true;
                            if (!warned)
                                Plugin.Log.LogWarning(
                                    "Missing authored winch support "
                                        + definition.Support
                                        + " for "
                                        + mount.name
                                        + "; "
                                        + PlacementContext(origin)
                                );
                            warned = true;
                            retryAfter = Time.unscaledTime + 1f;
                            return;
                        }
                        axisPoint = manager.boat.transform.InverseTransformPoint(
                            support.transform.TransformPoint(support.center)
                        );
                    }
                    var placements =
                        pinTargets != null
                            ? PinPlacements()
                            : WinchPlacementGeometry.Candidates(
                                definition,
                                origin,
                                radius,
                                axisPoint
                            );
                    var positions = placements.Select(p => p.Position).ToArray();
                    var rejections = warned ? null : new WinchReservations.Rejections();
                    reservation = manager.reservations.Acquire(
                        Source,
                        this,
                        positions,
                        radius,
                        (p, r) => manager.Obstructed(p, r, Source, pinTargets),
                        rejections
                    );
                    lastSourcePosition = origin;
                    if (reservation != null)
                        placementRotation = placements[reservation.Slot].Rotation;
                    if (reservation == null)
                    {
                        Suspend();
                        placementFailed = true;
                        if (!warned)
                            Plugin.Log.LogWarning(
                                "No free authored winch position for "
                                    + mount.name
                                    + "; "
                                    + PlacementContext(origin)
                                    + $", candidates={positions.Length}, nativeBlocked={rejections.Native}, reservationBlocked={rejections.Reserved}."
                            );
                        warned = true;
                        retryAfter = Time.unscaledTime + 1f;
                        return;
                    }
                }
                Position(manager.boat.transform.TransformPoint(reservation.Position));
                // Native binding reparents the rope controller beside the winch.
                // It follows this non-rotating mounting frame, not the spinning wheel.
                if (Winch.rope.transform.parent != mount)
                    Winch.AttachToController(Winch.rope);
                mount.gameObject.SetActive(true);
                Winch.ShowWinch(true);
                // Report initial success and recovery only, after binding and showing
                // the control. Routine refreshes and rebindings stay silent.
                if (!hasLoggedPlacement || placementFailed)
                {
                    Plugin.Log.LogInfo(
                        (
                            placementFailed
                                ? "Winch successfully placed after retry for "
                                : "Placed authored winch for "
                        )
                            + mount.name
                            + "; "
                            + PlacementContext(origin)
                            + $", position={reservation.Position.ToString("F4")}, slot={reservation.Slot}."
                    );
                    hasLoggedPlacement = true;
                    placementFailed = false;
                }
            }

            private string PlacementContext(Vector3 origin)
            {
                var mast = Owner.GetComponentInParent<Mast>();
                int donor = definition.SourceMast >= 0 ? definition.SourceMast : definition.Mast;
                var pin =
                    pinTargets != null && reservation != null ? pinTargets[reservation.Slot] : null;
                return $"boat={manager.boat.name}#{manager.boat.GetInstanceID()}, owner={Owner.name}#{Owner.GetInstanceID()}, stay/mast={(mast ? mast.orderIndex : -1)}, requestedMast={definition.Mast}, donorMast={donor}, role={definition.Role}, source={Source.name}#{Source.GetInstanceID()}, origin={origin.ToString("F4")}, radius={radius:F4}"
                    + (pin ? $", pin={pin.name}#{pin.GetInstanceID()}" : "");
            }

            private WinchPlacement[] PinPlacements()
            {
                if (pinTargets.Any(pin => !pin))
                    return Array.Empty<WinchPlacement>();
                var inverseBoat = Quaternion.Inverse(manager.boat.transform.rotation);
                var inverseSource = Quaternion.Inverse(
                    inverseBoat * sourceFrame.rotation * frameRotation
                );
                var placements = new WinchPlacement[pinTargets.Length];
                for (int i = 0; i < pinTargets.Length; i++)
                {
                    var pin = pinTargets[i].transform;
                    placements[i] = new WinchPlacement(
                        manager.boat.transform.InverseTransformPoint(pin.position),
                        inverseBoat * pin.rotation * inverseSource
                    );
                }
                return placements;
            }

            private void Position(Vector3 position)
            {
                var boatRotation = manager.boat.transform.rotation;
                mount.SetPositionAndRotation(
                    position,
                    boatRotation
                        * placementRotation
                        * Quaternion.Inverse(boatRotation)
                        * sourceFrame.rotation
                        * frameRotation
                );
                // Keep the wheel's local rotation untouched: native input measures it.
                var scale = mount.lossyScale;
                var sourceScale = Source.transform.lossyScale;
                Winch.transform.localScale = new Vector3(
                    sourceScale.x / scale.x,
                    sourceScale.y / scale.y,
                    sourceScale.z / scale.z
                );
            }

            internal void Suspend()
            {
                manager.reservations.Release(this);
                reservation = null;
                // Hiding a fitting must not disable the sail-owned controller.
                if (Winch && Winch.rope && Winch.rope.transform.parent == mount && manager.boat)
                    Winch.rope.transform.SetParent(manager.boat.transform, true);
                if (mount)
                    mount.gameObject.SetActive(false);
            }

            public void Dispose()
            {
                if (disposed)
                    return;
                disposed = true;
                Suspend();
                manager.controls.Remove(this);
                if (Winch)
                {
                    // Native controllers belong to the sail and can be rebound to a
                    // replacement donor. Do not destroy them with this mounting frame.
                    if (Winch.rope && Winch.rope.transform.parent == mount && manager.boat)
                        Winch.rope.transform.SetParent(manager.boat.transform, true);
                    Winch.rope = null;
                    // A grabbed TouchRotateHandle temporarily unparents itself.
                    if (Winch.rotHandle)
                        Object.Destroy(Winch.rotHandle.gameObject);
                }
                if (mount)
                    Object.Destroy(mount.gameObject);
            }
        }
    }
}
