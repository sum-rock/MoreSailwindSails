// Owns controls and transient placement claims for registered Fisherman stay mounts.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls.Configuration;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Controls
{
    // Owns boat-local winch allocation, live replacement and final control cleanup.
    internal sealed class FishermanWinchControls : MonoBehaviour
    {
        private readonly WinchReservations reservations = new WinchReservations();
        private readonly List<OwnedWinch> controls = new List<OwnedWinch>();
        private readonly List<ControlGroup> groups = new List<ControlGroup>();
        private readonly List<OwnedWinch> controlSnapshot = new List<OwnedWinch>();
        private readonly List<ControlGroup> groupSnapshot = new List<ControlGroup>();
        private BoatRefs boat;
        private NativeWinchSeats native;
        private SheetingWinchPlacementResolver sheets;
        private HalyardWinchPlacementResolver halyards;

        private static FishermanWinchControls For(BoatRefs boat)
        {
            var manager =
                boat.GetComponent<FishermanWinchControls>()
                ?? boat.gameObject.AddComponent<FishermanWinchControls>();
            if (manager.native == null)
            {
                manager.boat = boat;
                manager.native = new NativeWinchSeats(
                    boat,
                    c => c.GetComponent<FishermanOwnedWinchMarker>()
                );
                manager.sheets = new SheetingWinchPlacementResolver(
                    manager.native,
                    manager.reservations
                );
                manager.halyards = new HalyardWinchPlacementResolver(
                    manager.native,
                    manager.reservations
                );
            }
            return manager;
        }

        private ControlGroup Group(GameObject owner)
        {
            var group = groups.FirstOrDefault(g => g.Owner == owner);
            if (group == null)
                groups.Add(group = new ControlGroup(this, owner));
            return group;
        }

        internal static void Configure(
            BoatRefs boat,
            GameObject owner,
            Mast forward,
            Mast halyard,
            Mast nativeMount = null
        )
        {
            var manager = For(boat: boat);
            manager.native.Prepare();
            halyard = manager.native.PhysicalMast(section: halyard);
            var forwardBase = manager.native.PhysicalMast(section: forward);
            var group = manager.Group(owner: owner);
            if (
                group.Forward != forward
                || group.ForwardBase != forwardBase
                || group.HalyardMast != halyard
            )
                group.Suspend();
            group.Forward = forward;
            group.ForwardBase = forwardBase;
            group.HalyardMast = halyard;
            group.NativeMount = nativeMount;
        }

        internal static OwnedWinch Create(
            BoatRefs boat,
            GameObject owner,
            Transform parent,
            Mast donorMast,
            WinchRole role,
            string label
        )
        {
            var manager = For(boat);
            var group = manager.Group(owner);
            manager.native.Prepare();
            var source = NativeWinchSeats
                .Sources(mast: donorMast, role: role)
                ?.FirstOrDefault(manager.native.TemplateUsable);
            if (!source)
                throw new InvalidOperationException(
                    $"Winch bootstrap template unavailable: {label}."
                );
            var control = new OwnedWinch(manager, group, parent, source, role, label);
            manager.controls.Add(control);
            control.Group.Set(control);
            return control;
        }

        // Copy before callbacks can mutate the source; retain capacity between frames.
        private static List<T> Snapshot<T>(List<T> source, List<T> buffer)
        {
            buffer.Clear();
            buffer.AddRange(collection: source);
            return buffer;
        }

        private void LateUpdate()
        {
            if (!boat || controls.Count == 0)
                return;
            try
            {
                foreach (var control in Snapshot(source: controls, buffer: controlSnapshot))
                    if (!control.Owner)
                        control.Dispose();
                    else if (!control.Winch)
                        control.Retire();
                if (controls.Count == 0)
                    return;
                native.Prepare();
                // Release inactive owners before anyone competes for their seats.
                foreach (var group in Snapshot(source: groups, buffer: groupSnapshot))
                    group.ReleaseUnused();
                foreach (var group in Snapshot(source: groups, buffer: groupSnapshot))
                    group.Refresh();
            }
            finally
            {
                controlSnapshot.Clear();
                groupSnapshot.Clear();
            }
        }

        internal static void InvalidateInventory(Component component)
        {
            var boat = component ? component.GetComponentInParent<BoatRefs>() : null;
            var manager = boat ? boat.GetComponent<FishermanWinchControls>() : null;
            if (!manager)
                return;
            manager.native?.Invalidate();
            foreach (var control in manager.controls)
                if (control.Group.NativeMount == component)
                    control.NativeBindingComplete();
        }

        private void OnDestroy()
        {
            foreach (var control in controls.ToArray())
                control.Dispose();
        }

        internal sealed class ControlGroup
        {
            private readonly FishermanWinchControls manager;
            internal readonly GameObject Owner;
            internal Mast Forward,
                ForwardBase,
                HalyardMast,
                NativeMount;
            private readonly OwnedWinch[] sheetFittings = new OwnedWinch[2];
            private readonly OwnedWinch[] halyardFittings = new OwnedWinch[1];
            private OwnedWinch port
            {
                get => sheetFittings[0];
                set => sheetFittings[0] = value;
            }
            private OwnedWinch starboard
            {
                get => sheetFittings[1];
                set => sheetFittings[1] = value;
            }
            private OwnedWinch reef
            {
                get => halyardFittings[0];
                set => halyardFittings[0] = value;
            }
            private readonly PlacementState sheetState = new PlacementState();
            private readonly PlacementState halyardState = new PlacementState();

            internal ControlGroup(FishermanWinchControls manager, GameObject owner)
            {
                this.manager = manager;
                Owner = owner;
            }

            internal void Set(OwnedWinch control)
            {
                if (control.Role == WinchRole.Reef)
                    reef = control;
                else if (control.Role == WinchRole.Left)
                    port = control;
                else if (control.Role == WinchRole.Right)
                    starboard = control;
            }

            private bool Active => Owner && Owner.activeInHierarchy;
            private bool SheetWanted =>
                Active && port != null && starboard != null && port.Wanted && starboard.Wanted;
            private bool HalyardWanted => Active && reef != null && reef.Wanted;

            internal void ReleaseUnused()
            {
                if (!SheetWanted)
                    Release(state: sheetState, fittings: sheetFittings);
                if (!HalyardWanted)
                    Release(state: halyardState, fittings: halyardFittings);
            }

            internal void Suspend(bool preserveControllers = true)
            {
                Release(
                    state: sheetState,
                    fittings: sheetFittings,
                    preserveControllers: preserveControllers
                );
                Release(
                    state: halyardState,
                    fittings: halyardFittings,
                    preserveControllers: preserveControllers
                );
            }

            internal void Dirty()
            {
                sheetState.RetryAfter = halyardState.RetryAfter = 0f;
            }

            internal void Remove(OwnedWinch control)
            {
                // Destruction can start at any owner; never rescue sibling ropes here.
                Suspend(preserveControllers: false);
                if (port == control)
                    port = null;
                if (starboard == control)
                    starboard = null;
                if (reef == control)
                    reef = null;
                if (port == null && starboard == null && reef == null)
                    manager.groups.Remove(this);
            }

            private void Release(
                PlacementState state,
                OwnedWinch[] fittings,
                bool preserveControllers = true
            )
            {
                if (state.Released)
                    return;
                state.Released = true;
                manager.reservations.Release(state);
                state.Current = null;
                state.RetryAfter = 0f;
                foreach (var fitting in fittings)
                    fitting?.Suspend(preserveControllers: preserveControllers);
            }

            internal void Refresh()
            {
                if (SheetWanted)
                    Refresh(state: sheetState, pair: true, fittings: sheetFittings);
                if (HalyardWanted)
                    Refresh(state: halyardState, pair: false, fittings: halyardFittings);
            }

            private void Refresh(PlacementState state, bool pair, OwnedWinch[] fittings)
            {
                if (state.Current == null && Time.unscaledTime < state.RetryAfter)
                    return;
                try
                {
                    if (
                        WinchPlacementPolicy.TryRetain(
                            ledger: manager.reservations,
                            owner: state,
                            current: state.Current,
                            valid: pair
                                ? manager.sheets.ValidateCurrent(
                                    forward: ForwardBase,
                                    requested: HalyardMast,
                                    current: state.Current
                                )
                                : manager.halyards.ValidateCurrent(
                                    forward: ForwardBase,
                                    requested: HalyardMast,
                                    current: state.Current
                                )
                        )
                    )
                    {
                        // Boat/support motion still updates the mounting frame, without
                        // enumerating seats, replacing clones or touching the ledger.
                        for (int i = 0; i < fittings.Length; i++)
                        {
                            fittings[i]
                                .Position(
                                    seat: state.Current.Seats[i],
                                    templateScale: state.Current.TemplateScale
                                );
                            fittings[i].Show();
                        }
                        return;
                    }
                    state.Released = false;
                    var result = pair
                        ? manager.sheets.Resolve(
                            owner: state,
                            forward: ForwardBase,
                            requested: HalyardMast,
                            current: state.Current
                        )
                        : manager.halyards.Resolve(
                            owner: state,
                            forward: ForwardBase,
                            requested: HalyardMast,
                            current: state.Current
                        );
                    var selected = result.Candidate as NativeWinchCandidate;
                    if (selected == null)
                    {
                        Release(state, fittings);
                        state.Failed = true;
                        if (!state.Warned)
                            Plugin.Log.LogWarning(
                                $"No free native winch {(pair ? "pair" : "seat")}; {Context(pair)}, nativeUnavailable={result.NativeUnavailable}, reserved={result.Reserved}, missingSupports={result.MissingSupports}, fallbackBlocked={result.FallbackBlocked}."
                            );
                        state.Warned = true;
                        state.RetryAfter = Time.unscaledTime + 1f;
                        return;
                    }
                    bool changed = state.Current != null && !selected.SamePlacement(state.Current);
                    // Prepare both replacements before retiring either current clone.
                    var replacements = new Clone[fittings.Length];
                    try
                    {
                        for (int i = 0; i < fittings.Length; i++)
                            if (fittings[i].Source != selected.Templates[i])
                                replacements[i] = fittings[i].Prepare(selected.Templates[i]);
                        for (int i = 0; i < fittings.Length; i++)
                            if (replacements[i] != null)
                            {
                                var replacement = replacements[i];
                                replacements[i] = null;
                                fittings[i].Adopt(replacement);
                            }
                    }
                    finally
                    {
                        foreach (var replacement in replacements)
                            replacement?.Destroy();
                    }
                    for (int i = 0; i < fittings.Length; i++)
                        fittings[i]
                            .Position(
                                seat: selected.Seats[i],
                                templateScale: selected.TemplateScale
                            );
                    foreach (var fitting in fittings)
                        fitting.Show();
                    if (!state.Logged || state.Failed || changed)
                    {
                        string action =
                            state.Failed ? "Winch successfully placed after retry"
                            : changed ? "Winch reassigned after native/support change"
                            : "Placed native winch";
                        string poses = string.Join(
                            "; ",
                            selected.Seats.Select(
                                (s, i) =>
                                    $"side={i}, position={s.Position.ToString("F4")}, rotation={s.Rotation.ToString("F4")}, template={selected.Templates[i].name}#{selected.Templates[i].GetInstanceID()}"
                            )
                        );
                        Plugin.Log.LogInfo(
                            $"{action}; {Context(pair)}, {selected.Context}, pair/seat={selected.Id}, origin={(selected.Fallback ? "fallback" : "native")}; {poses}."
                        );
                        state.Logged = true;
                    }
                    state.Failed = false;
                    state.Warned = false;
                    state.Current = selected;
                }
                catch (Exception exception)
                {
                    Release(state, fittings);
                    if (!state.Warned)
                        Plugin.Log.LogWarning(
                            $"Winch binding/placement failed; {Context(pair)}: {exception}"
                        );
                    state.Warned = state.Failed = true;
                    state.RetryAfter = Time.unscaledTime + 1f;
                }
            }

            private string Context(bool pair) =>
                $"boat={manager.boat.name}#{manager.boat.GetInstanceID()}, owner={Owner.name}#{Owner.GetInstanceID()}, role={(pair ? "sheets" : "halyard")}, forward={(Forward ? Forward.orderIndex : -1)}, requestedMast={(HalyardMast ? HalyardMast.orderIndex : -1)}";

            private sealed class PlacementState
            {
                internal NativeWinchCandidate Current;
                internal float RetryAfter;
                internal bool Released = true;
                internal bool Logged,
                    Warned,
                    Failed;
            }
        }

        internal sealed class Clone
        {
            internal GPButtonRopeWinch Source,
                Winch;
            internal Transform Mount;

            internal void Detach(BoatRefs boat)
            {
                if (Winch && Winch.rope && Winch.rope.transform.parent == Mount && boat)
                    Winch.rope.transform.SetParent(boat.transform, true);
            }

            internal void Destroy()
            {
                if (Winch)
                {
                    Winch.rope = null;
                    if (Winch.rotHandle)
                        Object.Destroy(Winch.rotHandle.gameObject);
                }
                if (Mount)
                {
                    Mount.gameObject.SetActive(false);
                    Object.Destroy(Mount.gameObject);
                }
            }
        }

        internal sealed class OwnedWinch : IDisposable
        {
            private readonly FishermanWinchControls manager;
            internal readonly ControlGroup Group;
            internal readonly WinchRole Role;
            private readonly Transform parent;
            private readonly string label;
            private Clone clone;
            private bool disposed;
            internal GameObject Owner => Group.Owner;
            internal GPButtonRopeWinch Source => clone.Source;
            internal GPButtonRopeWinch Winch => clone.Winch;
            internal bool Wanted =>
                !disposed && Owner && Owner.activeInHierarchy && Winch && Winch.rope;

            internal OwnedWinch(
                FishermanWinchControls manager,
                ControlGroup group,
                Transform parent,
                GPButtonRopeWinch source,
                WinchRole role,
                string label
            )
            {
                this.manager = manager;
                Group = group;
                Role = role;
                this.parent = parent;
                this.label = label;
                clone = Prepare(source);
            }

            internal Clone Prepare(GPButtonRopeWinch source)
            {
                var root = new GameObject(label + " mount");
                root.SetActive(false);
                root.transform.SetParent(parent, false);
                var prepared = new Clone { Source = source, Mount = root.transform };
                try
                {
                    var obj = Object.Instantiate(source.gameObject, prepared.Mount, false);
                    obj.name = label;
                    prepared.Winch = obj.GetComponent<GPButtonRopeWinch>();
                    prepared.Winch.rope = null;
                    obj.AddComponent<FishermanOwnedWinchMarker>();
                    FishermanWinchVisuals.ResetClonedOutline(winch: prepared.Winch);
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;
                    if (
                        prepared.Winch.rotHandle
                        && !prepared.Winch.rotHandle.transform.IsChildOf(obj.transform)
                    )
                    {
                        prepared.Winch.rotHandle = Object.Instantiate(
                            prepared.Winch.rotHandle,
                            obj.transform,
                            false
                        );
                        prepared.Winch.rotHandle.transform.localPosition = Vector3.zero;
                    }
                    if (prepared.Winch.rotHandle)
                        prepared.Winch.rotHandle.rotatable = obj.transform;
                    obj.SetActive(true);
                    return prepared;
                }
                catch
                {
                    // No controller has been attached to this uncommitted clone.
                    Object.Destroy(root);
                    throw;
                }
            }

            internal void Adopt(Clone replacement)
            {
                var controller = Winch ? Winch.rope : null;
                var previous = clone;
                previous.Detach(manager.boat);
                clone = replacement;
                // Preserve the binding even if AttachToController throws while updating visuals.
                Winch.rope = controller;
                if (Group.NativeMount)
                {
                    if (Role == WinchRole.Reef)
                        Group.NativeMount.reefWinch = new[] { Winch };
                    else if (Role == WinchRole.Left)
                        Group.NativeMount.leftAngleWinch = new[] { Winch };
                    else if (Role == WinchRole.Right)
                        Group.NativeMount.rightAngleWinch = new[] { Winch };
                }
                previous.Destroy();
                if (controller)
                    Bind(controller);
            }

            internal void Bind(RopeController controller)
            {
                try
                {
                    Winch.AttachToController(controller);
                    NativeBindingComplete();
                }
                catch
                {
                    Group.Suspend();
                    throw;
                }
            }

            internal void NativeBindingComplete()
            {
                // Native binding reparents ropes beneath the winch. Hidden placement
                // mounts must not deactivate controllers during exhaustion or support loss.
                if (clone.Mount && !clone.Mount.gameObject.activeInHierarchy)
                    clone.Detach(boat: manager.boat);
                Group.Dirty();
            }

            internal void Position(WinchSeat seat, Vector3? templateScale = null)
            {
                clone.Mount.SetPositionAndRotation(
                    manager.boat.transform.TransformPoint(seat.Position),
                    manager.boat.transform.rotation * seat.Rotation
                );
                var scale = clone.Mount.lossyScale;
                var sourceScale = templateScale ?? Source.transform.lossyScale;
                Winch.transform.localScale = new Vector3(
                    sourceScale.x / scale.x,
                    sourceScale.y / scale.y,
                    sourceScale.z / scale.z
                );
                // Input is wheel-local: move only its parent mounting frame.
                if (Winch.rope && Winch.rope.transform.parent != clone.Mount)
                    Winch.AttachToController(Winch.rope);
            }

            internal void Show()
            {
                if (!clone.Mount.gameObject.activeSelf)
                    clone.Mount.gameObject.SetActive(true);
                if (
                    !Winch.GetComponent<Renderer>().enabled
                    || !Winch.GetComponent<Collider>().enabled
                )
                    Winch.ShowWinch(true);
            }

            internal void Suspend(bool preserveControllers)
            {
                if (preserveControllers)
                    clone.Detach(boat: manager.boat);
                if (clone.Mount)
                    clone.Mount.gameObject.SetActive(false);
            }

            internal void Retire()
            {
                if (disposed)
                    return;
                Group.Suspend(preserveControllers: true);
                clone.Detach(boat: manager.boat);
                Dispose();
            }

            public void Dispose()
            {
                if (disposed)
                    return;
                disposed = true;
                Group.Remove(this);
                clone.Destroy();
                manager.controls.Remove(this);
            }
        }
    }
}
