using MoreSailwindSails.Controls.Discovery;
using MoreSailwindSails.Controls.Runtime;
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
            Owned = control.GetComponent<OwnedWinchMarker>();
            Shapes = shapes;
        }

        internal WinchMountStatus Status(NativeWinchInventory inventory) =>
            !Control
                ? WinchMountStatus.Hidden
                : WinchMountOverlayGeometry.Classify(
                    owned: Owned,
                    visible: Control.gameObject.activeInHierarchy && Renderer && Renderer.enabled,
                    occupied: NativeWinchInventory.Occupied(c: Control),
                    mounted: inventory.Mounted(c: Control)
                );
    }
}
