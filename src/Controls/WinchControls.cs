// Coordinates mount-owned controls and transient placement claims for one boat.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls.Discovery;
using MoreSailwindSails.Controls.Placement;
using MoreSailwindSails.Controls.Runtime;
using UnityEngine;

namespace MoreSailwindSails.Controls
{
    // Owns boat-local winch allocation, live replacement and final control cleanup.
    internal sealed class WinchControls : MonoBehaviour
    {
        private readonly WinchReservations reservations = new WinchReservations();
        private readonly List<OwnedWinch> controls = new List<OwnedWinch>();
        private readonly List<WinchGroup> groups = new List<WinchGroup>();
        private readonly List<OwnedWinch> controlSnapshot = new List<OwnedWinch>();
        private readonly List<WinchGroup> groupSnapshot = new List<WinchGroup>();
        private BoatRefs boat;
        private NativeWinchInventory native;
        private SheetingWinchPlacementResolver sheets;
        private HalyardWinchPlacementResolver halyards;

        private static WinchControls For(BoatRefs boat)
        {
            var manager =
                boat.GetComponent<WinchControls>() ?? boat.gameObject.AddComponent<WinchControls>();
            if (manager.native == null)
            {
                manager.boat = boat;
                manager.native = new NativeWinchInventory(
                    boat: boat,
                    owned: c => c.GetComponent<OwnedWinchMarker>()
                );
                manager.sheets = new SheetingWinchPlacementResolver(
                    native: manager.native,
                    ledger: manager.reservations
                );
                manager.halyards = new HalyardWinchPlacementResolver(
                    native: manager.native,
                    ledger: manager.reservations
                );
            }
            return manager;
        }

        private WinchGroup Group(GameObject owner)
        {
            var group = groups.FirstOrDefault(g => g.Owner == owner);
            if (group == null)
                groups.Add(
                    group = new WinchGroup(
                        manager: this,
                        boat: boat,
                        reservations: reservations,
                        sheets: sheets,
                        halyards: halyards,
                        owner: owner
                    )
                );
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
                || group.NativeMount != nativeMount
            )
            {
                group.Suspend();
                group.ResetSheetSpan();
            }
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
            var manager = For(boat: boat);
            var group = manager.Group(owner: owner);
            manager.native.Prepare();
            var source = NativeWinchInventory
                .Sources(mast: donorMast, role: role)
                ?.FirstOrDefault(manager.native.TemplateUsable);
            if (!source)
                throw new InvalidOperationException(
                    $"Winch bootstrap template unavailable: {label}."
                );
            var control = new OwnedWinch(
                manager: manager,
                boat: boat,
                group: group,
                parent: parent,
                source: source,
                role: role,
                label: label
            );
            manager.controls.Add(control);
            control.Group.Set(control: control);
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
            var manager = boat ? boat.GetComponent<WinchControls>() : null;
            if (!manager)
                return;
            manager.native?.Invalidate();
            foreach (var control in manager.controls)
                if (control.Group.NativeMount == component)
                    control.NativeBindingComplete();
        }

        internal void Remove(OwnedWinch control) => controls.Remove(control);

        internal void Remove(WinchGroup group) => groups.Remove(group);

        private void OnDestroy()
        {
            foreach (var control in controls.ToArray())
                control.Dispose();
        }
    }
}
