using System;
using System.Collections.Generic;
using System.Linq;
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
        private Transform mastGuide,
            upperGuide;
        private SpritsailMastSurface mastSurface;
        private Mast surfaceMast;
        internal float SocketRadius { get; private set; }
        internal float SocketGap { get; private set; }
        private bool bindingDirty;

        internal void Invalidate() => bindingDirty = true;

        internal static LooseFootedSpritsailRigging For(Sail sail)
        {
            var rig =
                sail.GetComponent<LooseFootedSpritsailRigging>()
                ?? sail.gameObject.AddComponent<LooseFootedSpritsailRigging>();
            rig.sail = sail;
            return rig;
        }

        internal static bool TryResolve(Mast fore, out LooseFootedSpritsailMount pair, int slot = 0)
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
            var collider = fore.GetComponent<CapsuleCollider>();
            if (
                !boat
                || !collider
                || !SpritsailMastAlignment.IsUsable(
                    boatLocalAxis: boat.transform.InverseTransformVector(
                        vector: collider.transform.TransformVector(
                            vector: SpritsailMastAlignment.LocalAxis(direction: collider.direction)
                        )
                    )
                )
            )
                return false;
            if (
                !SpritsailNativeBinding.TryGuides(
                    boat: boat,
                    mast: collider,
                    primary: fore.mastReefAtt,
                    extensions: fore.mastReefAttExtension,
                    slot: slot,
                    lower: out var lower,
                    upper: out var upper
                )
            )
                return false;
            pair = new LooseFootedSpritsailMount
            {
                Boat = boat,
                Mast = fore,
                LowerGuide = lower,
                Guide = upper,
                Parts = SpritsailNativeBinding.SupportParts(mast: fore, lower: lower, upper: upper),
            };
            return pair.Active;
        }

        internal bool Bind(Mast mast)
        {
            var binding = sail.GetComponent<SpritsailNativeBinding>();
            if (binding && binding.Mast == mast && binding.Error != null)
                return false;
            int slot = binding && binding.Mast == mast ? binding.Slot : 0;
            if (Support != null && Support.Mast == mast && Support.Active && !bindingDirty)
                return true;
            if (!TryResolve(fore: mast, pair: out var resolved, slot: slot))
                return false;
            bool changed =
                Support == null
                || Support.Mast != resolved.Mast
                || Support.Guide != resolved.Guide
                || Support.LowerGuide != resolved.LowerGuide
                || !Support.Parts.SequenceEqual(second: resolved.Parts);
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
            var binding = sail.GetComponent<SpritsailNativeBinding>();
            if (!binding || binding.Mast != mast)
                return "(NATIVE MAST CONTROLS NOT READY)";
            if (binding.Error != null)
                return binding.Error;
            int slot = binding.Slot;
            if (!TryResolve(fore: mast, pair: out var pair, slot: slot))
                return "(REQUIRES AN ACTIVE PHYSICAL MAST WITH NATIVE GUIDES)";
            var head = mast.transform.TransformPoint(
                position: new Vector3(0, 0, sail.GetCurrentInstallHeight() - mast.mastHeight)
            );
            MastFrame(pair: pair, heightPoint: head, point: out head, axis: out var axis);
            if (Vector3.Dot(head - pair.Guide.position, axis) > 0.05f)
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

        internal Vector3 AftDirection(Vector3 axis) =>
            Vector3
                .ProjectOnPlane(vector: -Support.Boat.transform.forward, planeNormal: axis)
                .normalized;

        // Native binding owns the winches. These objects only route the sprit purchase.
        private void EnsureGuides()
        {
            if (controlsRoot)
                return;
            controlsRoot = new GameObject(name: "LooseFootedSpritsail halyard guides");
            controlsRoot.transform.SetParent(
                parent: Support.Boat.transform,
                worldPositionStays: false
            );
            mastGuide = new GameObject(name: "LooseFootedSpritsail halyard guide").transform;
            mastGuide.SetParent(parent: controlsRoot.transform, worldPositionStays: false);
            upperGuide = new GameObject(name: "LooseFootedSpritsail upper halyard guide").transform;
            upperGuide.SetParent(parent: controlsRoot.transform, worldPositionStays: false);
        }

        internal void UpdateHalyard(Transform attachment)
        {
            EnsureGuides();
            mastGuide.position = Support.LowerGuide.position;
            var axis = Support.Mast.GetComponent<CapsuleCollider>();
            var up = axis
                .transform.TransformDirection(
                    direction: SpritsailMastAlignment.LocalAxis(direction: axis.direction)
                )
                .normalized;
            if (Vector3.Dot(up, Support.Boat.transform.up) < 0)
                up = -up;
            upperGuide.position =
                Support.Guide.position
                + (Support.Guide == Support.LowerGuide ? up * 0.05f : Vector3.zero);
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
            return Support.Parts.Contains(value: option);
        }

        private void OnDestroy()
        {
            if (controlsRoot)
                Object.Destroy(obj: controlsRoot);
        }
    }
}
