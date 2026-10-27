// Keeps the neutral fitting prototype attached to any native or modded mast without sail controls.
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    [DefaultExecutionOrder(100)]
    internal sealed class LooseFootedSpritsailRig : MonoBehaviour
    {
        [SerializeField]
        internal Sail Sail;

        [SerializeField]
        internal Transform ScaleRoot;

        [SerializeField]
        internal Transform Frame;

        [SerializeField]
        internal Transform ClothBone;

        [SerializeField]
        internal Transform Sprit;

        [SerializeField]
        internal Transform SnotterAssembly;

        [SerializeField]
        internal Transform RotationPlate;

        [SerializeField]
        internal Transform ReefLineAttachment;

        [SerializeField]
        internal Transform SheetLineAttachment;

        [SerializeField]
        internal MeshRenderer ColorReference;

        [SerializeField]
        internal Material TemplateMaterial;

        [SerializeField]
        internal BoxCollider[] CollisionBoxes;

        [SerializeField]
        internal float BaseScale;

        [SerializeField]
        internal float BaseLuff;

        [SerializeField]
        internal float BaseArea;
        internal LooseFootedSpritsailAssetData Data;
        private Rigidbody body;
        private Transform lastParent;
        private Mast mast;
        private Collider mastCollider;
        private MeshCollider mastSurface;
        private Vector3[] mastVertices;
        private int[] mastTriangles;
        private SkinnedMeshRenderer clothRenderer;
        private ShipyardSailColChecker checker;
        private bool dirty = true;
        private bool warnedRadius;
        private float lastHeight;
        private float lastWidth;
        private float lastRadius;
        private float lastInstallHeight;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            checker = Sail.GetComponent<SailConnections>().colChecker;
            clothRenderer = Sail.cloth.GetComponent<SkinnedMeshRenderer>();
            Data = LooseFootedSpritsailPrefab.CreateAsset(prefabIndex: Sail.prefabIndex);
        }

        private void OnEnable()
        {
            dirty = true;
            Refresh();
        }

        private void FixedUpdate() => Refresh();

        private void LateUpdate() => Refresh();

        internal void Refresh()
        {
            if (!Sail || !Frame || !ScaleRoot || !isActiveAndEnabled)
                return;
            if (transform.parent != lastParent)
            {
                lastParent = transform.parent;
                mast = lastParent ? lastParent.GetComponent<Mast>() : null;
                var mesh = mast ? mast.GetComponent<MeshFilter>()?.sharedMesh : null;
                mastVertices = mesh && mesh.isReadable ? mesh.vertices : null;
                mastTriangles = mesh && mesh.isReadable ? mesh.triangles : null;
                mastSurface =
                    mast && mast.walkColMast ? mast.walkColMast.GetComponent<MeshCollider>() : null;
                // The walking copy must use the same mesh-local coordinates as the visual mast.
                if (mastSurface && mastSurface.sharedMesh != mesh)
                    mastSurface = null;
                mastCollider = mast ? mast.GetComponent<Collider>() : null;
                if (mast && !mastCollider && mast.mastCols != null)
                    foreach (var capsule in mast.mastCols)
                        if (capsule)
                        {
                            mastCollider = capsule;
                            break;
                        }
                dirty = true;
                warnedRadius = false;
            }
            if (!mast || Data == null)
                return;
            // This milestone is deliberately a neutral, non-propulsive fitting. Native
            // attach/load can temporarily make the body dynamic before toggling OnEnable.
            body.isKinematic = true;
            transform.localRotation = Quaternion.identity;
            float installHeight = Sail.GetCurrentInstallHeight();
            if (installHeight != lastInstallHeight)
                dirty = true;
            lastInstallHeight = installHeight;
            transform.localPosition = new Vector3(x: 0, y: 0, z: installHeight - mast.mastHeight);
            MeasureMast(out var center, out var axis, out float radius);
            var boat = mast.shipRigidbody ? mast.shipRigidbody.transform : mast.transform.parent;
            var aft = boat
                ? transform.InverseTransformDirection(direction: -boat.forward)
                : Vector3.up;
            aft = Vector3.ProjectOnPlane(vector: aft, planeNormal: axis);
            if (aft.sqrMagnitude < 1e-6f)
                aft = Vector3.ProjectOnPlane(vector: Vector3.up, planeNormal: axis);
            if (aft.sqrMagnitude < 1e-6f)
                aft = Vector3.ProjectOnPlane(vector: Vector3.right, planeNormal: axis);
            var rotation = Quaternion.LookRotation(forward: -aft.normalized, upwards: axis);
            float height = LooseFootedSpritsailFittingGeometry.ClampScale(
                value: ScaleRoot.localScale.y
            );
            float width = LooseFootedSpritsailFittingGeometry.ClampScale(
                value: ScaleRoot.localScale.z
            );
            if (
                (Frame.localPosition - center).sqrMagnitude > 1e-10f
                || Quaternion.Angle(a: Frame.localRotation, b: rotation) > 0.01f
            )
                dirty = true;
            Frame.localPosition = center;
            Frame.localRotation = rotation;
            // Cancel the serialized size-reference transform at the renderer. The skin
            // bone poses are computed separately, so hardware never inherits cloth stretching.
            var cloth = Sail.cloth.transform;
            cloth.localPosition = Vector3.zero;
            cloth.localRotation = Quaternion.identity;
            var referenceScale = ScaleRoot.localScale;
            cloth.localScale = new Vector3(
                x: 1 / referenceScale.x,
                y: 1 / referenceScale.y,
                z: 1 / referenceScale.z
            );
            Sail.cloth.enabled = false;
            if (
                !dirty
                && height == lastHeight
                && width == lastWidth
                && Mathf.Abs(f: radius - lastRadius) < 0.00001f
            )
                return;
            lastRadius = radius;
            if (mastVertices != null || mastSurface)
            {
                float collarHeight =
                    (
                        Data.SnotterAssembly.RestTransform.m13
                        - LooseFootedSpritsailFittingGeometry
                            .Corner(data: Data, index: Data.Throat)
                            .y
                    )
                    * BaseScale
                    * height;
                var collarCenter = center + axis * collarHeight;
                float visibleRadius =
                    mastVertices != null
                        ? LooseFootedSpritsailFittingGeometry.MastRadius(
                            vertices: mastVertices,
                            triangles: mastTriangles,
                            matrix: transform.worldToLocalMatrix
                                * mast.transform.localToWorldMatrix,
                            center: collarCenter,
                            axis: axis
                        )
                        : MeasureWalkingSurface(center: collarCenter, radius: radius);
                if (
                    LooseFootedSpritsailFittingGeometry.Finite(value: visibleRadius)
                    && visibleRadius > 0
                )
                    radius = visibleRadius;
            }
            var pose = LooseFootedSpritsailFittingGeometry.Pose(
                data: Data,
                baseScale: BaseScale,
                height: height,
                width: width,
                mastRadius: radius
            );
            Apply(node: ClothBone, pose: pose.Sail);
            Apply(node: Sprit, pose: pose.Sprit);
            Apply(node: SnotterAssembly, pose: pose.SnotterAssembly);
            Apply(node: RotationPlate, pose: pose.RotationPlate);
            RefreshCollision(pose: pose);
            var bounds = new Bounds(center: Vector3.zero, size: Vector3.zero);
            foreach (int index in new[] { Data.Tack, Data.Throat, Data.Peak, Data.Clew })
                bounds.Encapsulate(
                    point: cloth.InverseTransformPoint(
                        position: ClothBone.TransformPoint(position: Data.Sail.Vertices[index])
                    )
                );
            bounds.Expand(amount: 0.1f);
            clothRenderer.localBounds = bounds;
            Sail.sailArea = BaseArea * height * width;
            lastHeight = height;
            lastWidth = width;
            dirty = false;
        }

        // MeshCollider raycasts work even when an imported mesh has Read/Write disabled.
        private float MeasureWalkingSurface(Vector3 center, float radius)
        {
            var toWalk =
                mastSurface.transform.localToWorldMatrix
                * mast.transform.worldToLocalMatrix
                * transform.localToWorldMatrix;
            var fromWalk = toWalk.inverse;
            float measured = 0;
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI / 16;
                var direction =
                    Frame.localRotation
                    * new Vector3(x: Mathf.Cos(f: angle), y: 0, z: Mathf.Sin(f: angle));
                var ray = Utils.WinchCaptureGeometry.TransformRay(
                    transform: toWalk,
                    ray: new Ray(origin: center + direction * radius * 2, direction: -direction),
                    out float distanceScale
                );
                if (
                    !mastSurface.Raycast(
                        ray: ray,
                        hitInfo: out var hit,
                        maxDistance: radius * 2 * distanceScale
                    )
                )
                    return 0;
                measured = Mathf.Max(
                    a: measured,
                    b: (fromWalk.MultiplyPoint3x4(point: hit.point) - center).magnitude
                );
            }
            return measured;
        }

        private void MeasureMast(out Vector3 center, out Vector3 axis, out float radius)
        {
            axis = Vector3.forward;
            center = Vector3.zero;
            radius = 0;
            if (mastCollider)
            {
                var matrix =
                    transform.worldToLocalMatrix * mastCollider.transform.localToWorldMatrix;
                var capsule = mastCollider as CapsuleCollider;
                var box = mastCollider as BoxCollider;
                if (capsule)
                {
                    var localAxis =
                        capsule.direction == 0 ? Vector3.right
                        : capsule.direction == 1 ? Vector3.up
                        : Vector3.forward;
                    axis = matrix.MultiplyVector(vector: localAxis).normalized;
                    var a = capsule.direction == 0 ? Vector3.up : Vector3.right;
                    var b = capsule.direction == 2 ? Vector3.up : Vector3.forward;
                    radius =
                        capsule.radius
                        * Mathf.Max(
                            a: matrix.MultiplyVector(vector: a).magnitude,
                            b: matrix.MultiplyVector(vector: b).magnitude
                        );
                    center = matrix.MultiplyPoint3x4(point: capsule.center);
                }
                else if (box)
                {
                    // Native mast transforms use local Z as their installation axis.
                    center = matrix.MultiplyPoint3x4(point: box.center);
                    radius =
                        Mathf.Max(
                            a: matrix.MultiplyVector(vector: Vector3.right * box.size.x).magnitude,
                            b: matrix.MultiplyVector(vector: Vector3.up * box.size.y).magnitude
                        ) / 2;
                }
            }
            if (!LooseFootedSpritsailFittingGeometry.Finite(value: radius) || radius <= 0)
            {
                radius = LooseFootedSpritsailFittingGeometry.CollarRadius(data: Data) * BaseScale;
                if (!warnedRadius)
                {
                    Plugin.Log.LogWarning(
                        $"{Sail.sailName}: no usable mast collider on {mast.name}; using the authored collar radius."
                    );
                    warnedRadius = true;
                }
            }
            if (Vector3.Dot(lhs: axis, rhs: Vector3.forward) < 0)
                axis = -axis;
            // Closest point of the collider centerline to the native installation origin.
            center -= axis * Vector3.Dot(lhs: center, rhs: axis);
        }

        private static void Apply(Transform node, Matrix4x4 pose)
        {
            node.localPosition = pose.MultiplyPoint3x4(point: Vector3.zero);
            node.localRotation = pose.rotation;
            node.localScale = pose.lossyScale;
        }

        private void RefreshCollision(LooseFootedSpritsailPose pose)
        {
            for (int i = 0; i < LooseFootedSpritsailFittingGeometry.CollisionStrips; i++)
            {
                var box = CollisionBoxes[i];
                box.enabled = LooseFootedSpritsailFittingGeometry.CollisionStrip(
                    data: Data,
                    pose: pose,
                    column: i,
                    out var center,
                    out var size
                );
                box.transform.localPosition = Frame.localPosition + Frame.localRotation * center;
                box.transform.localRotation = Frame.localRotation;
                box.center = Vector3.zero;
                box.size = size;
            }
            var spar = CollisionBoxes[CollisionBoxes.Length - 1];
            var centerSpar = (Data.Sprit.BoundsMin + Data.Sprit.BoundsMax) / 2;
            spar.transform.localPosition =
                Frame.localPosition
                + Frame.localRotation * pose.Sprit.MultiplyPoint3x4(point: centerSpar);
            spar.transform.localRotation = Frame.localRotation * pose.Sprit.rotation;
            spar.size = Vector3.Scale(
                a: Data.Sprit.BoundsMax - Data.Sprit.BoundsMin,
                b: pose.Sprit.lossyScale
            );
            spar.center = Vector3.zero;
        }

        internal void ChangeScale(float height, float width)
        {
            var old = ScaleRoot.localScale;
            float y = LooseFootedSpritsailFittingGeometry.ClampScale(value: old.y + height);
            float z = LooseFootedSpritsailFittingGeometry.ClampScale(value: old.z + width);
            ScaleRoot.localScale = new Vector3(x: y, y: y, z: z);
            dirty = true;
            Refresh();
            Sail.SetSailArea();
            if (GameState.currentShipyard && ShipyardUI.instance)
                ShipyardUI.instance.UpdateDescriptionText();
        }

        internal void SyncColor()
        {
            var renderer = Sail.cloth.GetComponent<SkinnedMeshRenderer>();
            var material = renderer.sharedMaterial;
            material.mainTexture = Texture2D.whiteTexture;
            var previous = ColorReference.sharedMaterial;
            if (previous && previous != material && previous != TemplateMaterial)
                Destroy(obj: previous);
            ColorReference.sharedMaterial = material;
        }

        internal void PositionCollisionChecker(ShipyardSailColChecker target, float angle)
        {
            Refresh();
            if (!mast)
                return;
            var root = target.transform;
            var walk = mast.walkColMast;
            var axis = Frame.localRotation * Vector3.up;
            if (walk && root.parent == walk)
            {
                root.position = walk.TransformPoint(position: transform.localPosition);
                root.rotation = walk.rotation * Quaternion.AngleAxis(angle: angle, axis: axis);
            }
            else
            {
                root.position = transform.position;
                root.rotation = transform.rotation * Quaternion.AngleAxis(angle: angle, axis: axis);
            }
            root.localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            if (checker && !checker.transform.IsChildOf(parent: transform))
                Destroy(obj: checker.gameObject);
            if (
                clothRenderer
                && clothRenderer.sharedMaterial
                && clothRenderer.sharedMaterial != TemplateMaterial
            )
                Destroy(obj: clothRenderer.sharedMaterial);
        }
    }
}
