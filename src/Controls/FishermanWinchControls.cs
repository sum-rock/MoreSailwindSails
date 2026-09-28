using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Sails;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Controls
{
    internal sealed class FishermanOwnedWinchMarker : MonoBehaviour { }

    internal sealed class FishermanWinchControls : MonoBehaviour
    {
        private readonly WinchReservations reservations = new WinchReservations();
        private readonly List<OwnedWinch> controls = new List<OwnedWinch>();
        private readonly List<ControlGroup> groups = new List<ControlGroup>();
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
            var group = For(boat).Group(owner);
            if (group.Forward != forward || group.HalyardMast != halyard)
                group.Suspend();
            group.Forward = forward;
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
            var source = Source(boat, donorMast, role);
            if (!source)
                throw new InvalidOperationException("No usable source winch: " + label);
            var control = new OwnedWinch(
                manager,
                manager.Group(owner),
                parent,
                source,
                donorMast,
                role,
                label
            );
            manager.controls.Add(control);
            control.Group.Set(control);
            return control;
        }

        // This supplies an initial hidden clone for native startup binding only.
        // The selected seat later supplies its own exact clone template.
        internal static GPButtonRopeWinch Source(BoatRefs boat, Mast mast, WinchRole role) =>
            NativeWinchSeats.Sources(mast, role)?.FirstOrDefault(NativeWinchSeats.Usable);

        internal static void Reconcile(
            ref OwnedWinch[] current,
            BoatRefs boat,
            GameObject owner,
            Transform parent,
            Mast forward,
            Mast[] donors,
            string[] labels
        )
        {
            Configure(boat, owner, forward, donors[0]);
            var roles = new[] { WinchRole.Reef, WinchRole.Left, WinchRole.Right };
            var next = new OwnedWinch[3];
            try
            {
                for (int i = 0; i < 3; i++)
                    next[i] =
                        current != null
                        && !current[i].IsDisposed
                        && current[i].Winch
                        && current[i].TemplateDonor == donors[i]
                            ? current[i]
                            : Create(boat, owner, parent, donors[i], roles[i], labels[i]);
            }
            catch
            {
                foreach (var control in next)
                    if (control != null && (current == null || !current.Contains(control)))
                        control.Dispose();
                if (current != null)
                    foreach (var control in current)
                        control.Group.Set(control);
                throw;
            }
            if (current != null)
                foreach (var control in current)
                    if (!next.Contains(control))
                        control.Dispose();
            current = next;
        }

        internal static void BindSheets(
            OwnedWinch port,
            RopeController portRope,
            OwnedWinch starboard,
            RopeController starboardRope
        )
        {
            // Both controllers are registered before the boat's late refresh can allocate.
            port.Bind(portRope);
            starboard.Bind(starboardRope);
        }

        private void LateUpdate()
        {
            if (!boat)
                return;
            native.Refresh();
            foreach (var control in controls.ToArray())
                if (!control.Owner || !control.Winch)
                    control.Dispose();
            // Release inactive owners before anyone competes for their seats.
            foreach (var group in groups.ToArray())
                group.ReleaseUnused();
            foreach (var group in groups.ToArray())
                group.Refresh();
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
                HalyardMast,
                NativeMount;
            private OwnedWinch port,
                starboard,
                reef;
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

            private bool Active =>
                Owner
                && Owner.activeInHierarchy
                && (
                    !NativeMount
                    || NativeMount.sails == null
                    || !NativeMount.sails.Any(s =>
                        s
                        && (
                            s.GetComponent<Sails.FishermansFlyingSail.FishermansFlyingSailRig>()
                            || s.GetComponent<Sails.FishermansStaysail.FishermansStaysailRig>()
                        )
                    )
                );
            private bool SheetWanted =>
                Active && port != null && starboard != null && port.Wanted && starboard.Wanted;
            private bool HalyardWanted => Active && reef != null && reef.Wanted;

            internal void ReleaseUnused()
            {
                if (!SheetWanted)
                    Release(sheetState, port, starboard);
                if (!HalyardWanted)
                    Release(halyardState, reef);
            }

            internal void Suspend()
            {
                Release(sheetState, port, starboard);
                Release(halyardState, reef);
            }

            internal void Dirty()
            {
                sheetState.RetryAfter = halyardState.RetryAfter = 0f;
            }

            internal void Remove(OwnedWinch control)
            {
                Suspend();
                if (port == control)
                    port = null;
                if (starboard == control)
                    starboard = null;
                if (reef == control)
                    reef = null;
                if (port == null && starboard == null && reef == null)
                    manager.groups.Remove(this);
            }

            private void Release(PlacementState state, params OwnedWinch[] fittings)
            {
                manager.reservations.Release(state);
                state.Current = null;
                state.RetryAfter = 0f;
                foreach (var fitting in fittings)
                    fitting?.Suspend();
            }

            internal void Refresh()
            {
                if (SheetWanted)
                    Refresh(sheetState, true, new[] { port, starboard });
                if (HalyardWanted)
                    Refresh(halyardState, false, new[] { reef });
            }

            private void Refresh(PlacementState state, bool pair, OwnedWinch[] fittings)
            {
                if (state.Current == null && Time.unscaledTime < state.RetryAfter)
                    return;
                try
                {
                    var result = pair
                        ? manager.sheets.Resolve(state, Owner, Forward, state.Current)
                        : manager.halyards.Resolve(state, HalyardMast, state.Current);
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
                            replacement?.Destroy(manager.boat);
                    }
                    for (int i = 0; i < fittings.Length; i++)
                        fittings[i].Position(selected.Seats[i]);
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

            internal void Destroy(BoatRefs boat)
            {
                Detach(boat);
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
            internal readonly Mast TemplateDonor;
            internal readonly WinchRole Role;
            private readonly Transform parent;
            private readonly string label;
            private Clone clone;
            private bool disposed;
            internal GameObject Owner => Group.Owner;
            internal GPButtonRopeWinch Source => clone.Source;
            internal GPButtonRopeWinch Winch => clone.Winch;
            internal bool IsDisposed => disposed;
            internal bool Wanted =>
                !disposed && Owner && Owner.activeInHierarchy && Winch && Winch.rope;

            internal OwnedWinch(
                FishermanWinchControls manager,
                ControlGroup group,
                Transform parent,
                GPButtonRopeWinch source,
                Mast donor,
                WinchRole role,
                string label
            )
            {
                this.manager = manager;
                Group = group;
                TemplateDonor = donor;
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
                    FishermanWinchVisuals.ResetClonedOutline(prepared.Winch);
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
                previous.Destroy(manager.boat);
                if (controller)
                    Bind(controller);
            }

            internal void Bind(RopeController controller)
            {
                try
                {
                    Winch.AttachToController(controller);
                    if (!clone.Mount.gameObject.activeInHierarchy)
                        clone.Detach(manager.boat);
                    Group.Dirty();
                }
                catch
                {
                    Group.Suspend();
                    throw;
                }
            }

            internal void Position(WinchSeat seat)
            {
                clone.Mount.SetPositionAndRotation(
                    manager.boat.transform.TransformPoint(seat.Position),
                    manager.boat.transform.rotation * seat.Rotation
                );
                var scale = clone.Mount.lossyScale;
                var sourceScale = Source.transform.lossyScale;
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
                clone.Mount.gameObject.SetActive(true);
                Winch.ShowWinch(true);
            }

            internal void Suspend()
            {
                clone.Detach(manager.boat);
                if (clone.Mount)
                    clone.Mount.gameObject.SetActive(false);
            }

            public void Dispose()
            {
                if (disposed)
                    return;
                disposed = true;
                Group.Remove(this);
                clone.Destroy(manager.boat);
                manager.controls.Remove(this);
            }
        }
    }
}
