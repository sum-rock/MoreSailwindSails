using MoreSailwindSails.Controls;
using UnityEngine;

namespace MoreSailwindSails.Utils
{
    // Caches a discovered control's drawing sources without changing its native state.
    internal sealed class WinchMountTrace
    {
        internal readonly GPButtonRopeWinch Control;
        internal readonly Renderer Renderer;
        internal readonly bool Owned;
        internal readonly WinchMountShape[] Shapes;

        internal WinchMountTrace(GPButtonRopeWinch control, WinchMountShape[] shapes)
        {
            Control = control;
            Renderer = control.GetComponent<Renderer>();
            Owned = control.GetComponent<FishermanOwnedWinchMarker>();
            Shapes = shapes;
        }

        internal WinchMountStatus Status(NativeWinchSeats inventory) =>
            !Control
                ? WinchMountStatus.Hidden
                : WinchMountOverlayGeometry.Classify(
                    owned: Owned,
                    visible: Control.gameObject.activeInHierarchy && Renderer && Renderer.enabled,
                    occupied: NativeWinchSeats.Occupied(c: Control),
                    mounted: inventory.Mounted(c: Control)
                );
    }
}
