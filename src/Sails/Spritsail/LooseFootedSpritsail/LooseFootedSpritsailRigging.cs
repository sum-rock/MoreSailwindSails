using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail
{
    // Runtime support belongs to the sail; no synthetic Mast or shipyard part is created.
    internal sealed class LooseFootedSpritsailRigging : MonoBehaviour
    {
        internal LooseFootedSpritsailMount Support { get; private set; }
        private Sail sail;
        private GameObject controlsRoot;
        private FishermanWinchControls.OwnedWinch[] controls;
        private Transform mastGuide,
            upperGuide;
        private SpritsailMastSurface mastSurface;
        private Mast surfaceMast;
        internal float SocketRadius { get; private set; }
        internal float SocketGap { get; private set; }
        private bool bindingDirty;
        private bool controlsDirty;

        internal void Invalidate() => bindingDirty = controlsDirty = true;

        internal static LooseFootedSpritsailRigging For(Sail sail)
        {
            var rig =
                sail.GetComponent<LooseFootedSpritsailRigging>()
                ?? sail.gameObject.AddComponent<LooseFootedSpritsailRigging>();
            rig.sail = sail;
            return rig;
        }

        internal static bool TryResolve(Mast fore, out LooseFootedSpritsailMount pair)
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
            var profile = boat ? BoatRigCatalog.Find(boatName: boat.name) : null;
            // Every registered boat profile can supply authored mast ancestry and guides.
            if (profile == null)
                return false;
            if (
                !profile.MastParents.ContainsKey(key: fore.orderIndex)
                || !IsUpright(mast: fore, boat: boat)
                || !FishermanWinchControls.BootstrapReady(boat: boat, forward: fore, halyard: fore)
            )
                return false;
            var masts = boat.GetComponentsInChildren<Mast>(includeInactive: true)
                .Where(predicate: m => m.gameObject.activeInHierarchy)
                .GroupBy(keySelector: m => m.orderIndex)
                .ToDictionary(keySelector: g => g.Key, elementSelector: g => g.First());
            if (
                profile
                    .Sections(section: fore.orderIndex)
                    .Any(predicate: id =>
                        !masts.TryGetValue(key: id, value: out var support)
                        || !IsUpright(mast: support, boat: boat)
                    )
            )
                return false;
            Transform highest = null;
            Mast[] selected = null;
            float height = float.NegativeInfinity;
            foreach (var section in masts.Values)
            {
                if (!profile.MastParents.ContainsKey(key: section.orderIndex))
                    continue;
                var chain = profile.Sections(section: section.orderIndex);
                if (
                    !chain.Contains(value: fore.orderIndex)
                    || chain.Any(predicate: id =>
                        !masts.ContainsKey(key: id) || !IsUpright(mast: masts[id], boat: boat)
                    )
                )
                    continue;
                foreach (
                    var guide in (section.mastReefAtt ?? Array.Empty<Transform>()).Concat(
                        second: section.mastReefAttExtension ?? Array.Empty<Transform>()
                    )
                )
                {
                    if (!guide || !guide.gameObject.activeInHierarchy)
                        continue;
                    float y = boat.transform.InverseTransformPoint(position: guide.position).y;
                    if (y <= height)
                        continue;
                    height = y;
                    highest = guide;
                    selected = chain.Select(selector: id => masts[id]).ToArray();
                }
            }
            if (!highest)
                return false;
            pair = new LooseFootedSpritsailMount
            {
                Boat = boat,
                Mast = fore,
                Guide = highest,
                Sections = selected,
            };
            return true;
        }

        private static GPButtonRopeWinch FirstControl(GPButtonRopeWinch[] winches) =>
            winches?.FirstOrDefault(predicate: w =>
                w && w.GetComponent<Renderer>() && w.GetComponent<Collider>()
            );

        private static bool IsUpright(Mast mast, BoatRefs boat)
        {
            var collider = mast ? mast.GetComponent<CapsuleCollider>() : null;
            if (!collider || !boat)
                return false;
            var localAxis =
                collider.direction == 0 ? Vector3.right
                : collider.direction == 1 ? Vector3.up
                : Vector3.forward;
            return SpritsailMastAlignment.IsUpright(
                boatLocalAxis: boat.transform.InverseTransformVector(
                    vector: collider.transform.TransformVector(vector: localAxis)
                )
            );
        }

        internal bool Bind(Mast mast)
        {
            // A removal preview temporarily enables two mutually exclusive options.
            // Keep the current support until that order finishes, so the removal
            // guard cannot be evaded by silently rebinding to the preview option.
            if (
                Support != null
                && Support.Mast == mast
                && Support.Active
                && Support.Sections.All(predicate: section =>
                    IsUpright(mast: section, boat: Support.Boat)
                )
                && (!bindingDirty || GameState.currentShipyard)
            )
                return true;
            if (!TryResolve(fore: mast, pair: out var resolved))
                return false;
            bool changed =
                Support == null
                || Support.Mast != resolved.Mast
                || Support.Guide != resolved.Guide
                || !Support.Sections.SequenceEqual(second: resolved.Sections);
            controlsDirty |= changed;
            Support = resolved;
            bindingDirty = false;
            if (changed)
                Plugin.Log.LogInfo(
                    data: $"LooseFootedSpritsail mast rig: boat={Support.Boat.name}, mast={Support.Mast.orderIndex}, guide={Support.Guide.name}."
                );
            return true;
        }

        internal static string InstallError(Sail sail, Mast mast)
        {
            var pair = sail.GetComponent<LooseFootedSpritsailRigging>()?.Support;
            if (pair == null || pair.Mast != mast || !pair.Active)
                if (!TryResolve(fore: mast, pair: out pair))
                    return "(REQUIRES A SUPPORTED UPRIGHT MAST WITH ACTIVE GUIDES AND CONTROL TEMPLATES)";
            if (pair.Sections.Any(predicate: section => !IsUpright(mast: section, boat: pair.Boat)))
                return "(SPRITSAILS REQUIRE AN UPRIGHT MAST)";
            var head = mast.transform.TransformPoint(
                position: new Vector3(0, 0, sail.GetCurrentInstallHeight() - mast.mastHeight)
            );
            if (Vector3.Dot(head - pair.Guide.position, pair.Boat.transform.up) > 0.05f)
                return "(LUFF ABOVE MAST GUIDE)";
            var scale = sail.cloth.transform.parent.localScale;
            if (
                !SpritsailDeployment.Finite(value: scale.x)
                || !SpritsailDeployment.Finite(value: scale.y)
                || !SpritsailDeployment.Finite(value: scale.z)
                || scale.x <= 0
                || scale.y <= 0
                || scale.z <= 0
            )
                return "(INVALID SAIL SCALE)";
            var rig = sail.GetComponent<LooseFootedSpritsailRig>();
            var worldScale = sail.cloth.transform.lossyScale;
            var corners = Array.ConvertAll(
                rig.Corners,
                corner => Vector3.Scale(corner, worldScale)
            );
            var struck = SpritsailDeployment.Evaluate(corners: corners, unroll: 0);
            float rise =
                SpritsailDeployment.PurchasePoint(heel: struck.Heel, tip: struck.Tip).x
                - corners[0].x;
            var mastCollider = mast.GetComponent<CapsuleCollider>();
            var localAxis =
                mastCollider.direction == 0 ? Vector3.right
                : mastCollider.direction == 1 ? Vector3.up
                : Vector3.forward;
            var axis = mast.transform.TransformDirection(direction: localAxis).normalized;
            if (Vector3.Dot(axis, pair.Boat.transform.up) < 0)
                axis = -axis;
            if (Vector3.Dot(head + axis * rise - pair.Guide.position, axis) > -0.05f)
                return "(SPRIT HOIST REQUIRES A HIGHER MAST GUIDE)";
            return null;
        }

        internal static float MastRadius(Mast mast)
        {
            var collider = mast.GetComponent<CapsuleCollider>();
            var scale = collider.transform.lossyScale;
            return collider.radius
                * Mathf.Max(
                    a: Mathf.Abs(f: collider.direction == 0 ? scale.y : scale.x),
                    b: Mathf.Abs(f: collider.direction == 2 ? scale.y : scale.z)
                );
        }

        internal void LuffSailFrame(out Vector3 point, out Vector3 axis, out Vector3 hingePoint)
        {
            var mast = Support.Mast;
            var heightPoint = mast.transform.TransformPoint(
                position: new Vector3(0, 0, sail.GetCurrentInstallHeight() - mast.mastHeight)
            );
            MastFrame(pair: Support, heightPoint: heightPoint, point: out point, axis: out axis);
            hingePoint = point;
            var starboard = Vector3
                .ProjectOnPlane(vector: Support.Boat.transform.right, planeNormal: axis)
                .normalized;
            var rig = sail.GetComponent<LooseFootedSpritsailRig>();
            if (surfaceMast != mast || mastSurface == null)
            {
                surfaceMast = mast;
                mastSurface = new SpritsailMastSurface(mast: mast);
            }
            float down =
                (rig.Corners[0].x - rig.Corners[2].x)
                * sail.cloth.transform.lossyScale.x
                * (1 - SpritsailDeployment.SocketLuffFraction);
            SocketRadius = mastSurface.Radius(
                center: point - axis * down,
                direction: starboard,
                fallback: MastRadius(mast: mast)
            );
            SocketGap =
                SpritsailSnotterGeometry.LuffDistance(mastRadius: SocketRadius) - SocketRadius;
            point += starboard * (SocketRadius + SocketGap);
        }

        private static void MastFrame(
            LooseFootedSpritsailMount pair,
            Vector3 heightPoint,
            out Vector3 point,
            out Vector3 axis
        )
        {
            var mast = pair.Mast;
            var collider = mast.GetComponent<CapsuleCollider>();
            var localAxis =
                collider.direction == 0 ? Vector3.right
                : collider.direction == 1 ? Vector3.up
                : Vector3.forward;
            var bottom = pair.Boat.transform.InverseTransformPoint(
                position: collider.transform.TransformPoint(
                    position: collider.center - localAxis * collider.height * 0.5f
                )
            );
            var top = pair.Boat.transform.InverseTransformPoint(
                position: collider.transform.TransformPoint(
                    position: collider.center + localAxis * collider.height * 0.5f
                )
            );
            if (bottom.y > top.y)
            {
                var swap = bottom;
                bottom = top;
                top = swap;
            }
            point = pair.Boat.transform.TransformPoint(
                position: LooseFootedSpritsailMastInstallationGeometry.AtHeight(
                    bottom: bottom,
                    top: top,
                    height: pair.Boat.transform.InverseTransformPoint(position: heightPoint).y
                )
            );
            axis = pair.Boat.transform.TransformDirection(direction: (top - bottom).normalized);
        }

        internal Vector3 AftDirection
        {
            get
            {
                LuffSailFrame(point: out _, axis: out var axis, hingePoint: out _);
                return Vector3
                    .ProjectOnPlane(vector: -Support.Boat.transform.forward, planeNormal: axis)
                    .normalized;
            }
        }

        internal void AttachControls()
        {
            var mast = sail.transform.parent ? sail.transform.parent.GetComponent<Mast>() : null;
            if (!Bind(mast: mast))
                return;
            if (!controlsRoot)
            {
                controlsRoot = new GameObject(name: "LooseFootedSpritsail independent controls");
                controlsRoot.SetActive(value: false);
                controlsRoot.transform.SetParent(
                    parent: Support.Boat.transform,
                    worldPositionStays: false
                );
                mastGuide = new GameObject(name: "LooseFootedSpritsail halyard guide").transform;
                mastGuide.SetParent(parent: controlsRoot.transform, worldPositionStays: false);
                upperGuide = new GameObject(
                    name: "LooseFootedSpritsail upper halyard guide"
                ).transform;
                upperGuide.SetParent(parent: controlsRoot.transform, worldPositionStays: false);
                controlsRoot.SetActive(value: true);
            }
            FishermanWinchControls.Reconcile(
                current: ref controls,
                boat: Support.Boat,
                owner: sail.gameObject,
                parent: controlsRoot.transform,
                forward: Support.Mast,
                donors: new[] { Support.Mast, Support.Mast, Support.Mast },
                labels: new[]
                {
                    "LooseFootedSpritsail Snotter",
                    "LooseFootedSpritsail Port sheet",
                    "LooseFootedSpritsail Starboard sheet",
                }
            );
            controlsDirty = false;
            var connections = sail.GetComponent<SailConnections>();
            controls[0].Bind(controller: connections.reefController);
            FishermanWinchControls.BindSheets(
                port: controls[1],
                portRope: connections.angleControllerLeft,
                starboard: controls[2],
                starboardRope: connections.angleControllerRight
            );
            connections.colChecker.RegisterBoatWalkCol(walkCol: mast.walkColMast);
            // Keep collision, shipyard description and native sheet limits
            // within the shared spritsail envelope.
            connections.colChecker.colAngleMin = SpritsailTravel.Clamp(
                angle: connections.colChecker.colAngleMin
            );
            connections.colChecker.colAngleMax = SpritsailTravel.Clamp(
                angle: connections.colChecker.colAngleMax
            );
            sail.minAngle = connections.colChecker.colAngleMin;
            sail.maxAngle = connections.colChecker.colAngleMax;
        }

        internal void UpdateHalyard(Transform attachment)
        {
            if (!controlsRoot || controlsDirty)
                AttachControls();
            if (!controlsRoot)
                return;
            mastGuide.position = Support.Guide.position;
            upperGuide.position = Support.Guide.position + Support.Boat.transform.up * 0.05f;
            var connections = sail.GetComponent<SailConnections>();
            var guide = connections.mastReefAttachment;
            var upper = connections.mastReefAttExtension;
            guide.SetParent(parent: mastGuide, worldPositionStays: false);
            guide.localPosition = Vector3.zero;
            upper.SetParent(parent: upperGuide, worldPositionStays: false);
            upper.localPosition = Vector3.zero;
            connections.reefController.GetComponent<RopeEffect>().attachment = guide;
            guide.GetComponent<RopeEffect>().attachment = upper;
            upper.GetComponent<RopeEffect>().attachment = attachment;
        }

        internal bool DependsOn(BoatPartOption option)
        {
            if (Support == null)
                return false;
            return Support.Sections.Any(predicate: m =>
                m && (m.GetComponent<BoatPartOption>() == option || option.childMast == m)
            );
        }

        private void OnDestroy()
        {
            if (controls != null)
                foreach (var control in controls)
                    control.Dispose();
            if (controlsRoot)
                Object.Destroy(obj: controlsRoot);
        }
    }
}
