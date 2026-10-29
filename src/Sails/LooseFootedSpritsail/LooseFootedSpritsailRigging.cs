// Adapts native sail connections to shared winch placement, with independent halyard retries.
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
        internal SailConnections Connections;

        internal RopeControllerSailAngleJib Left =>
            Connections?.angleControllerLeft as RopeControllerSailAngleJib;
        internal RopeControllerSailAngleJib Right =>
            Connections?.angleControllerRight as RopeControllerSailAngleJib;
        internal RopeControllerSailReef Reef =>
            Connections?.reefController as RopeControllerSailReef;

        [SerializeField]
        internal JibAngleMaster AngleMaster;

        private OwnedWinch reefWinch;
        private Transform reefGuide;
        private float reefRetryAfter;
        private bool reefWarned;
        private LooseFootedSpritsailRopes ropes;
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
            ropes = new LooseFootedSpritsailRopes(
                rig: Rig,
                source: Reef.GetComponent<RopeEffect>()
            );
        }

        private void OnEnable() => retryAfter = reefRetryAfter = 0;

        internal void Refresh()
        {
            var nextMast = Rig.Mast;
            var nextBoat = Rig.Boat;
            if (mast != nextMast || boat != nextBoat)
            {
                ReleaseControls();
                mast = nextMast;
                boat = nextBoat;
                retryAfter = reefRetryAfter = 0;
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
            RefreshReef();
            leftRope.ReceiveSailArea(area: Rig.Sail.sailArea);
            rightRope.ReceiveSailArea(area: Rig.Sail.sailArea);
            SetSheetsActive(
                active: Placed(control: leftWinch, controller: Left)
                    && Placed(control: rightWinch, controller: Right)
            );
        }

        private void RefreshReef()
        {
            if (Time.unscaledTime >= reefRetryAfter)
            {
                reefRetryAfter = Time.unscaledTime + 1;
                reefGuide = FindGuide();
                if (reefGuide)
                {
                    try
                    {
                        EnsureControl(control: ref reefWinch, role: WinchRole.Reef);
                        WinchControls.Register(boat: boat, owner: gameObject, consumer: this);
                        reefWarned = false;
                    }
                    catch (Exception exception)
                    {
                        // Keep the sheets and their claims alive when only this role is missing.
                        if (!reefWarned)
                            Plugin.Log.LogWarning(
                                $"Spritsail halyard unavailable: {exception.Message}"
                            );
                        reefWarned = true;
                    }
                }
            }
            bool active =
                reefGuide
                && reefGuide.gameObject.activeInHierarchy
                && Placed(control: reefWinch, controller: Reef);
            Reef.gameObject.SetActive(value: active);
        }

        private Transform FindGuide()
        {
            var guides = mast.mastReefAtt;
            int order = Rig.Sail.mastOrder;
            if (
                guides != null
                && order >= 0
                && order < guides.Length
                && EligibleGuide(guide: guides[order])
            )
                return guides[order];
            Transform closest = null;
            float distance = float.PositiveInfinity;
            foreach (var candidates in new[] { mast.mastReefAtt, mast.mastReefAttExtension })
            {
                if (candidates == null)
                    continue;
                foreach (var candidate in candidates)
                {
                    if (!EligibleGuide(guide: candidate))
                        continue;
                    float next = (candidate.position - Rig.Frame.position).sqrMagnitude;
                    if (next < distance)
                    {
                        closest = candidate;
                        distance = next;
                    }
                }
            }
            return closest;
        }

        private bool EligibleGuide(Transform guide)
        {
            if (!guide || !guide.gameObject.activeInHierarchy || Rig.ClothGeometry == null)
                return false;
            var point = Rig.Frame.InverseTransformPoint(position: guide.position);
            return Rig.ClothGeometry.CanRoute(guide: point);
        }

        internal void Draw() =>
            ropes.Draw(
                winch: Reef.gameObject.activeInHierarchy && reefGuide ? Reef.transform : null,
                guide: reefGuide
            );

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
                Reef = reefGuide && reefGuide.gameObject.activeInHierarchy ? Reef : null,
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
            if (Reef)
                Reef.gameObject.SetActive(value: false);
            ropes?.Hide();
            reefGuide = null;
            if (boat)
                WinchControls.Unregister(boat: boat, owner: gameObject, consumer: this);
            leftWinch?.Retire();
            rightWinch?.Retire();
            reefWinch?.Retire();
            leftWinch = rightWinch = reefWinch = null;
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
            if (Reef)
                Destroy(obj: Reef.gameObject);
        }
    }
}
