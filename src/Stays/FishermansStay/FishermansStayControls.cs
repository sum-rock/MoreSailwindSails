// Adapts a dedicated stay mount and its fitted sail to the shared control allocator.
using System;
using MoreSailwindSails.Controls;
using MoreSailwindSails.Sails.FishermansStaysail;
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay
{
    internal sealed class FishermansStayControls : ISailControlRig
    {
        private readonly FishermansStay stay;
        private readonly BoatRefs boat;
        private GameObject currentSail;
        private SailConnections connections;
        private FishermansStaysailRig rig;
        private readonly Mast[] supports = new Mast[2];

        internal FishermansStayControls(FishermansStay stay, BoatRefs boat)
        {
            this.stay = stay;
            this.boat = boat;
        }

        public bool TryGetControlRequest(out SailControlRequest request)
        {
            request = default;
            var mount = stay.Mount;
            if (!mount)
                return false;
            GameObject fitted = null;
            foreach (var item in mount.sails)
                if (item && item.transform.parent == mount.transform)
                {
                    fitted = item;
                    break;
                }
            if (fitted != currentSail)
            {
                currentSail = fitted;
                connections = fitted ? fitted.GetComponent<SailConnections>() : null;
                rig = fitted ? fitted.GetComponent<FishermansStaysailRig>() : null;
            }
            var references = stay.References;
            if (supports[0] != references.Fore || supports[1] != references.Aft)
            {
                if (rig)
                    rig.InvalidateSheetSpan();
                supports[0] = references.Fore;
                supports[1] = references.Aft;
            }
            if (!connections || !fitted.activeInHierarchy)
                return false;
            request = new SailControlRequest
            {
                Owner = mount.gameObject,
                Boat = boat,
                Forward = references.Fore,
                Halyard = references.Aft,
                NativeMount = mount,
                Supports = supports,
                SourcePolicy = NativeControlSourcePolicy.MatchingStays,
                FittedSheetSpan = rig ? rig.FittedSheetSpan : (float?)0f,
                Left = connections.angleControllerLeft,
                Right = connections.angleControllerRight,
                Reef = connections.reefController,
            };
            return true;
        }

        public void OnControlsChanged(SailControlSet controls)
        {
            if (!stay.Mount)
                return;
            stay.Mount.leftAngleWinch = controls.Left
                ? new[] { controls.Left }
                : Array.Empty<GPButtonRopeWinch>();
            stay.Mount.rightAngleWinch = controls.Right
                ? new[] { controls.Right }
                : Array.Empty<GPButtonRopeWinch>();
            stay.Mount.reefWinch = controls.Reef
                ? new[] { controls.Reef }
                : Array.Empty<GPButtonRopeWinch>();
        }
    }
}
