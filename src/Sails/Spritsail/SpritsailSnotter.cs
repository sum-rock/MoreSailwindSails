using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Owns the fixed mast pocket and upper-sprit purchase visuals without altering controls.
    internal sealed class SpritsailSnotter : MonoBehaviour
    {
        public MeshRenderer Timber;
        public LineRenderer Purchase;
        public SpritsailRopeCollar PurchaseCollar;
        public RopeEffect ReefSource;
        private Mesh ownedMesh;
        private Vector3[] profile;
        private Vector3[] posed;
        private float meshBackDepth = -1;
        private float meshMastRadius = -1;
        private Material ownedMetal;

        internal static SpritsailSnotter Create(Transform parent, Material timber)
        {
            var root = new GameObject(name: "Spritsail snotter mast fitting");
            root.transform.SetParent(parent: parent, worldPositionStays: false);
            var fitting = root.AddComponent<SpritsailSnotter>();
            root.AddComponent<MeshFilter>();
            fitting.Timber = root.AddComponent<MeshRenderer>();
            var metal = Resources
                .FindObjectsOfTypeAll<Material>()
                .FirstOrDefault(material => material.name == "mast_metal");
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
            fitting.Timber.sharedMaterials = new[] { metal, metal };
            var connections = parent.GetComponent<SailConnections>();
            fitting.ReefSource = connections.reefController.GetComponent<RopeEffect>();
            var source = connections.angleControllerLeft.GetComponent<RopeEffect>();
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
            var right = Vector3.Cross(axis, forward).normalized;
            float backDepth = Mathf.Max(
                a: 1.1f,
                b: ((heel - center).magnitude - mastRadius) / sparRadius
            );
            float scaledMastRadius = mastRadius / sparRadius;
            if (
                !ownedMesh
                || Mathf.Abs(backDepth - meshBackDepth) > 0.001f
                || Mathf.Abs(scaledMastRadius - meshMastRadius) > 0.001f
            )
            {
                SpritsailSnotterGeometry.Create(
                    backDepth: backDepth,
                    mastRadius: scaledMastRadius,
                    vertices: out profile,
                    uv: out var uv,
                    wood: out var wood,
                    iron: out var iron
                );
                posed = new Vector3[profile.Length];
                if (!ownedMesh)
                {
                    ownedMesh = new Mesh { name = "Spritsail fixed heel pocket" };
                    ownedMesh.MarkDynamic();
                    GetComponent<MeshFilter>().sharedMesh = ownedMesh;
                }
                ownedMesh.vertices = profile;
                ownedMesh.uv = uv;
                ownedMesh.subMeshCount = 2;
                ownedMesh.SetTriangles(triangles: wood, submesh: 0);
                ownedMesh.SetTriangles(triangles: iron, submesh: 1);
                meshBackDepth = backDepth;
                meshMastRadius = scaledMastRadius;
            }
            Vector3 World(Vector3 point) =>
                heel + (right * point.x + axis * point.y + forward * point.z) * sparRadius;
            for (int i = 0; i < profile.Length; i++)
                posed[i] = transform.InverseTransformPoint(position: World(point: profile[i]));
            ownedMesh.vertices = posed;
            ownedMesh.RecalculateNormals();
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
