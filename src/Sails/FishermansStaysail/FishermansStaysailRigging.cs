using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Controls;
using MoreSailwindSails.Stays.FishermansStay;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    // Save ownership stays on the registered stay; the physical pivot is the fore mast.
    internal sealed class FishermansStaysailRigging : MonoBehaviour
    {
        internal sealed class MountPair
        {
            internal BoatRefs Boat;
            internal Mast Mount,
                Fore,
                Aft,
                AftBase,
                SheetControlSource;
            internal FishermansStay Stay;
            internal Mast[] ForeSections,
                AftSections;
            internal Transform AftGuide;
            internal bool Active =>
                Mount
                && Mount.gameObject.activeInHierarchy
                && Stay.Available
                && AftGuide
                && AftGuide.gameObject.activeInHierarchy
                && ForeSections.Concat(AftSections).All(m => m && m.gameObject.activeInHierarchy);
        }

        internal MountPair Pair { get; private set; }
        private Sail sail;
        private GameObject controlsRoot;
        private FishermanWinchControls.OwnedWinch[] controls;
        private Transform mastGuide,
            upperGuide;
        private bool bindingDirty;
        private bool controlsDirty;

        internal void Invalidate() => bindingDirty = controlsDirty = true;

        internal static FishermansStaysailRigging For(Sail sail)
        {
            var rig =
                sail.GetComponent<FishermansStaysailRigging>()
                ?? sail.gameObject.AddComponent<FishermansStaysailRigging>();
            rig.sail = sail;
            return rig;
        }

        internal static bool TryResolve(Mast mount, out MountPair pair)
        {
            pair = null;
            if (
                !FishermansStayRegistry.TryFind(mount, out var stay)
                || !stay.Available
                || !mount.gameObject.activeInHierarchy
            )
                return false;
            var refs = stay.References;
            var boat = mount.GetComponentInParent<BoatRefs>();
            var profile = BoatRigCatalog.Find(boat.name);
            int baseId = profile.Base(refs.Aft.orderIndex);
            var foreSections = profile
                .Sections(refs.Fore.orderIndex)
                .Select(id => boat.masts[id])
                .ToArray();
            var aftSections = profile
                .Sections(refs.Aft.orderIndex)
                .Select(id => boat.masts[id])
                .ToArray();
            var aftBase = boat.masts[baseId];
            if (
                foreSections.Concat(aftSections).Any(m => !m || !m.gameObject.activeInHierarchy)
                || !aftBase
                || !aftBase.gameObject.activeInHierarchy
                || !refs.Guide
                || !refs.Guide.gameObject.activeInHierarchy
                || !FirstControl(aftBase.reefWinch)
                || !FirstControl(refs.Donor.leftAngleWinch)
                || !FirstControl(refs.Donor.rightAngleWinch)
            )
                return false;
            pair = new MountPair
            {
                Boat = boat,
                Mount = mount,
                Stay = stay,
                Fore = refs.Fore,
                Aft = refs.Aft,
                AftBase = aftBase,
                ForeSections = foreSections,
                AftSections = aftSections,
                SheetControlSource = refs.Donor,
                AftGuide = refs.Guide,
            };
            return true;
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
                && Pair.Mount == mast
                && Pair.Active
                && (!bindingDirty || GameState.currentShipyard)
            )
                return true;
            if (!TryResolve(mast, out var resolved))
                return false;
            bool changed =
                Pair == null
                || Pair.Mount != resolved.Mount
                || Pair.Aft != resolved.Aft
                || Pair.AftGuide != resolved.AftGuide
                || Pair.AftBase != resolved.AftBase
                || Pair.SheetControlSource != resolved.SheetControlSource;
            controlsDirty |= changed;
            Pair = resolved;
            bindingDirty = false;
            if (changed)
                Plugin.Log.LogInfo(
                    $"FishermansStaysail mast rig: boat={Pair.Boat.name}, fore={Pair.Fore.orderIndex}, aft={Pair.Aft.orderIndex}, upperGuide={Pair.AftGuide.parent.name}/{Pair.AftGuide.name}."
                );
            return true;
        }

        internal static string InstallError(Sail sail, Mast mast)
        {
            if (!TryResolve(mast, out var pair))
                return "(REQUIRES AN ACTIVE FISHERMAN'S STAY)";
            var rig = sail.GetComponent<FishermansStaysailRig>();
            if (!rig || !rig.RefreshFrame())
                return "(STAYSAIL RIG NOT READY)";
            var binding = For(sail);
            binding.ForeSailFrame(out var head, out var axis);
            var scale = sail.cloth.transform.parent.localScale;
            float width = -rig.Corners[0].z * scale.z;
            var bottom = binding.SectionBottom;
            float room = Vector3.Dot(head - bottom, axis);
            float span = Vector3
                .ProjectOnPlane(binding.AftPoint - binding.ForePoint, axis)
                .magnitude;
            return FishermansStaysailInstallationGeometry.FitError(
                width,
                -rig.Corners[2].x * scale.x,
                mast.mastHeight - sail.GetCurrentInstallHeight(),
                room,
                span
            );
        }

        internal Vector3 ForePoint =>
            Pair.Fore.transform.TransformPoint(Pair.Stay.References.Definition.ForePoint);
        internal Vector3 AftPoint =>
            Pair.Aft.transform.TransformPoint(Pair.Stay.References.Definition.AftPoint);
        internal Vector3 ForeAxis => MastAxis(Pair.Fore);

        private Vector3 MastAxis(Mast mast)
        {
            var c = mast.GetComponent<CapsuleCollider>();
            var axis = c.transform.TransformDirection(
                c.direction == 0 ? Vector3.right
                : c.direction == 1 ? Vector3.up
                : Vector3.forward
            );
            return Vector3.Dot(axis, Pair.Boat.transform.up) < 0 ? -axis : axis;
        }

        internal Vector3 SectionBottom
        {
            get
            {
                var c = Pair.Fore.GetComponent<CapsuleCollider>();
                var center = c.transform.TransformPoint(c.center);
                var direction =
                    c.direction == 0 ? Vector3.right
                    : c.direction == 1 ? Vector3.up
                    : Vector3.forward;
                return center
                    - ForeAxis * c.transform.TransformVector(direction * c.height * 0.5f).magnitude;
            }
        }
        internal float HeadSlope =>
            FishermansStaysailInstallationGeometry.HeadSlope(AftPoint - ForePoint, ForeAxis);

        internal void ForeSailFrame(out Vector3 point, out Vector3 axis)
        {
            axis = ForeAxis;
            point =
                ForePoint
                - axis
                    * (
                        FishermansStaysailInstallationGeometry.HeadClearance
                        + Pair.Mount.mastHeight
                        - sail.GetCurrentInstallHeight()
                    );
        }

        internal Vector3 AftReference => AftPoint;

        // Reefing stays on the fitted section: the foot travels up to the head.
        internal Vector3 LuffPoint(Vector3 requested) =>
            ForePoint + ForeAxis * Vector3.Dot(requested - ForePoint, ForeAxis);

        internal void AttachControls()
        {
            var mast = sail.transform.parent ? sail.transform.parent.GetComponent<Mast>() : null;
            if (!Bind(mast))
                return;
            if (!controlsRoot)
            {
                controlsRoot = new GameObject("FishermansStaysail independent controls");
                controlsRoot.SetActive(false);
                controlsRoot.transform.SetParent(Pair.Boat.transform, false);
                mastGuide = new GameObject("FishermansStaysail halyard guide").transform;
                mastGuide.SetParent(controlsRoot.transform, false);
                upperGuide = new GameObject("FishermansStaysail upper halyard guide").transform;
                upperGuide.SetParent(controlsRoot.transform, false);
                controlsRoot.SetActive(true);
            }
            FishermanWinchControls.Reconcile(
                ref controls,
                Pair.Boat,
                sail.gameObject,
                controlsRoot.transform,
                Pair.Fore,
                new[] { Pair.AftBase, Pair.SheetControlSource, Pair.SheetControlSource },
                new[]
                {
                    "FishermansStaysail Halyard",
                    "FishermansStaysail Port sheet",
                    "FishermansStaysail Starboard sheet",
                }
            );
            controlsDirty = false;
            var connections = sail.GetComponent<SailConnections>();
            controls[0].Bind(connections.reefController);
            FishermanWinchControls.BindSheets(
                controls[1],
                connections.angleControllerLeft,
                controls[2],
                connections.angleControllerRight
            );
            connections.colChecker.RegisterBoatWalkCol(mast.walkColMast);
            // Old saves can restore the wider donor range; keep the checker,
            // shipyard description and native sail limits in agreement.
            connections.colChecker.colAngleMin = FishermansStaysailTravel.Clamp(
                connections.colChecker.colAngleMin
            );
            connections.colChecker.colAngleMax = FishermansStaysailTravel.Clamp(
                connections.colChecker.colAngleMax
            );
            sail.minAngle = connections.colChecker.colAngleMin;
            sail.maxAngle = connections.colChecker.colAngleMax;
        }

        internal void UpdateHalyard(Transform attachment)
        {
            if (!controlsRoot || controlsDirty)
                AttachControls();
            if (!controlsRoot || Pair == null || !Pair.Active)
                return;
            // These are sail-owned guides. Never hand the physical mast fitting
            // or a skin bone to RopeEffect, which rotates endpoints with LookAt.
            upperGuide.position = Pair.AftGuide.position;
            mastGuide.position = upperGuide.position - MastAxis(Pair.Aft) * 0.05f;
            var connections = sail.GetComponent<SailConnections>();
            var guide = connections.mastReefAttachment;
            var upper = connections.mastReefAttExtension;
            if (guide.parent != mastGuide)
                guide.SetParent(mastGuide, false);
            guide.localPosition = Vector3.zero;
            if (upper.parent != upperGuide)
                upper.SetParent(upperGuide, false);
            upper.localPosition = Vector3.zero;
            connections.reefController.GetComponent<RopeEffect>().attachment = guide;
            guide.GetComponent<RopeEffect>().attachment = upper;
            upper.GetComponent<RopeEffect>().attachment = attachment;
        }

        internal bool DependsOn(BoatPartOption option)
        {
            if (Pair == null)
                return false;
            return new[] { Pair.Mount }
                .Concat(Pair.ForeSections)
                .Concat(Pair.AftSections)
                .Any(m =>
                    m && (m.GetComponent<BoatPartOption>() == option || option.childMast == m)
                );
        }

        private void OnDestroy()
        {
            if (controls != null)
                foreach (var control in controls)
                    control.Dispose();
            if (controlsRoot)
                Object.Destroy(controlsRoot);
        }
    }
}
