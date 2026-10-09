using MoreSailwindSails.Visuals;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Owns the rotating sleeve, fixed lower mounting and upper-sprit purchase visuals.
    internal sealed class SpritsailSnotter : MonoBehaviour
    {
        public Material[] Materials;
        public RoutedRope Purchase;
        public SpritsailRopeCollar PurchaseCollar;
        public RopeEffect ReefSource;
        private SpritsailSnotterMesh rotatingMesh;
        private SpritsailSnotterMesh mountingMesh;
        private readonly SailVisualCache cache = new SailVisualCache();
        private Matrix4x4 rotatingFrame;
        private Matrix4x4 mountingFrame;
        private Transform mountAnchor;
        private bool wasPurchaseVisible;
        private float meshContactRadius = -1;
        private float meshPivotDistance = -1;
        private float meshMastRadius = -1;
        private Material ownedMetal;
        private Material ownedWood;

        internal static SpritsailSnotter Create(
            Transform parent,
            Material timber,
            PrefabsDirectory directory
        )
        {
            var root = new GameObject(name: "Spritsail snotter mast fitting");
            root.transform.SetParent(parent: parent, worldPositionStays: false);
            var fitting = root.AddComponent<SpritsailSnotter>();
            var woodIndex = SpritsailSnotterMaterials.WoodSailIndex;
            var metalIndex = SpritsailSnotterMaterials.MetalItemIndex;
            var darkWood = SpritsailSnotterMaterials.Wood(
                donor: directory.sails != null && directory.sails.Length > woodIndex
                    ? directory.sails[woodIndex]
                    : null,
                shader: timber.shader,
                owned: out fitting.ownedWood
            );
            var metal = SpritsailSnotterMaterials.Metal(
                donor: directory.directory != null && directory.directory.Length > metalIndex
                    ? directory.directory[metalIndex]
                    : null,
                shader: timber.shader,
                owned: out fitting.ownedMetal
            );
            if (fitting.ownedWood || fitting.ownedMetal)
                Plugin.Log.LogWarning(
                    data: "Snotter native finish donor unavailable; using an owned untextured fallback."
                );
            fitting.Materials = new[] { darkWood, metal };
            var connections = parent.GetComponent<SailConnections>();
            fitting.ReefSource = connections.reefController.GetComponent<RopeEffect>();
            var sheet = connections.angleControllerLeft
                ? connections.angleControllerLeft
                : connections.angleControllerMid;
            var source = sheet.GetComponent<RopeEffect>();
            fitting.Purchase = CreateLine(
                parent: root.transform,
                source: source,
                name: "Snotter purchase",
                count: 3
            );
            fitting.PurchaseCollar = SpritsailRopeCollar.Create(
                parent: root.transform,
                source: source,
                name: "Upper sprit halyard coil"
            );
            fitting.Hide();
            return fitting;
        }

        private static RoutedRope CreateLine(
            Transform parent,
            RopeEffect source,
            string name,
            int count
        )
        {
            var root = new GameObject(name: name);
            root.transform.SetParent(parent: parent, worldPositionStays: false);
            var line = root.AddComponent<LineRenderer>();
            var original = source.GetComponent<LineRenderer>();
            line.sharedMaterials = original.sharedMaterials;
            line.startColor = original.startColor;
            line.endColor = original.endColor;
            line.startWidth = line.endWidth =
                source.ropeWidth * SpritsailSpritGeometry.RopeThicknessMultiplier;
            line.textureMode = LineTextureMode.Tile;
            line.useWorldSpace = true;
            line.positionCount = count;
            return RoutedRope.Attach(line: line);
        }

        internal void Pose(
            CapsuleCollider mast,
            Vector3 heel,
            Vector3 tip,
            float sparRadius,
            Vector3 guide,
            Vector3 fallbackDirection,
            Vector3 mountingDirection,
            float mastRadius,
            bool visible,
            long revision
        )
        {
            if (!visible || !mast)
            {
                Hide();
                return;
            }
            bool purchaseVisible = ReefSource && ReefSource.gameObject.activeInHierarchy;
            if (purchaseVisible && !wasPurchaseVisible)
                cache.Invalidate();
            wasPurchaseVisible = purchaseVisible;
            bool rebuild = cache.Needs(revision: revision);
            SpritsailMountProfile.Consumer(part: SpritsailVisualPart.Snotter, rebuild: rebuild);
            if (rebuild)
            {
                var localAxis =
                    mast.direction == 0 ? Vector3.right
                    : mast.direction == 1 ? Vector3.up
                    : Vector3.forward;
                var axis = mast.transform.TransformDirection(direction: localAxis).normalized;
                if (Vector3.Dot(axis, tip - heel) < 0)
                    axis = -axis;
                var origin = mast.transform.TransformPoint(position: mast.center);
                var center = origin + axis * Vector3.Dot(heel - origin, axis);
                var forward = Vector3.ProjectOnPlane(vector: heel - center, planeNormal: axis);
                if (forward.sqrMagnitude < 0.000001f)
                    forward = Vector3.ProjectOnPlane(vector: fallbackDirection, planeNormal: axis);
                forward.Normalize();
                var fixedForward = Vector3
                    .ProjectOnPlane(vector: mountingDirection, planeNormal: axis)
                    .normalized;
                if (fixedForward.sqrMagnitude < 0.000001f)
                {
                    // Mast-local fallback is independent of the sail's current tack.
                    var reference = localAxis == Vector3.right ? Vector3.up : Vector3.right;
                    fixedForward = Vector3
                        .ProjectOnPlane(
                            vector: mast.transform.TransformDirection(direction: reference),
                            planeNormal: axis
                        )
                        .normalized;
                }
                float pivotDistance = (heel - center).magnitude;
                float contactRadius = SpritsailSpritGeometry.RadiusAtPivot(
                    radius: sparRadius,
                    pivotToTip: (tip - heel).magnitude
                );
                if (
                    rotatingMesh == null
                    || Mathf.Abs(pivotDistance - meshPivotDistance) > 0.001f
                    || Mathf.Abs(contactRadius - meshContactRadius) > 0.001f
                    || Mathf.Abs(mastRadius - meshMastRadius) > 0.001f
                )
                    Refit(
                        contactRadius: contactRadius,
                        mastRadius: mastRadius,
                        pivotDistance: pivotDistance
                    );
                mountAnchor = mast.transform;
                rotatingFrame =
                    transform.parent.worldToLocalMatrix
                    * SpritsailSnotterGeometry.Frame(center: center, axis: axis, outward: forward);
                mountingFrame =
                    mountAnchor.worldToLocalMatrix
                    * SpritsailSnotterGeometry.Frame(
                        center: center,
                        axis: axis,
                        outward: fixedForward
                    );
                var purchasePoint = SpritsailDeployment.PurchasePoint(heel: heel, tip: tip);
                var contact = PurchaseCollar.Pose(
                    center: purchasePoint,
                    axis: tip - heel,
                    outward: guide - purchasePoint,
                    radius: sparRadius
                        * Mathf.Lerp(a: 1, b: SpritsailSpritGeometry.EndRadiusRatio, t: 0.8f)
                );
                if (ReefSource)
                {
                    Purchase.SetPosition(index: 0, position: ReefSource.transform.position);
                    Purchase.SetPosition(index: 1, position: guide);
                    Purchase.SetPosition(index: 2, position: contact);
                }
                cache.Commit(revision: revision);
            }
            rotatingMesh.Draw(
                frame: transform.parent.localToWorldMatrix * rotatingFrame,
                materials: Materials,
                layer: gameObject.layer
            );
            mountingMesh.Draw(
                frame: mountAnchor.localToWorldMatrix * mountingFrame,
                materials: Materials,
                layer: gameObject.layer
            );
            PurchaseCollar.Rope.DrawCached(
                visible: true,
                anchor: transform.parent,
                revision: revision
            );
            Purchase.DrawCached(
                visible: purchaseVisible,
                anchor: transform.parent,
                revision: revision
            );
        }

        private void Refit(float contactRadius, float mastRadius, float pivotDistance)
        {
            SpritsailSnotterGeometry.Fit(
                sparRadius: contactRadius,
                mastRadius: mastRadius,
                pivotDistance: pivotDistance,
                vertices: out var profile,
                normals: out var profileNormals
            );
            if (rotatingMesh == null)
                rotatingMesh = new SpritsailSnotterMesh(
                    mounting: false,
                    profile: profile,
                    profileNormals: profileNormals
                );
            else
                rotatingMesh.Refit(profile: profile, profileNormals: profileNormals);
            if (mountingMesh == null)
                mountingMesh = new SpritsailSnotterMesh(
                    mounting: true,
                    profile: profile,
                    profileNormals: profileNormals
                );
            else
                mountingMesh.Refit(profile: profile, profileNormals: profileNormals);
            meshContactRadius = contactRadius;
            meshPivotDistance = pivotDistance;
            meshMastRadius = mastRadius;
        }

        internal void Hide()
        {
            if (Purchase)
                Purchase.SetVisible(visible: false);
            if (PurchaseCollar)
                PurchaseCollar.Hide();
        }

        private void OnDisable()
        {
            cache.Invalidate();
            Hide();
        }

        private void OnDestroy()
        {
            rotatingMesh?.Dispose();
            mountingMesh?.Dispose();
            if (ownedWood)
                Destroy(obj: ownedWood);
            if (ownedMetal)
                Destroy(obj: ownedMetal);
        }
    }
}
