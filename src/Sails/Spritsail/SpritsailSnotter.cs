using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Owns the rotating sleeve, fixed lower mounting and upper-sprit purchase visuals.
    internal sealed class SpritsailSnotter : MonoBehaviour
    {
        public MeshRenderer Timber;
        public LineRenderer Purchase;
        public SpritsailRopeCollar PurchaseCollar;
        public RopeEffect ReefSource;
        private Mesh ownedMesh;
        private Vector3[] profile;
        private Vector3[] posed;
        private Vector3[] profileNormals;
        private Vector3[] posedNormals;
        private float meshContactRadius = -1;
        private float meshPivotDistance = -1;
        private float meshMastRadius = -1;
        private Material ownedMetal;

        internal static SpritsailSnotter Create(Transform parent, Material timber)
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>();
            var root = new GameObject(name: "Spritsail snotter mast fitting");
            root.transform.SetParent(parent: parent, worldPositionStays: false);
            var fitting = root.AddComponent<SpritsailSnotter>();
            root.AddComponent<MeshFilter>();
            fitting.Timber = root.AddComponent<MeshRenderer>();
            var metal = materials.FirstOrDefault(material => material.name == "mast_metal");
            if (!metal)
            {
                metal = new Material(timber)
                {
                    name = "Spritsail dark iron",
                    color = new Color(0.04f, 0.04f, 0.04f, 1),
                };
                fitting.ownedMetal = metal;
                if (metal.HasProperty(name: "_Metallic"))
                    metal.SetFloat(name: "_Metallic", value: 0.7f);
                if (metal.HasProperty(name: "_Glossiness"))
                    metal.SetFloat(name: "_Glossiness", value: 0.25f);
            }
            // Match the authored material slots: native spar timber, then mast metal.
            fitting.Timber.sharedMaterials = new[] { timber, metal };
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

        private static LineRenderer CreateLine(
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
            return line;
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
            bool visible
        )
        {
            if (!visible || !mast)
            {
                Hide();
                return;
            }
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
                !ownedMesh
                || Mathf.Abs(pivotDistance - meshPivotDistance) > 0.001f
                || Mathf.Abs(contactRadius - meshContactRadius) > 0.001f
                || Mathf.Abs(mastRadius - meshMastRadius) > 0.001f
            )
            {
                SpritsailSnotterGeometry.Create(
                    sparRadius: contactRadius,
                    mastRadius: mastRadius,
                    pivotDistance: pivotDistance,
                    vertices: out profile,
                    normals: out profileNormals,
                    uv: out var uv,
                    triangles: out _
                );
                posed = new Vector3[profile.Length];
                posedNormals = new Vector3[profile.Length];
                if (!ownedMesh)
                {
                    ownedMesh = new Mesh { name = "Spritsail authored sleeve, bolt and mounting" };
                    ownedMesh.MarkDynamic();
                    GetComponent<MeshFilter>().sharedMesh = ownedMesh;
                }
                ownedMesh.vertices = profile;
                ownedMesh.uv = uv;
                ownedMesh.subMeshCount = SpritsailSnotterGeometry.MaterialCount;
                for (
                    int material = 0;
                    material < SpritsailSnotterGeometry.MaterialCount;
                    material++
                )
                    ownedMesh.SetTriangles(
                        triangles: SpritsailSnotterGeometry.MaterialTriangles(material: material),
                        submesh: material
                    );
                meshContactRadius = contactRadius;
                meshPivotDistance = pivotDistance;
                meshMastRadius = mastRadius;
            }
            var normalToLocal = transform.localToWorldMatrix.transpose;
            for (int i = 0; i < profile.Length; i++)
            {
                posed[i] = transform.InverseTransformPoint(
                    position: SpritsailSnotterGeometry.PosePoint(
                        vertex: i,
                        point: profile[i],
                        center: center,
                        axis: axis,
                        rotatingDirection: forward,
                        fixedDirection: fixedForward
                    )
                );
                // Normals follow the same rotating/fixed frame as their points,
                // then use the inverse-transpose of the world-to-local point map.
                posedNormals[i] = normalToLocal
                    .MultiplyVector(
                        vector: SpritsailSnotterGeometry.PosePoint(
                            vertex: i,
                            point: profileNormals[i],
                            center: Vector3.zero,
                            axis: axis,
                            rotatingDirection: forward,
                            fixedDirection: fixedForward
                        )
                    )
                    .normalized;
            }
            ownedMesh.vertices = posed;
            ownedMesh.normals = posedNormals;
            ownedMesh.RecalculateTangents();
            ownedMesh.RecalculateBounds();
            Timber.enabled = true;
            var purchasePoint = SpritsailDeployment.PurchasePoint(heel: heel, tip: tip);
            var contact = PurchaseCollar.Pose(
                center: purchasePoint,
                axis: tip - heel,
                outward: guide - purchasePoint,
                radius: sparRadius
                    * Mathf.Lerp(a: 1, b: SpritsailSpritGeometry.EndRadiusRatio, t: 0.8f)
            );
            Purchase.enabled = ReefSource && ReefSource.gameObject.activeInHierarchy;
            if (Purchase.enabled)
            {
                Purchase.SetPosition(index: 0, position: ReefSource.transform.position);
                Purchase.SetPosition(index: 1, position: guide);
                Purchase.SetPosition(index: 2, position: contact);
            }
        }

        internal void Hide()
        {
            if (Timber)
                Timber.enabled = false;
            if (Purchase)
                Purchase.enabled = false;
            if (PurchaseCollar)
                PurchaseCollar.Hide();
        }

        private void OnDisable() => Hide();

        private void OnDestroy()
        {
            if (ownedMesh)
                Destroy(obj: ownedMesh);
            if (ownedMetal)
                Destroy(obj: ownedMetal);
        }
    }
}
