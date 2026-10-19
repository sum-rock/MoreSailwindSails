// Owns one mount's paired sheet and halyard placement, retention, retry and release.
using System;
using System.Linq;
using MoreSailwindSails.Controls.Placement;
using UnityEngine;

namespace MoreSailwindSails.Controls.Runtime
{
    internal sealed class WinchGroup
    {
        private readonly WinchControls manager;
        private readonly BoatRefs boat;
        private readonly WinchReservations reservations;
        private readonly SheetingWinchPlacementResolver sheets;
        private readonly HalyardWinchPlacementResolver halyards;
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
        private readonly WinchPlacementState sheetState = new WinchPlacementState();
        private readonly WinchPlacementState halyardState = new WinchPlacementState();

        internal WinchGroup(
            WinchControls manager,
            BoatRefs boat,
            WinchReservations reservations,
            SheetingWinchPlacementResolver sheets,
            HalyardWinchPlacementResolver halyards,
            GameObject owner
        )
        {
            this.manager = manager;
            this.boat = boat;
            this.reservations = reservations;
            this.sheets = sheets;
            this.halyards = halyards;
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
                manager.Remove(group: this);
        }

        private void Release(
            WinchPlacementState state,
            OwnedWinch[] fittings,
            bool preserveControllers = true
        )
        {
            if (state.Released)
                return;
            state.Released = true;
            reservations.Release(owner: state);
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

        private void Refresh(WinchPlacementState state, bool pair, OwnedWinch[] fittings)
        {
            if (state.Current == null && Time.unscaledTime < state.RetryAfter)
                return;
            try
            {
                if (
                    WinchPlacementPolicy.TryRetain(
                        ledger: reservations,
                        owner: state,
                        current: state.Current,
                        valid: pair
                            ? sheets.ValidateCurrent(
                                forward: ForwardBase,
                                requested: HalyardMast,
                                current: state.Current
                            )
                            : halyards.ValidateCurrent(
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
                    ? sheets.Resolve(
                        owner: state,
                        forward: ForwardBase,
                        requested: HalyardMast,
                        current: state.Current
                    )
                    : halyards.Resolve(
                        owner: state,
                        forward: ForwardBase,
                        requested: HalyardMast,
                        current: state.Current
                    );
                var selected = result.Candidate as WinchPlacementCandidate;
                if (selected == null)
                {
                    Release(state: state, fittings: fittings);
                    state.Failed = true;
                    if (!state.Warned)
                        Plugin.Log.LogWarning(
                            $"No free native winch {(pair ? "pair" : "seat")}; {Context(pair: pair)}, nativeUnavailable={result.NativeUnavailable}, reserved={result.Reserved}, missingSupports={result.MissingSupports}, fallbackBlocked={result.FallbackBlocked}."
                        );
                    state.Warned = true;
                    state.RetryAfter = Time.unscaledTime + 1f;
                    return;
                }
                bool changed =
                    state.Current != null && !selected.SamePlacement(other: state.Current);
                // Prepare both replacements before retiring either current clone.
                var replacements = new WinchClone[fittings.Length];
                try
                {
                    for (int i = 0; i < fittings.Length; i++)
                        if (fittings[i].Source != selected.Templates[i])
                            replacements[i] = fittings[i].Prepare(source: selected.Templates[i]);
                    for (int i = 0; i < fittings.Length; i++)
                        if (replacements[i] != null)
                        {
                            var replacement = replacements[i];
                            replacements[i] = null;
                            fittings[i].Adopt(replacement: replacement);
                        }
                }
                finally
                {
                    foreach (var replacement in replacements)
                        replacement?.Destroy();
                }
                for (int i = 0; i < fittings.Length; i++)
                    fittings[i]
                        .Position(seat: selected.Seats[i], templateScale: selected.TemplateScale);
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
                        $"{action}; {Context(pair: pair)}, {selected.Context}, pair/seat={selected.Id}, origin={(selected.Fallback ? "fallback" : "native")}; {poses}."
                    );
                    state.Logged = true;
                }
                state.Failed = false;
                state.Warned = false;
                state.Current = selected;
            }
            catch (Exception exception)
            {
                Release(state: state, fittings: fittings);
                if (!state.Warned)
                    Plugin.Log.LogWarning(
                        $"Winch binding/placement failed; {Context(pair: pair)}: {exception}"
                    );
                state.Warned = state.Failed = true;
                state.RetryAfter = Time.unscaledTime + 1f;
            }
        }

        private string Context(bool pair) =>
            $"boat={boat.name}#{boat.GetInstanceID()}, owner={Owner.name}#{Owner.GetInstanceID()}, role={(pair ? "sheets" : "halyard")}, forward={(Forward ? Forward.orderIndex : -1)}, requestedMast={(HalyardMast ? HalyardMast.orderIndex : -1)}";
    }
}
