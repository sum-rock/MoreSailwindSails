using System;
using MoreSailwindSails.Visuals;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Samples native paid-out controls and authored fitting state once per rig visual update.
    internal sealed class SpritsailVisualTriggers
    {
        private readonly SailVisualRevision revision = new SailVisualRevision();
        private int tack;

        internal long Update(
            Sail sail,
            Transform boat,
            CapsuleCollider mast,
            Vector3[] corners,
            bool active
        )
        {
            var connections = sail.GetComponent<SailConnections>();
            var left = connections.angleControllerLeft;
            var mid = connections.angleControllerMid;
            var right = connections.angleControllerRight;
            if (boat)
            {
                var aft = boat.InverseTransformDirection(direction: sail.cloth.transform.forward);
                float angle = (float)(Math.Atan2(aft.x, Math.Abs(aft.z)) * 180 / Math.PI);
                tack = SailVisualRevision.TackSide(angle: angle, previous: tack);
            }
            // Local scale products intentionally exclude rotation-dependent lossyScale.
            var scale = Vector3.one;
            for (
                var node = sail.cloth.transform;
                node && node != sail.transform.parent;
                node = node.parent
            )
                scale = Vector3.Scale(a: scale, b: node.localScale);
            var mastScale = Vector3.one;
            for (var node = mast ? mast.transform : null; node; node = node.parent)
                mastScale = Vector3.Scale(a: mastScale, b: node.localScale);
            var filter = mast ? mast.GetComponent<MeshFilter>() : null;
            long value = revision.Update(
                state: new SailVisualState(
                    sheets: new Vector3(
                        left ? left.currentLength : 0,
                        mid ? mid.currentLength : 0,
                        right ? right.currentLength : 0
                    ),
                    unroll: sail.currentUnroll,
                    tack: tack,
                    scale: scale,
                    mastScale: mastScale,
                    height: sail.GetCurrentInstallHeight(),
                    mast: mast,
                    mesh: filter ? filter.sharedMesh : null,
                    left: left,
                    mid: mid,
                    right: right,
                    active: active
                ),
                shape: corners
            );
            SpritsailMountProfile.Revision(reasons: revision.Reasons);
            return value;
        }

        internal void Reset() => revision.Reset();
    }
}
