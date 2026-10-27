// Fits the sail in a neutral mast frame while allowing the native hinge to drive sailing motion.
using MoreSailwindSails.Controls;
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
        private HingeJoint hinge;
        private LooseFootedSpritsailRigging rigging;
        private Transform lastParent;
        private Mast mast;
        private Rigidbody mastBody;
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
        private Vector3 neutralPosition;
        private Matrix4x4 neutralWorld;
        private Matrix4x4 neutralPlate;
        private bool fitted;
        private int fittedFrame;

        internal Mast Mast => mast;
        internal BoatRefs Boat { get; private set; }
        internal float? FittedSheetSpan { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            hinge = GetComponent<HingeJoint>();
            rigging = GetComponent<LooseFootedSpritsailRigging>();
            checker = Sail.GetComponent<SailConnections>().colChecker;
            clothRenderer = Sail.cloth.GetComponent<SkinnedMeshRenderer>();
            Data = LooseFootedSpritsailPrefab.CreateAsset(prefabIndex: Sail.prefabIndex);
        }

        private void OnEnable()
        {
            dirty = true;
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh();
            UpdatePlate();
        }

        internal bool PrepareSailing()
        {
            Refresh();
            rigging.Refresh();
            Sail.currentUnroll = 1;
            bool ready =
                isActiveAndEnabled
                && fitted
                && Time.frameCount > fittedFrame
                && mast
                && mast.gameObject.activeInHierarchy
                && Sail.IsInstalled()
                && Sail.shipRigidbody
                && Sail.shipRigidbody == mast.shipRigidbody
                && GameState.playing
                && !GameState.currentShipyard
                && !GameState.shipyardUpdatingOrder
                && !checker.checking
                && rigging.SheetsReady
                && LooseFootedSpritsailFittingGeometry.Finite(value: Sail.minAngle)
                && LooseFootedSpritsailFittingGeometry.Finite(value: Sail.maxAngle)
                && Sail.minAngle < Sail.maxAngle
                && RefreshAerodynamics();
            if (!ready)
            {
                ResetToNeutral();
                LooseFootedSpritsailPhysics.Clear(sail: Sail);
                return false;
            }
            body.isKinematic = false;
            return true;
        }

        internal bool RefreshAerodynamics()
        {
            if (
                !fitted
                || !LooseFootedSpritsailSailingGeometry.TryWindFrame(
                    throat: ClothBone.TransformPoint(position: Data.Sail.Vertices[Data.Throat]),
                    tack: ClothBone.TransformPoint(position: Data.Sail.Vertices[Data.Tack]),
                    peak: ClothBone.TransformPoint(position: Data.Sail.Vertices[Data.Peak]),
                    clew: ClothBone.TransformPoint(position: Data.Sail.Vertices[Data.Clew]),
                    out var wind
                )
            )
                return false;
            Sail.windcenter.SetPositionAndRotation(
                position: wind.Center,
                rotation: Quaternion.LookRotation(forward: wind.Axis, upwards: wind.Normal)
            );
            return true;
        }

        private void ResetToNeutral()
        {
            if (!body.isKinematic)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }
            if (mast)
            {
                transform.localPosition = neutralPosition;
                transform.localRotation = Quaternion.identity;
                UpdatePlate();
            }
        }

        private void UpdatePlate()
        {
            if (!fitted || !mast)
                return;
            var world = neutralWorld * neutralPlate;
            RotationPlate.SetPositionAndRotation(
                position: world.MultiplyPoint3x4(point: Vector3.zero),
                rotation: world.rotation
            );
        }

        internal void Refresh()
        {
            if (!Sail || !Frame || !ScaleRoot || !isActiveAndEnabled)
                return;
            if (transform.parent != lastParent)
            {
                lastParent = transform.parent;
                mast = lastParent ? lastParent.GetComponent<Mast>() : null;
                mastBody = mast ? mast.shipRigidbody : null;
                Boat = mast ? mast.GetComponentInParent<BoatRefs>() : null;
                fitted = false;
                FittedSheetSpan = null;
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
            if (mast.shipRigidbody != mastBody)
            {
                mastBody = mast.shipRigidbody;
                Boat = mast.GetComponentInParent<BoatRefs>();
                dirty = true;
            }
            if (hinge.connectedBody != Sail.shipRigidbody)
                dirty = true;
            float installHeight = Sail.GetCurrentInstallHeight();
            if (installHeight != lastInstallHeight)
                dirty = true;
            lastInstallHeight = installHeight;
            var nextPosition = new Vector3(x: 0, y: 0, z: installHeight - mast.mastHeight);
            if (nextPosition != neutralPosition)
                dirty = true;
            neutralPosition = nextPosition;
            neutralWorld =
                mast.transform.localToWorldMatrix * Matrix4x4.Translate(vector: neutralPosition);
            MeasureMast(out var center, out var axis, out float radius);
            var boat = Boat ? Boat.transform : mast.transform.parent;
            var aft = boat
                ? Quaternion.Inverse(
                    rotation: RelativeMatrix(node: mast.transform, parent: boat).rotation
                ) * Vector3.back
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
            // A fitting change establishes a new hinge rest pose. Normal refreshes never
            // write the sail body's transform, so wind and sheets retain their live angle.
            ResetToNeutral();
            hinge.autoConfigureConnectedAnchor = false;
            hinge.anchor = center;
            hinge.axis = axis;
            hinge.useLimits = true;
            hinge.useSpring = hinge.useMotor = false;
            hinge.connectedBody = Sail.shipRigidbody;
            var pivot = neutralWorld.MultiplyPoint3x4(point: center);
            hinge.connectedAnchor = Sail.shipRigidbody
                ? Sail.shipRigidbody.transform.InverseTransformPoint(position: pivot)
                : pivot;
            body.centerOfMass = center;
            body.inertiaTensor = Vector3.one * body.mass;
            body.inertiaTensorRotation = Quaternion.identity;
            rigging.AngleMaster.UpdateInitialAngle();
            fittedFrame = Time.frameCount;
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
                            matrix: Matrix4x4.Translate(vector: -neutralPosition),
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
            neutralPlate =
                Matrix4x4.TRS(pos: center, q: rotation, s: Vector3.one) * pose.RotationPlate;
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
            var throat = pose.Sail.MultiplyPoint3x4(point: Data.Sail.Vertices[Data.Throat]);
            var peak = pose.Sail.MultiplyPoint3x4(point: Data.Sail.Vertices[Data.Peak]);
            var clew = pose.Sail.MultiplyPoint3x4(point: Data.Sail.Vertices[Data.Clew]);
            var widthVector =
                rotation * new Vector3(x: 0, y: 0, z: Mathf.Min(a: peak.z, b: clew.z) - throat.z);
            FittedSheetSpan = Boat
                ? SailControlGeometry.SheetSpan(
                    neutralWidthInBoat: RelativeMatrix(node: mast.transform, parent: Boat.transform)
                        .MultiplyVector(vector: widthVector)
                )
                : null;
            lastHeight = height;
            lastWidth = width;
            dirty = false;
            fitted = true;
            UpdatePlate();
        }

        // MeshCollider raycasts work even when an imported mesh has Read/Write disabled.
        private float MeasureWalkingSurface(Vector3 center, float radius)
        {
            var toWalk =
                mastSurface.transform.localToWorldMatrix
                * mast.transform.worldToLocalMatrix
                * neutralWorld;
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
                    Matrix4x4.Translate(vector: -neutralPosition)
                    * RelativeMatrix(node: mastCollider.transform, parent: mast.transform);
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

        // Compose local transforms instead of cancelling world matrices. At sea, floating
        // point drift in that cancellation would repeatedly refit and stop a moving hinge.
        private static Matrix4x4 RelativeMatrix(Transform node, Transform parent)
        {
            var matrix = Matrix4x4.identity;
            var current = node;
            while (current && current != parent)
            {
                matrix =
                    Matrix4x4.TRS(
                        pos: current.localPosition,
                        q: current.localRotation,
                        s: current.localScale
                    ) * matrix;
                current = current.parent;
            }
            return current == parent ? matrix : parent.worldToLocalMatrix * node.localToWorldMatrix;
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
            var travel = LooseFootedSpritsailSailingGeometry.PivotRotation(
                center: Frame.localPosition,
                axis: axis,
                degrees: angle
            );
            var position = neutralPosition + travel.MultiplyPoint3x4(point: Vector3.zero);
            var rotation = travel.rotation;
            if (walk && root.parent == walk)
            {
                root.position = walk.TransformPoint(position: position);
                root.rotation = walk.rotation * rotation;
            }
            else
            {
                root.position = mast.transform.TransformPoint(position: position);
                root.rotation = mast.transform.rotation * rotation;
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
