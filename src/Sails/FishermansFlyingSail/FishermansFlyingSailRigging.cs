using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls;
using MoreSailwindSails.Controls.Runtime;
using MoreSailwindSails.Sails.FishermansFlyingSail.Configuration;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Sails.FishermansFlyingSail
{
    // Runtime support belongs to the sail; no synthetic Mast or shipyard part is created.
    internal sealed class FishermansFlyingSailRigging : MonoBehaviour, ISailControlRig
    {
        internal sealed class MountPair
        {
            internal BoatRefs Boat;
            internal Mast Fore,
                Aft;
            internal Transform ForeGuide,
                AftGuide;
            internal Mast ForeGuideMast,
                AftGuideMast;

            internal bool Active =>
                Fore
                && Aft
                && Fore.gameObject.activeInHierarchy
                && Aft.gameObject.activeInHierarchy
                && ForeGuide
                && AftGuide
                && ForeGuideMast
                && AftGuideMast
                && ForeGuideMast.gameObject.activeInHierarchy
                && AftGuideMast.gameObject.activeInHierarchy
                && ForeGuide.gameObject.activeInHierarchy
                && AftGuide.gameObject.activeInHierarchy;
        }

        internal MountPair Pair { get; private set; }
        private Sail sail;
        private SailConnections connections;
        private FishermansFlyingSailRig sailRig;
        private BoatRefs controlBoat;
        private readonly Mast[] controlSupports = new Mast[4];
        private OwnedWinch leftControl,
            rightControl,
            reefControl;

        public bool TryGetControlRequest(out SailControlRequest request)
        {
            request = default;
            if (
                !sail
                || !sail.gameObject.activeInHierarchy
                || Pair == null
                || !Pair.Active
                || sail.transform.parent != Pair.Fore.transform
                || !connections
            )
                return false;
            controlSupports[0] = Pair.Fore;
            controlSupports[1] = Pair.Aft;
            controlSupports[2] = Pair.ForeGuideMast;
            controlSupports[3] = Pair.AftGuideMast;
            request = new SailControlRequest
            {
                Owner = sail.gameObject,
                Boat = Pair.Boat,
                Forward = Pair.Fore,
                Halyard = Pair.Fore,
                Supports = controlSupports,
                SourcePolicy = NativeControlSourcePolicy.FittedMast,
                SourceMast = Pair.Fore,
                FittedSheetSpan = sailRig ? sailRig.FittedSheetSpan : null,
                Left = connections.angleControllerLeft,
                Right = connections.angleControllerRight,
                Reef = connections.reefController,
            };
            return true;
        }

        public void OnControlsChanged(SailControlSet controls)
        {
            // Controllers bind through the request. Physical mast arrays retain native controls.
        }

        internal void ReleaseNativeBindings(Mast mast)
        {
            connections = sail.GetComponent<SailConnections>();
            sailRig = sail.GetComponent<FishermansFlyingSailRig>();
            var boat = mast.GetComponentInParent<BoatRefs>();
            ReleaseNative(controls: mast.leftAngleWinch, boat: boat);
            ReleaseNative(controls: mast.rightAngleWinch, boat: boat);
            ReleaseNative(controls: mast.reefWinch, boat: boat);
        }

        private void ReleaseNative(GPButtonRopeWinch[] controls, BoatRefs boat)
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

        private float controlRetryAfter;
        private bool controlWarning;

        private bool EnsureControls()
        {
            if (Time.unscaledTime < controlRetryAfter)
                return false;
            if (controlBoat && controlBoat != Pair.Boat)
                ReleaseControls();
            controlBoat = Pair.Boat;
            connections = connections ? connections : sail.GetComponent<SailConnections>();
            sailRig = sailRig ? sailRig : sail.GetComponent<FishermansFlyingSailRig>();
            try
            {
                EnsureControl(control: ref leftControl, role: WinchRole.Left);
                EnsureControl(control: ref rightControl, role: WinchRole.Right);
                EnsureControl(control: ref reefControl, role: WinchRole.Reef);
                WinchControls.Register(boat: controlBoat, owner: sail.gameObject, consumer: this);
                controlWarning = false;
                return true;
            }
            catch (Exception exception)
            {
                ReleaseControls();
                controlRetryAfter = Time.unscaledTime + 1f;
                if (!controlWarning)
                    Plugin.Log.LogWarning($"Flying Sail controls unavailable: {exception}");
                controlWarning = true;
                return false;
            }
        }

        private void EnsureControl(ref OwnedWinch control, WinchRole role)
        {
            if (control != null && control.Winch)
                return;
            control?.Retire();
            control = CreateControl(role: role);
        }

        private OwnedWinch CreateControl(WinchRole role) =>
            WinchControls.Create(
                boat: controlBoat,
                owner: sail.gameObject,
                parent: controlBoat.transform,
                donorMast: Pair.Fore,
                role: role,
                label: "FishermansFlyingSail " + role
            );

        private void ReleaseControls()
        {
            if (controlBoat)
                WinchControls.Unregister(boat: controlBoat, owner: gameObject, consumer: this);
            leftControl?.Retire();
            rightControl?.Retire();
            reefControl?.Retire();
            leftControl = rightControl = reefControl = null;
            controlBoat = null;
        }

        private void OnEnable() => controlsDirty = true;

        private void OnDisable() => ReleaseControls();

        private GameObject controlsRoot;
        private Transform mastGuide,
            upperGuide;
        private bool bindingDirty;
        private bool controlsDirty;

        internal void Invalidate() => bindingDirty = controlsDirty = true;

        internal static FishermansFlyingSailRigging For(Sail sail)
        {
            var rig =
                sail.GetComponent<FishermansFlyingSailRigging>()
                ?? sail.gameObject.AddComponent<FishermansFlyingSailRigging>();
            rig.sail = sail;
            return rig;
        }

        internal static bool TryResolve(Mast fore, out MountPair pair)
        {
            pair = null;
            if (
                !fore
                || fore.onlyStaysails
                || fore.onlySquareSails
                || !fore.gameObject.activeInHierarchy
            )
                return false;
            var boat = fore.GetComponentInParent<BoatRefs>();
            var profile = boat ? FlyingSailCatalog.Find(boatName: boat.name) : null;
            if (profile == null)
                return false;
            var masts = boat.GetComponentsInChildren<Mast>(true)
                .GroupBy(m => m.orderIndex)
                .ToDictionary(g => g.Key, g => g.First());
            float nearest = float.PositiveInfinity;
            var groups = profile.MastPairs(fore.orderIndex);
            foreach (var group in groups)
            {
                var foreSections = group
                    .SelectMany(v => v.ForeSections)
                    .Distinct()
                    .Where(masts.ContainsKey)
                    .Select(id => masts[id])
                    .ToArray();
                var aftSections = group
                    .SelectMany(v => v.AftSections)
                    .Distinct()
                    .Where(masts.ContainsKey)
                    .Select(id => masts[id])
                    .ToArray();
                var fg = HighestGuide(foreSections, boat, out var foreGuideMast);
                var ag = HighestGuide(aftSections, boat, out var aftGuideMast);
                if (!fg || !ag)
                    continue;
                var aft =
                    aftSections.FirstOrDefault(m =>
                        m.orderIndex == group.Key && m.gameObject.activeInHierarchy
                    ) ?? aftSections.FirstOrDefault(m => m.gameObject.activeInHierarchy);
                if (
                    !aft
                    || !fore.GetComponent<CapsuleCollider>()
                    || !aft.GetComponent<CapsuleCollider>()
                )
                    continue;
                float distance = Vector3
                    .ProjectOnPlane(ag.position - fg.position, boat.transform.up)
                    .sqrMagnitude;
                if (distance < 0.25f || distance >= nearest)
                    continue;
                nearest = distance;
                pair = new MountPair
                {
                    Boat = boat,
                    Fore = fore,
                    Aft = aft,
                    ForeGuide = fg,
                    AftGuide = ag,
                    ForeGuideMast = foreGuideMast,
                    AftGuideMast = aftGuideMast,
                };
            }
            return pair != null;
        }

        private static Transform HighestGuide(Mast[] sections, BoatRefs boat, out Mast owner)
        {
            var candidates = sections
                .SelectMany(section =>
                    (section.mastReefAtt ?? new Transform[0])
                        .Concat(section.mastReefAttExtension ?? new Transform[0])
                        .Where(guide => guide)
                        .Select(guide => Tuple.Create(section, guide))
                )
                .Distinct()
                .ToArray();
            var states = candidates
                .Select(c => new FishermansFlyingSailSheetGuideState(
                    c.Item2.position - boat.transform.position,
                    c.Item1.gameObject.activeInHierarchy,
                    c.Item2.gameObject.activeInHierarchy
                ))
                .ToArray();
            int selected = FishermansFlyingSailFrameGeometry.HighestGuideIndex(
                states,
                boat.transform.up
            );
            owner = selected >= 0 ? candidates[selected].Item1 : null;
            return selected >= 0 ? candidates[selected].Item2 : null;
        }

        private static GPButtonRopeWinch FirstControl(GPButtonRopeWinch[] winches) =>
            winches?.FirstOrDefault(w =>
                w && w.GetComponent<Renderer>() && w.GetComponent<Collider>()
            );

        internal bool Bind(Mast mast)
        {
            // A removal preview temporarily enables two mutually exclusive options.
            // Keep the current support until that order finishes, so the removal
            // guard cannot be evaded by silently rebinding to the preview option.
            if (
                Pair != null
                && Pair.Fore == mast
                && Pair.Active
                && (!bindingDirty || GameState.currentShipyard)
            )
                return true;
            if (!TryResolve(mast, out var resolved))
                return false;
            bool changed =
                Pair == null
                || Pair.Fore != resolved.Fore
                || Pair.Aft != resolved.Aft
                || Pair.ForeGuide != resolved.ForeGuide
                || Pair.AftGuide != resolved.AftGuide;
            controlsDirty |= changed;
            Pair = resolved;
            bindingDirty = false;
            if (changed)
                Plugin.Log.LogInfo(
                    $"FishermansFlyingSail mast rig: boat={Pair.Boat.name}, fore={Pair.Fore.orderIndex}, aft={Pair.Aft.orderIndex}, upperGuide={Pair.AftGuideMast.name}/{Pair.AftGuide.parent.name}/{Pair.AftGuide.name}."
                );
            return true;
        }

        internal static string InstallError(Sail sail, Mast mast)
        {
            var pair = sail.GetComponent<FishermansFlyingSailRigging>()?.Pair;
            if (pair == null || pair.Fore != mast || !pair.Active)
                if (!TryResolve(mast, out pair))
                    return "(REQUIRES AN ACTIVE AFT MAST WITH HALYARD GUIDES)";
            var rig = sail.GetComponent<FishermansFlyingSailRig>();
            var scale = sail.cloth.transform.parent.localScale;
            var head = mast.transform.TransformPoint(
                new Vector3(0, 0, sail.GetCurrentInstallHeight() - mast.mastHeight)
            );
            MastFrame(pair, head, out var mastPoint, out var axis);
            var aft = FishermansFlyingSailFrameGeometry.AftDirection(
                mastPoint,
                axis,
                pair.AftGuide.position
            );
            var foreHead = FishermansFlyingSailFrameGeometry.LuffPivot(
                mastPoint,
                axis,
                pair.AftGuide.position,
                MastRadius(mast)
            );
            var aftHead =
                foreHead
                + axis * ((rig.Corners[1].x - rig.Corners[0].x) * scale.x)
                + aft * ((rig.Corners[1].z - rig.Corners[0].z) * scale.z);
            float span = Vector3.Dot(pair.AftGuide.position - mastPoint, aft);
            return FishermansFlyingSailMastInstallationGeometry.FitError(
                Vector3.Dot(aftHead - mastPoint, aft),
                pair.Boat.transform.InverseTransformPoint(foreHead).y,
                pair.Boat.transform.InverseTransformPoint(aftHead).y,
                pair.Boat.transform.InverseTransformPoint(pair.ForeGuide.position).y,
                pair.Boat.transform.InverseTransformPoint(pair.AftGuide.position).y,
                span
            );
        }

        internal static float MastRadius(Mast mast)
        {
            var collider = mast.GetComponent<CapsuleCollider>();
            var scale = collider.transform.lossyScale;
            return collider.radius
                * Mathf.Max(
                    Mathf.Abs(collider.direction == 0 ? scale.y : scale.x),
                    Mathf.Abs(collider.direction == 2 ? scale.y : scale.z)
                );
        }

        internal void LuffSailFrame(out Vector3 point, out Vector3 axis)
        {
            var mast = Pair.Fore;
            var heightPoint = mast.transform.TransformPoint(
                new Vector3(0, 0, sail.GetCurrentInstallHeight() - mast.mastHeight)
            );
            MastFrame(Pair, heightPoint, out point, out axis);
            point = FishermansFlyingSailFrameGeometry.LuffPivot(
                point,
                axis,
                AftReference,
                MastRadius(mast)
            );
        }

        private static void MastFrame(
            MountPair pair,
            Vector3 heightPoint,
            out Vector3 point,
            out Vector3 axis
        )
        {
            var mast = pair.Fore;
            var collider = mast.GetComponent<CapsuleCollider>();
            var localAxis =
                collider.direction == 0 ? Vector3.right
                : collider.direction == 1 ? Vector3.up
                : Vector3.forward;
            var bottom = pair.Boat.transform.InverseTransformPoint(
                collider.transform.TransformPoint(
                    collider.center - localAxis * collider.height * 0.5f
                )
            );
            var top = pair.Boat.transform.InverseTransformPoint(
                collider.transform.TransformPoint(
                    collider.center + localAxis * collider.height * 0.5f
                )
            );
            if (bottom.y > top.y)
            {
                var swap = bottom;
                bottom = top;
                top = swap;
            }
            point = pair.Boat.transform.TransformPoint(
                FishermansFlyingSailMastInstallationGeometry.AtHeight(
                    bottom,
                    top,
                    pair.Boat.transform.InverseTransformPoint(heightPoint).y
                )
            );
            axis = pair.Boat.transform.TransformDirection((top - bottom).normalized);
        }

        internal Vector3 AftReference => Pair.AftGuide.position;

        internal Vector3 DeckPoint
        {
            get
            {
                LuffSailFrame(out var top, out var axis);
                // Native mastHeight ends at the usable mast base. The hoist winch
                // supplies the deck datum even when fitting to a topmast section.
                float y =
                    Pair.Boat.transform.InverseTransformPoint(
                        FirstControl(Pair.Fore.reefWinch).transform.position
                    ).y - 0.5f;
                var localTop = Pair.Boat.transform.InverseTransformPoint(top);
                var localAxis = Pair.Boat.transform.InverseTransformDirection(axis);
                return Pair.Boat.transform.TransformPoint(
                    FishermansFlyingSailMastInstallationGeometry.AtHeight(
                        localTop,
                        localTop + localAxis,
                        y
                    )
                );
            }
        }

        internal void AttachControls()
        {
            var mast = sail.transform.parent ? sail.transform.parent.GetComponent<Mast>() : null;
            if (!Bind(mast))
                return;
            if (!controlsRoot)
            {
                controlsRoot = new GameObject("FishermansFlyingSail rope guides");
                controlsRoot.SetActive(false);
                controlsRoot.transform.SetParent(Pair.Boat.transform, false);
                mastGuide = new GameObject("FishermansFlyingSail halyard guide").transform;
                mastGuide.SetParent(controlsRoot.transform, false);
                upperGuide = new GameObject("FishermansFlyingSail upper halyard guide").transform;
                upperGuide.SetParent(controlsRoot.transform, false);
                controlsRoot.SetActive(true);
            }
            if (!EnsureControls())
                return;
            controlsDirty = false;
            var connections = sail.GetComponent<SailConnections>();
            connections.colChecker.RegisterBoatWalkCol(mast.walkColMast);
            // Old saves can restore the wider donor range; keep the checker,
            // shipyard description and native sail limits in agreement.
            connections.colChecker.colAngleMin = FishermansFlyingSailTravel.Clamp(
                connections.colChecker.colAngleMin
            );
            connections.colChecker.colAngleMax = FishermansFlyingSailTravel.Clamp(
                connections.colChecker.colAngleMax
            );
            sail.minAngle = connections.colChecker.colAngleMin;
            sail.maxAngle = connections.colChecker.colAngleMax;
        }

        internal void UpdateHalyard(Transform[] attachments, bool struck)
        {
            if (
                !controlsRoot
                || controlsDirty
                || leftControl == null
                || !leftControl.Winch
                || rightControl == null
                || !rightControl.Winch
                || reefControl == null
                || !reefControl.Winch
            )
                AttachControls();
            if (!controlsRoot)
                return;
            mastGuide.position = Pair.ForeGuide.position;
            upperGuide.position = Pair.ForeGuide.position + Pair.Boat.transform.up * 0.05f;
            var connections = sail.GetComponent<SailConnections>();
            var guide = connections.mastReefAttachment;
            var upper = connections.mastReefAttExtension;
            guide.SetParent(mastGuide, false);
            guide.localPosition = Vector3.zero;
            upper.SetParent(upperGuide, false);
            upper.localPosition = Vector3.zero;
            connections.reefController.GetComponent<RopeEffect>().attachment = guide;
            guide.GetComponent<RopeEffect>().attachment = upper;
            upper.GetComponent<RopeEffect>().attachment = attachments[0];
            if (struck)
                attachments[0].position = Pair.ForeGuide.position;
        }

        internal bool DependsOn(BoatPartOption option)
        {
            if (Pair == null)
                return false;
            return new[] { Pair.Fore, Pair.Aft, Pair.ForeGuideMast, Pair.AftGuideMast }.Any(m =>
                m && (m.GetComponent<BoatPartOption>() == option || option.childMast == m)
            );
        }

        private void OnDestroy()
        {
            ReleaseControls();
            if (controlsRoot)
                Object.Destroy(controlsRoot);
        }
    }
}
