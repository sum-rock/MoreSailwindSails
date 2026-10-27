// Owns paired sheets independently of native mast arrays, including retries and cleanup.
using System;
using HarmonyLib;
using MoreSailwindSails.Controls;
using MoreSailwindSails.Controls.Runtime;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal sealed class LooseFootedSpritsailRigging : MonoBehaviour, ISailControlRig
    {
        private static readonly AccessTools.FieldRef<RopeEffect, ClothRope> clothRope =
            AccessTools.FieldRefAccess<RopeEffect, ClothRope>(fieldName: "clothRope");

        [SerializeField]
        internal LooseFootedSpritsailRig Rig;

        [SerializeField]
        internal RopeControllerSailAngleJib Left;

        [SerializeField]
        internal RopeControllerSailAngleJib Right;

        [SerializeField]
        internal JibAngleMaster AngleMaster;
        private readonly Mast[] supports = new Mast[1];
        private BoatRefs boat;
        private Mast mast;
        private OwnedWinch leftWinch,
            rightWinch;
        private RopeEffect leftRope,
            rightRope;
        private float retryAfter;
        private bool warned;
        private int sheetsEnabledFrame;
        private bool sheetsActive;

        internal bool SheetsReady => sheetsActive && Time.frameCount > sheetsEnabledFrame;

        private void Awake()
        {
            leftRope = Left.GetComponent<RopeEffect>();
            rightRope = Right.GetComponent<RopeEffect>();
        }

        private void OnEnable() => retryAfter = 0;

        internal void Refresh()
        {
            var nextMast = Rig.Mast;
            var nextBoat = Rig.Boat;
            if (mast != nextMast || boat != nextBoat)
            {
                ReleaseControls();
                mast = nextMast;
                boat = nextBoat;
                retryAfter = 0;
            }
            if (!TryGetControlRequest(out _))
            {
                ReleaseControls();
                return;
            }
            if (
                (leftWinch == null || !leftWinch.Winch || rightWinch == null || !rightWinch.Winch)
                && Time.unscaledTime >= retryAfter
            )
            {
                try
                {
                    EnsureControl(control: ref leftWinch, role: WinchRole.Left);
                    EnsureControl(control: ref rightWinch, role: WinchRole.Right);
                    WinchControls.Register(boat: boat, owner: gameObject, consumer: this);
                    warned = false;
                }
                catch (Exception exception)
                {
                    ReleaseControls();
                    retryAfter = Time.unscaledTime + 1;
                    if (!warned)
                        Plugin.Log.LogWarning($"Spritsail sheets unavailable: {exception.Message}");
                    warned = true;
                }
            }
            leftRope.ReceiveSailArea(area: Rig.Sail.sailArea);
            rightRope.ReceiveSailArea(area: Rig.Sail.sailArea);
            SetSheetsActive(
                active: Placed(control: leftWinch, controller: Left)
                    && Placed(control: rightWinch, controller: Right)
            );
        }

        private static bool Placed(OwnedWinch control, RopeController controller) =>
            control != null
            && control.Winch
            && control.Winch.gameObject.activeInHierarchy
            && control.Winch.rope == controller;

        private void SetSheetsActive(bool active)
        {
            if (active && !sheetsActive)
                sheetsEnabledFrame = Time.frameCount;
            sheetsActive = active;
            if (AngleMaster)
                AngleMaster.enabled = active;
            if (Left)
                Left.gameObject.SetActive(value: active);
            if (Right)
                Right.gameObject.SetActive(value: active);
            if (!active)
            {
                HideClothRope(rope: leftRope);
                HideClothRope(rope: rightRope);
            }
        }

        private static void HideClothRope(RopeEffect rope)
        {
            // Native 3D ropes are siblings of their controller, not children.
            // Deactivating the controller alone can leave a rope visible at its old seat.
            var cloth = rope ? clothRope(rope) : null;
            if (cloth)
                cloth.gameObject.SetActive(value: false);
        }

        public bool TryGetControlRequest(out SailControlRequest request)
        {
            request = default;
            if (
                !isActiveAndEnabled
                || !boat
                || !mast
                || !mast.gameObject.activeInHierarchy
                || Rig.Mast != mast
                || !Rig.Sail.IsInstalled()
            )
                return false;
            supports[0] = mast;
            request = new SailControlRequest
            {
                Owner = gameObject,
                Boat = boat,
                Forward = mast,
                Halyard = mast,
                Supports = supports,
                SourcePolicy = NativeControlSourcePolicy.FittedMast,
                SourceMast = mast,
                FittedSheetSpan = Rig.FittedSheetSpan,
                Left = Left,
                Right = Right,
            };
            return true;
        }

        public void OnControlsChanged(SailControlSet controls) { }

        private void EnsureControl(ref OwnedWinch control, WinchRole role)
        {
            if (control != null && control.Winch)
                return;
            control?.Retire();
            control = WinchControls.Create(
                boat: boat,
                owner: gameObject,
                parent: boat.transform,
                donorMast: mast,
                role: role,
                label: "Loose Footed Spritsail " + role
            );
        }

        private void ReleaseControls()
        {
            SetSheetsActive(active: false);
            if (boat)
                WinchControls.Unregister(boat: boat, owner: gameObject, consumer: this);
            leftWinch?.Retire();
            rightWinch?.Retire();
            leftWinch = rightWinch = null;
        }

        private void OnDisable() => ReleaseControls();

        private void OnDestroy()
        {
            ReleaseControls();
            // The allocator detaches controllers to the boat before retiring winches.
            // They remain hidden across disables, and must be explicitly destroyed here.
            if (Left)
                Destroy(obj: Left.gameObject);
            if (Right)
                Destroy(obj: Right.gameObject);
        }
    }
}
