using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls;
using MoreSailwindSails.Controls.Configuration;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Stays.FishermansStay
{
    // Owns one registered stay mount, its controls and its visual/walking geometry.
    internal sealed class FishermansStay
    {
        internal const string DisplayName = "Fisherman's Stay";
        internal Mast Mount;
        internal BoatPartOption Option;
        internal GameObject WalkObject;
        private readonly BoatRefs boat;
        private readonly FishermansStayReferences[] configurations;
        private FishermansStayReferences references;
        internal FishermansStayReferences References => references;
        internal Vector3 HalyardPoint
        {
            get
            {
                if (!references.Definition.AlignGuideHeightToAftAnchor)
                    return references.Guide.position;
                var spar = references.Aft.GetComponent<CapsuleCollider>();
                var axis =
                    spar.direction == 0 ? Vector3.right
                    : spar.direction == 1 ? Vector3.up
                    : Vector3.forward;
                return FishermansStayGeometry.AlignGuideHeight(
                    guide: references.Guide.position,
                    head: references.Aft.transform.TransformPoint(references.Definition.AftPoint),
                    mastAxis: references.Aft.transform.TransformDirection(axis)
                );
            }
        }
        private Mast source => references.Donor;
        private FishermansStayVisual visual;
        private readonly List<Mesh> ownedMeshes = new List<Mesh>();
        private bool warned;
        private bool fits;
        private readonly List<Tuple<Transform, Transform>> anchors =
            new List<Tuple<Transform, Transform>>();
        private readonly List<FishermanWinchControls.OwnedWinch> controls =
            new List<FishermanWinchControls.OwnedWinch>();

        internal FishermansStay(BoatRefs boat, FishermansStayReferences[] configurations)
        {
            this.boat = boat;
            this.configurations = configurations;
            references =
                configurations.FirstOrDefault(r => r.Fitted(boatRoot: boat.transform))
                ?? configurations[0];
        }

        internal void Create()
        {
            var definition = references.Definition;
            // Native hinges use the mount's boat-local position as their anchor.
            var root = NewInactive(
                $"FishermansStay {definition.MountIndex} {definition.Label}",
                boat.transform
            );
            Mount = root.AddComponent<Mast>();
            Mount.orderIndex = definition.MountIndex;
            Mount.maxSails = 1;
            Mount.onlyStaysails = true;
            Mount.shipRigidbody = boat.GetComponent<Rigidbody>();
            Mount.startSailPrefab = null;
            Mount.startSailPrefabs = new GameObject[0];
            Mount.startSailsHeightOffsets = new float[0];
            Mount.sails = new List<GameObject>();
            Mount.mastCols = references
                .Required.Select(o => o.GetComponent<CapsuleCollider>())
                .Where(c => c)
                .ToArray();
            Mount.startingSailColor = source.startingSailColor;
            FishermanWinchControls.Configure(
                boat,
                Mount.gameObject,
                references.Fore,
                references.Aft,
                Mount
            );
            Mount.leftAngleWinch = CloneWinches(source, WinchRole.Left, "Port sheet");
            Mount.rightAngleWinch = CloneWinches(source, WinchRole.Right, "Starboard sheet");
            // Audited stay donors use paired sheets; centre controls are not pair candidates.
            Mount.midAngleWinch = Array.Empty<GPButtonRopeWinch>();
            Mount.reefWinch = CloneWinches(references.Aft, WinchRole.Reef, "Halyard");
            if (
                Mount.reefWinch.Length == 0
                || Mount.leftAngleWinch.Length == 0
                || Mount.rightAngleWinch.Length == 0
            )
            {
                var missing = new List<string>();
                if (Mount.reefWinch.Length == 0)
                    missing.Add("halyard");
                if (Mount.leftAngleWinch.Length == 0)
                    missing.Add("port sheet");
                if (Mount.rightAngleWinch.Length == 0)
                    missing.Add("starboard sheet");
                throw new InvalidOperationException(
                    $"Incomplete controls for Fisherman's Stay mount {definition.MountIndex} ({definition.Label}): missing {string.Join(", ", missing)}; sheet mapping={source.orderIndex}, halyard mapping={references.Aft.orderIndex}."
                );
            }
            Mount.midRopeAtt = CloneAnchors(source.midRopeAtt, "Sheet attachment");
            Mount.mastReefAtt = CloneAnchors(new[] { references.Guide }, "Halyard guide");
            Mount.mastReefAttExtension = CloneAnchors(
                new[] { references.Guide },
                "Halyard upper guide"
            );
            // Native sail fitting needs a walking frame, not stay collision surfaces.
            WalkObject = NewInactive(name: root.name + " Walk", parent: boat.walkCol);
            Mount.walkColMast = WalkObject.transform;
            visual = new FishermansStayVisual(
                owner: root.transform,
                ropeMaterial: source.GetComponent<MeshRenderer>().sharedMaterial,
                visualLayer: source.gameObject.layer,
                ownedMeshes: ownedMeshes
            );
            var original = source.GetComponent<BoatPartOption>();
            Option = root.AddComponent<BoatPartOption>();
            Option.optionName = DisplayName + " (" + definition.Label + ")";
            Option.basePrice = original.basePrice;
            Option.installCost = original.installCost;
            Option.mass = original.mass;
            // The donor contributes assets, not installation requirements.
            Option.requires = references.Required.ToList();
            Option.requiresDisabled = references.Forbidden.ToList();
            Option.childOptions = new GameObject[0];
            Option.childMast = Mount;
            Option.walkColObject = WalkObject;
            Place();
        }

        internal bool Available =>
            fits
            && references.Required.All(o => o && o.gameObject.activeInHierarchy)
            && references.Forbidden.All(o => o && !o.gameObject.activeInHierarchy)
            && references.Guide
            && references.Guide.gameObject.activeInHierarchy;

        internal void Refresh()
        {
            try
            {
                // A fitted sail pins the current support through removal previews.
                if (configurations.Length > 1 && !Mount.sails.Any(s => s))
                {
                    var fitted = configurations
                        .Where(r => r.Fitted(boatRoot: boat.transform))
                        .ToArray();
                    if (fitted.Length != 1)
                    {
                        fits = false;
                        Option.canInstall = false;
                        return;
                    }
                    var next = fitted[0];
                    if (next != references)
                    {
                        FishermanWinchControls.Configure(
                            boat: boat,
                            owner: Mount.gameObject,
                            forward: next.Fore,
                            halyard: next.Aft,
                            nativeMount: Mount
                        );
                        Option.requires = next.Required.ToList();
                        Option.requiresDisabled = next.Forbidden.ToList();
                        Mount.mastCols = next
                            .Required.Select(o => o.GetComponent<CapsuleCollider>())
                            .Where(c => c)
                            .ToArray();
                        for (int i = 0; i < anchors.Count; i++)
                            if (anchors[i].Item1 == references.Guide)
                                anchors[i] = Tuple.Create(next.Guide, anchors[i].Item2);
                        references = next;
                    }
                }
                Place();
                Option.canInstall = Available;
            }
            catch (Exception exception)
            {
                fits = false;
                Option.canInstall = false;
                if (!warned)
                    Plugin.Log.LogWarning(
                        $"Fisherman's Stay {Mount.orderIndex} unavailable: {exception.Message}"
                    );
                warned = true;
            }
        }

        private void Place()
        {
            var definition = references.Definition;
            var fore = references.Fore.transform.TransformPoint(definition.ForePoint);
            var aft = references.Aft.transform.TransformPoint(definition.AftPoint);
            float span = FishermansStayGeometry.Span(aft, fore);
            var forward = (aft - fore) / span;
            var rotation = Quaternion.LookRotation(
                forward,
                FishermansStayGeometry.FrameUp(up: boat.transform.up, forward: forward)
            );
            bool moved = Mount.transform.position != aft || Mount.transform.rotation != rotation;
            Mount.transform.SetPositionAndRotation(aft, rotation);
            Mount.transform.localScale = Vector3.one;
            Mount.mastHeight = span;
            // The sail checker's walking frame mirrors the boat model, whose import frame
            // can differ from the rigidbody root (including a 90-degree rotation).
            var toWalk = boat.boatModel.worldToLocalMatrix * Mount.transform.localToWorldMatrix;
            WalkObject.transform.localPosition = toWalk.MultiplyPoint3x4(Vector3.zero);
            WalkObject.transform.localRotation = toWalk.rotation;
            WalkObject.transform.localScale = toWalk.lossyScale;
            visual.Place(references: references, span: span);
            foreach (var pair in anchors)
                pair.Item2.SetPositionAndRotation(
                    pair.Item1 == references.Guide ? HalyardPoint : pair.Item1.position,
                    pair.Item1.rotation
                );
            if (moved && Mount.sails.Count > 0)
                Mount.UpdateControllerAttachments();
            fits = true;
        }

        internal void Destroy()
        {
            foreach (var control in controls)
                control.Dispose();
            controls.Clear();
            if (Mount)
            {
                Mount.gameObject.SetActive(false);
                Object.Destroy(Mount.gameObject);
            }
            if (WalkObject)
                Object.Destroy(WalkObject);
            foreach (var mesh in ownedMeshes)
                if (mesh)
                    Object.Destroy(mesh);
            ownedMeshes.Clear();
        }

        private GPButtonRopeWinch[] CloneWinches(Mast donor, WinchRole role, string label)
        {
            var control = FishermanWinchControls.Create(
                boat,
                Mount.gameObject,
                Mount.transform,
                donor,
                role,
                "FishermansStay " + label
            );
            controls.Add(control);
            return new[] { control.Winch };
        }

        private Transform[] CloneAnchors(Transform[] sources, string label)
        {
            if (sources == null || sources.Length == 0 || !sources[0])
                return new Transform[0];
            var clone = new GameObject("FishermansStay " + label).transform;
            clone.SetParent(Mount.transform, false);
            clone.SetPositionAndRotation(sources[0].position, sources[0].rotation);
            anchors.Add(Tuple.Create(sources[0], clone));
            return new[] { clone };
        }

        internal static GameObject NewInactive(string name, Transform parent)
        {
            var result = new GameObject(name);
            result.SetActive(false);
            result.transform.SetParent(parent, false);
            return result;
        }
    }
}
