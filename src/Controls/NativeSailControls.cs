// Preserves native sail connections while handing managed sails to shared winch placement.
using MoreSailwindSails.Controls.Discovery;
using UnityEngine;

namespace MoreSailwindSails.Controls
{
    internal static class NativeSailControls
    {
        internal static bool Managed(Component sail) =>
            sail && sail.GetComponent<INativeSailControlRig>() != null;

        internal static void BindReef(Mast mast, int index, SailConnections connections)
        {
            if (
                !Managed(sail: connections)
                || Available(
                    controls: mast.reefWinch,
                    index: index,
                    controller: connections.reefController
                )
            )
                mast.reefWinch[index].AttachToController(connections.reefController);
        }

        internal static void BindSheets(Mast mast, int index, SailConnections connections)
        {
            if (
                Managed(sail: connections)
                && (
                    !Available(
                        controls: mast.leftAngleWinch,
                        index: index,
                        controller: connections.angleControllerLeft
                    )
                    || !Available(
                        controls: mast.rightAngleWinch,
                        index: index,
                        controller: connections.angleControllerRight
                    )
                    || mast.leftAngleWinch[index] == mast.rightAngleWinch[index]
                )
            )
                return;
            mast.leftAngleWinch[index].AttachToController(connections.angleControllerLeft);
            mast.rightAngleWinch[index].AttachToController(connections.angleControllerRight);
        }

        private static bool Available(
            GPButtonRopeWinch[] controls,
            int index,
            RopeController controller
        ) =>
            controls != null
            && index >= 0
            && index < controls.Length
            && controller
            && NativeWinchInventory.Usable(c: controls[index])
            && (!controls[index].rope || controls[index].rope == controller);

        internal static void ReleaseNativeBindings(Mast mast, SailConnections connections)
        {
            var boat = mast.GetComponentInParent<BoatRefs>();
            ReleaseNative(controls: mast.leftAngleWinch, connections: connections, boat: boat);
            ReleaseNative(controls: mast.rightAngleWinch, connections: connections, boat: boat);
            ReleaseNative(controls: mast.reefWinch, connections: connections, boat: boat);
        }

        private static void ReleaseNative(
            GPButtonRopeWinch[] controls,
            SailConnections connections,
            BoatRefs boat
        )
        {
            if (controls == null || !connections)
                return;
            foreach (var control in controls)
                if (
                    control
                    && control.rope
                    && (
                        control.rope == connections.angleControllerLeft
                        || control.rope == connections.angleControllerRight
                        || control.rope == connections.reefController
                    )
                )
                {
                    // Missing placements must not leave controllers beneath inactive rigging.
                    if (boat)
                        control.rope.transform.SetParent(
                            parent: boat.transform,
                            worldPositionStays: true
                        );
                    control.rope = null;
                    control.ShowWinch(false);
                }
        }
    }

    // Present on the prefab rig before native binding, including lazily created Flying rigging.
    internal interface INativeSailControlRig
    {
        void AttachControls();
    }
}
