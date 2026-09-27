using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Stays.FishermansStay
{
    internal sealed class FishermansStay
    {
        internal const string DisplayName = "Fisherman's Stay";
        internal Mast Mount;
        internal BoatPartOption Option;
        internal GameObject WalkObject;
        private readonly BoatRefs boat;
        private readonly FishermansStayReferences references;
        internal FishermansStayReferences References => references;
        private Mast source => references.Donor;
        private Transform visual,
            walkVisual;
        private float visualMin,
            visualMax,
            walkMin,
            walkMax;
        private bool warned;
        private bool fits;
        private readonly List<Tuple<Transform, Transform>> anchors =
            new List<Tuple<Transform, Transform>>();
        private readonly List<FishermanWinchControls.OwnedWinch> controls =
            new List<FishermanWinchControls.OwnedWinch>();

        internal FishermansStay(BoatRefs boat, FishermansStayReferences references)
        {
            this.boat = boat;
            this.references = references;
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
            Mount.leftAngleWinch = CloneWinches(source, WinchRole.Left, "Port sheet");
            Mount.rightAngleWinch = CloneWinches(source, WinchRole.Right, "Starboard sheet");
            // Audited stay donors use paired sheets; centre controls are not pair candidates.
            Mount.midAngleWinch = Array.Empty<GPButtonRopeWinch>();
            Mount.reefWinch = CloneWinches(references.Aft, WinchRole.Reef, "Halyard");
            FishermanWinchControls.Configure(
                boat,
                Mount.gameObject,
                references.Fore,
                references.Aft,
                Mount
            );
            if (
                Mount.reefWinch.Length == 0
                || (
                    Mount.midAngleWinch.Length == 0
                    && (Mount.leftAngleWinch.Length == 0 || Mount.rightAngleWinch.Length == 0)
                )
            )
            {
                var missing = new List<string>();
                if (Mount.reefWinch.Length == 0)
                    missing.Add("halyard");
                if (Mount.midAngleWinch.Length == 0)
                {
                    if (Mount.leftAngleWinch.Length == 0)
                        missing.Add("port sheet");
                    if (Mount.rightAngleWinch.Length == 0)
                        missing.Add("starboard sheet");
                }
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
            visual = CopyGeometry(source.transform, root.transform, true);
            GeometryRange(visual, out visualMin, out visualMax);
            WalkObject = NewInactive(root.name + " Walk", boat.walkCol);
            walkVisual = CopyGeometry(source.walkColMast, WalkObject.transform);
            GeometryRange(walkVisual, out walkMin, out walkMax);
            Mount.walkColMast = WalkObject.transform;
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
                FishermansStayGeometry.FrameUp(source.transform.up, forward)
            );
            bool moved = Mount.transform.position != aft || Mount.transform.rotation != rotation;
            Mount.transform.SetPositionAndRotation(aft, rotation);
            Mount.transform.localScale = Vector3.one;
            Mount.mastHeight = span;
            FitGeometry(visual, visualMin, visualMax, span);
            // Convert through the donor's matching visual/walking frames; the
            // walking boat has its own origin and rotation.
            WalkObject.transform.SetPositionAndRotation(
                source.walkColMast.TransformPoint(source.transform.InverseTransformPoint(aft)),
                source.walkColMast.rotation
                    * Quaternion.Inverse(source.transform.rotation)
                    * rotation
            );
            WalkObject.transform.localScale = Vector3.one;
            FitGeometry(walkVisual, walkMin, walkMax, span);
            foreach (var pair in anchors)
                pair.Item2.SetPositionAndRotation(pair.Item1.position, pair.Item1.rotation);
            if (moved && Mount.sails.Count > 0)
                Mount.UpdateControllerAttachments();
            fits = true;
        }

        private static void FitGeometry(Transform geometry, float min, float max, float span)
        {
            FishermansStayGeometry.FitAxis(min, max, span, out float scale, out float offset);
            geometry.localScale = new Vector3(1, 1, scale);
            geometry.localPosition = new Vector3(0, 0, offset);
        }

        private static void GeometryRange(Transform root, out float min, out float max)
        {
            min = float.PositiveInfinity;
            max = float.NegativeInfinity;
            var meshes = root.GetComponentsInChildren<MeshFilter>(true)
                .Select(f => Tuple.Create(f.transform, f.sharedMesh))
                .Concat(
                    root.GetComponentsInChildren<MeshCollider>(true)
                        .Select(c => Tuple.Create(c.transform, c.sharedMesh))
                );
            foreach (var item in meshes)
            {
                if (!item.Item2)
                    continue;
                var bounds = item.Item2.bounds;
                // The walking representation may contain only MeshColliders.
                // Evaluate both representations in their own unscaled frames.
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    var corner =
                        bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                    float along = root.InverseTransformPoint(item.Item1.TransformPoint(corner)).z;
                    min = Math.Min(min, along);
                    max = Math.Max(max, along);
                }
            }
            if (float.IsInfinity(min) || float.IsInfinity(max) || max - min < 0.25f)
                throw new InvalidOperationException(
                    "The donor has no usable static stay geometry."
                );
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
        }

        private GPButtonRopeWinch[] CloneWinches(Mast donor, WinchRole role, string label)
        {
            var sourceWinch = FishermanWinchControls.Source(boat, donor, role);
            if (!sourceWinch)
                return new GPButtonRopeWinch[0];
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

        // Copy static geometry only. Instantiating a fitted Mast would also copy
        // live sails, script state, and references to the original winches.
        private static Transform CopyGeometry(
            Transform source,
            Transform parent,
            bool mastVisual = false
        )
        {
            var root = new GameObject("FishermansStay geometry").transform;
            root.SetParent(parent, false);
            var nodes = new Dictionary<Transform, Transform> { [source] = root };
            foreach (var old in source.GetComponentsInChildren<Transform>(true))
            {
                if (
                    old != source
                    && (
                        old.GetComponentInParent<Sail>()
                        || old.GetComponentInParent<GPButtonRopeWinch>()
                    )
                )
                    continue;
                if (!nodes.TryGetValue(old, out var node))
                {
                    if (!nodes.TryGetValue(old.parent, out var parentNode))
                        continue;
                    node = new GameObject(old.name).transform;
                    node.SetParent(parentNode, false);
                    node.localPosition = old.localPosition;
                    node.localRotation = old.localRotation;
                    node.localScale = old.localScale;
                    node.gameObject.SetActive(old.gameObject.activeSelf);
                    nodes.Add(old, node);
                }
                node.gameObject.layer = old.gameObject.layer;
                var filter = old.GetComponent<MeshFilter>();
                var renderer = old.GetComponent<MeshRenderer>();
                if (filter && renderer)
                {
                    node.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    var copy = node.gameObject.AddComponent<MeshRenderer>();
                    copy.sharedMaterials = renderer.sharedMaterials;
                    copy.shadowCastingMode = renderer.shadowCastingMode;
                    copy.receiveShadows = renderer.receiveShadows;
                }
                foreach (var collider in old.GetComponents<Collider>())
                {
                    Collider copy;
                    if (collider is CapsuleCollider capsule)
                    {
                        var c = node.gameObject.AddComponent<CapsuleCollider>();
                        c.center = capsule.center;
                        c.radius = capsule.radius;
                        c.height = capsule.height;
                        c.direction = capsule.direction;
                        copy = c;
                    }
                    else if (collider is BoxCollider box)
                    {
                        var c = node.gameObject.AddComponent<BoxCollider>();
                        c.center = box.center;
                        c.size = box.size;
                        copy = c;
                    }
                    else if (collider is MeshCollider mesh)
                    {
                        var c = node.gameObject.AddComponent<MeshCollider>();
                        c.sharedMesh = mesh.sharedMesh;
                        c.convex = mesh.convex;
                        copy = c;
                    }
                    else if (collider is SphereCollider sphere)
                    {
                        var c = node.gameObject.AddComponent<SphereCollider>();
                        c.center = sphere.center;
                        c.radius = sphere.radius;
                        copy = c;
                    }
                    else
                        throw new InvalidOperationException(
                            $"Unsupported stay collider: {collider.GetType().Name}."
                        );
                    copy.sharedMaterial = collider.sharedMaterial;
                    // Mast.Awake normally makes the source's root collider a
                    // trigger. Our visual is a child of the new mount instead.
                    copy.isTrigger = collider.isTrigger || (mastVisual && old == source);
                    copy.enabled = collider.enabled;
                }
            }
            return root;
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
