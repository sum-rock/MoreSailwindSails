using System;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Temporarily hides invalid controller references from native binding, retaining their objects and saved input.
    internal sealed class SpritsailControlBindingState
    {
        internal readonly SpritsailNativeBinding Binding;
        private readonly SailConnections connections;
        private readonly RopeController reef,
            mid,
            left,
            right;
        private readonly Transform middle,
            lower,
            upper;
        private bool suppressed;

        internal SpritsailControlBindingState(Sail sail)
        {
            Binding = SpritsailNativeBinding.For(sail: sail);
            connections = sail.GetComponent<SailConnections>();
            reef = connections.reefController;
            mid = connections.angleControllerMid;
            left = connections.angleControllerLeft;
            right = connections.angleControllerRight;
            middle = connections.midRopeAttachment;
            lower = connections.mastReefAttachment;
            upper = connections.mastReefAttExtension;
        }

        internal void Suppress()
        {
            suppressed = true;
            connections.reefController = null;
            connections.angleControllerMid = null;
            connections.angleControllerLeft = null;
            connections.angleControllerRight = null;
            connections.midRopeAttachment = null;
            connections.mastReefAttachment = null;
            connections.mastReefAttExtension = null;
        }

        internal void Restore(bool failed)
        {
            if (!connections)
                return;
            if (suppressed)
            {
                connections.reefController = reef;
                connections.angleControllerMid = mid;
                connections.angleControllerLeft = left;
                connections.angleControllerRight = right;
                connections.midRopeAttachment = middle;
                connections.mastReefAttachment = lower;
                connections.mastReefAttExtension = upper;
            }
            // Preserve the existing family envelope after native attachment refreshes,
            // including saved limits that predate the current travel range.
            var checker = connections.colChecker;
            if (checker && !failed)
            {
                checker.colAngleMin = SpritsailTravel.Clamp(angle: checker.colAngleMin);
                checker.colAngleMax = SpritsailTravel.Clamp(angle: checker.colAngleMax);
                connections.sail.minAngle = checker.colAngleMin;
                connections.sail.maxAngle = checker.colAngleMax;
            }
            foreach (var control in new[] { reef, mid, left, right })
                if (control)
                {
                    if (failed || Binding.Error != null)
                        foreach (
                            var winch in new[]
                            {
                                Binding.Mast.reefWinch,
                                Binding.Mast.midAngleWinch,
                                Binding.Mast.leftAngleWinch,
                                Binding.Mast.rightAngleWinch,
                            }.SelectMany(selector: items =>
                                items ?? Array.Empty<GPButtonRopeWinch>()
                            )
                        )
                            if (winch && winch.rope == control)
                            {
                                winch.rope = null;
                                winch.ShowWinch(false);
                            }
                    control.gameObject.SetActive(value: !failed && Binding.Error == null);
                }
        }
    }
}
