using System;
using MoreSailwindSails.Utils.Profiling;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    // Only the inactive template container owns meshes. Installed copies share
    // these read-only assets and must not destroy them when an individual sail is removed.
    internal sealed class FishermansStaysailAssets : MonoBehaviour
    {
        public Mesh[] Meshes;

        private void OnDestroy()
        {
            if (Meshes == null)
                return;
            foreach (var mesh in Meshes)
                if (mesh)
                    UnityEngine.Object.Destroy(mesh);
        }
    }

    // Serialized fields are intentionally copied into each installed prefab instance.
    [DefaultExecutionOrder(100)]
    internal sealed class FishermansStaysailRig : MonoBehaviour
    {
        public Sail Sail;
        public FishermansStaysailReefing Reefing;
        private Mesh instanceMesh,
            instanceShadow;
        private bool cutInitialized;
        public Transform[] Bones;
        public Vector3[] Corners;
        public Transform SheetAttachment;
        public Transform HalyardAttachment;
        public SkinnedMeshRenderer ReefedRenderer;
        public MeshRenderer FurledColorReference;
        public Transform MastFrame;
        public Vector3 OriginalHingeAxis;
        public Vector3 OriginalHingeAnchor;
        public bool OriginalAutoAnchor;
        public Transform Shadow;
        private FishermansStaysailFixedHead fixedHead;
        private float clothLoad;
        private float camber = 1;
        private int camberSide = 1;
        private bool tensionWarning;
        private readonly Vector3[] leechPoints = new Vector3[FishermansStaysailGeometry.Rows + 1];
        private int lastRenderState = -1;
        private FishermansStaysailRigging rigging;
        private Mast lastMount;
        private bool refreshRequested;
        private bool bindingDirty = true;
        private bool boundToMast;
        private Vector3 pivot;
        private Vector3 pivotAxis;
        private Vector3 lastScale;
        private Vector3 lastFramePosition;
        private Quaternion lastFrameRotation;

        private void OnEnable() => bindingDirty = true;

        private void FixedUpdate() => RefreshFrame();

        internal void RefreshCloth() => refreshRequested = true;

        internal static void Configure(
            Sail sail,
            FishermansStaysailMeshData data,
            Mesh mesh,
            Mesh shadowMesh
        )
        {
            var cloth = sail.cloth;
            var renderer = cloth.GetComponent<SkinnedMeshRenderer>();
            var scaleRoot = cloth.transform.parent;
            var reef = sail.GetComponent<ReefEffectAnimUniversal>();
            if (!reef || !reef.overrideAnimator)
                throw new InvalidOperationException("Expected the brig jib's furl animator.");
            var nativeReefing = FishermansStaysailReefing.Create(sail, reef);
            reef.enabled = false;
            reef.overrideAnimator.enabled = false;
            if (reef.furledSail)
                reef.furledSail.enabled = false;
            // Retain the disabled Animator as Shipyard Expansion's scaling reference.
            scaleRoot.localPosition = Vector3.zero;
            scaleRoot.localRotation = Quaternion.identity;
            cloth.transform.localPosition = Vector3.zero;
            cloth.transform.localRotation = Quaternion.identity;
            cloth.transform.localScale = Vector3.one;

            var rig = sail.gameObject.AddComponent<FishermansStaysailRig>();
            rig.Sail = sail;
            rig.Reefing = nativeReefing;
            rig.Corners = data.Corners;
            var originalHinge = sail.GetComponent<HingeJoint>();
            rig.OriginalHingeAxis = originalHinge.axis;
            rig.OriginalHingeAnchor = originalHinge.anchor;
            rig.OriginalAutoAnchor = originalHinge.autoConfigureConnectedAnchor;
            rig.MastFrame = new GameObject("FishermansStaysail mast pivot frame").transform;
            rig.MastFrame.SetParent(sail.transform, false);
            scaleRoot.SetParent(rig.MastFrame, false);
            rig.Bones = new Transform[data.BonePositions.Length];
            var poses = new Matrix4x4[rig.Bones.Length];
            for (int i = 0; i < rig.Bones.Length; i++)
            {
                var bone = new GameObject("FishermansStaysail corner " + i).transform;
                bone.SetParent(cloth.transform, false);
                bone.localPosition = data.BonePositions[i];
                rig.Bones[i] = bone;
                poses[i] = bone.worldToLocalMatrix * cloth.transform.localToWorldMatrix;
            }
            // RopeEffect.LookAt rotates both endpoint transforms. Never give
            // it a skin bone directly: use independent leaves beneath the bones.
            rig.HalyardAttachment = new GameObject("FishermansStaysail aft-head halyard").transform;
            rig.HalyardAttachment.SetParent(rig.Bones[1], false);
            mesh.bindposes = poses;
            // The donor Cloth contains serialized simulation data for a different
            // topology. Recreate only that component on the inactive clone, after
            // saving its physical settings; WindCloth resolves it in Awake later.
            var clothObject = cloth.gameObject;
            float damping = cloth.damping,
                friction = cloth.friction;
            bool gravity = cloth.useGravity;
            cloth.enabled = false;
            UnityEngine.Object.DestroyImmediate(cloth);

            renderer.sharedMesh = mesh;
            renderer.bones = rig.Bones;
            renderer.rootBone = clothObject.transform;
            renderer.quality = SkinQuality.Bone4;
            renderer.localBounds = new Bounds(
                mesh.bounds.center,
                new Vector3(
                    mesh.bounds.size.x + sail.installHeight * 0.25f,
                    mesh.bounds.size.x * 2.25f,
                    sail.installHeight * 1.25f
                )
            );
            renderer.updateWhenOffscreen = true;
            cloth = clothObject.AddComponent<Cloth>();
            cloth.enabled = false;
            sail.cloth = cloth;
            cloth.bendingStiffness = 0.4f;
            cloth.stretchingStiffness = 0.99f;
            cloth.damping = damping;
            cloth.friction = friction;
            cloth.useGravity = gravity;
            cloth.worldVelocityScale = 0;
            cloth.worldAccelerationScale = 0;
            cloth.clothSolverFrequency = 120;
            cloth.coefficients = data.Constraints;
            if (cloth.coefficients.Length != mesh.vertexCount)
                throw new InvalidOperationException(
                    "The new Cloth does not match the trapezoid vertex count."
                );
            // First binding initializes the instance cut before enabling Cloth.
            cloth.enabled = false;
            var wind = clothObject.GetComponent<WindCloth>();
            if (wind)
            {
                // Keep the donor's serialized wind response (5 on the brig
                // jib). The previous 0.6 override let gravity dominate it.
                wind.minClothDamping = 0.25f;
                wind.maxClothDamping = 0.8f;
            }

            var connections = sail.GetComponent<SailConnections>();
            var left = connections.angleControllerLeft.GetComponent<RopeEffect>();
            var right = connections.angleControllerRight.GetComponent<RopeEffect>();
            rig.SheetAttachment = left.attachment;
            if (!rig.SheetAttachment || right.attachment != rig.SheetAttachment)
                throw new InvalidOperationException("Expected a shared brig jib sheet attachment.");
            rig.SheetAttachment.SetParent(rig.Bones[3], false);
            rig.SheetAttachment.localPosition = Vector3.zero;
            sail.windcenter.SetParent(scaleRoot, false);
            sail.windcenter.localPosition = data.Center;
            // The donor windcenter also carries SailFlapAudio, whose Awake only
            // searches two parents up for Sail. Keep its initial world pose;
            // RefreshAerodynamics supplies the posed center after scaling/reefing.
            sail.windcenter.SetParent(rig.MastFrame, true);
            connections.colChecker.transform.SetParent(scaleRoot, false);
            ConfigureCollision(connections.colChecker, data.Corners);
            foreach (var visual in scaleRoot.GetComponentsInChildren<MeshRenderer>(true))
                visual.enabled = false;
            var shadow = sail.GetComponentInChildren<SailShadowCol>(true);
            if (shadow)
            {
                // SailShadowCol.Awake resolves its Sail exactly two parents up.
                // Keep that contract despite the added mast-pivot frame.
                shadow.transform.SetParent(rig.MastFrame, false);
                rig.Shadow = shadow.transform;
                shadow.transform.localPosition = Vector3.zero;
                shadow.transform.localRotation = Quaternion.identity;
                shadow.transform.localScale = scaleRoot.localScale;
                shadow.GetComponent<MeshFilter>().sharedMesh = shadowMesh;
                var box = shadow.GetComponent<BoxCollider>();
                if (box)
                {
                    box.center = new Vector3(mesh.bounds.center.x, 0, mesh.bounds.center.z);
                    box.size = new Vector3(
                        mesh.bounds.size.x,
                        mesh.bounds.size.y * 2 + 0.1f,
                        mesh.bounds.size.z
                    );
                }
            }
            var reefed = new GameObject("FishermansStaysail reefing cloth");
            reefed.transform.SetParent(scaleRoot, false);
            rig.ReefedRenderer = reefed.AddComponent<SkinnedMeshRenderer>();
            rig.ReefedRenderer.sharedMesh = mesh;
            rig.ReefedRenderer.bones = rig.Bones;
            rig.ReefedRenderer.rootBone = cloth.transform;
            rig.ReefedRenderer.quality = SkinQuality.Bone4;
            rig.ReefedRenderer.localBounds = renderer.localBounds;
            rig.ReefedRenderer.sharedMaterials = renderer.sharedMaterials;
            rig.ReefedRenderer.enabled = false;
            rig.FurledColorReference = nativeReefing.Bundle;
            reef.furledSail = nativeReefing.Bundle;
            var scaler = sail.GetComponent<ShipyardExpansion.SailScaler>();
            scaler.scaleType = ShipyardExpansion.ScaleType.Uniform;
            scaler.rotatablePart = null;
            scaler.flippable = false;
        }

        private static void ConfigureCollision(ShipyardSailColChecker checker, Vector3[] corners)
        {
            checker.startMinAngle = -FishermansStaysailTravel.MaximumAngle;
            checker.startMaxAngle = FishermansStaysailTravel.MaximumAngle;
            checker.colAngleMin = checker.startMinAngle;
            checker.colAngleMax = checker.startMaxAngle;
            // Use narrow inscribed strips rather than the old triangular clew box.
            // Keep the checker outside the animated bones: it measures the fully set sail.
            var root = checker.transform;
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            var old = checker.GetComponentsInChildren<BoxCollider>(true);
            if (old.Length != 1 || old[0].transform.parent != root)
                throw new InvalidOperationException("Unexpected brig jib collision hierarchy.");
            for (int i = 0; i < FishermansStaysailGeometry.Columns; i++)
            {
                var box =
                    i == 0
                        ? old[0]
                        : new GameObject(
                            "FishermansStaysail collision strip " + i
                        ).AddComponent<BoxCollider>();
                box.transform.SetParent(root, false);
                box.transform.localPosition = Vector3.zero;
                box.transform.localRotation = Quaternion.identity;
                box.transform.localScale = Vector3.one;
                box.enabled = FishermansStaysailInstallationGeometry.CollisionStrip(
                    corners,
                    i,
                    0,
                    out var center,
                    out var size
                );
                box.center = center;
                box.size = size;
                box.isTrigger = true;
                var visual = box.GetComponent<MeshRenderer>();
                if (visual)
                    visual.enabled = false;
            }
        }

        internal bool RefreshFrame()
        {
            using (
                PerformanceProfile.Measure(
                    target: ProfileTarget.Frame,
                    family: ProfileFamily.Staysail
                )
            )
            {
                if (
                    !Sail
                    || !MastFrame
                    || Bones == null
                    || Bones.Length != FishermansStaysailGeometry.BoneCount
                )
                    return false;
                var mount = Sail.transform.parent
                    ? Sail.transform.parent.GetComponent<Mast>()
                    : null;
                if (mount != lastMount)
                {
                    bool wasBound = boundToMast;
                    lastMount = mount;
                    fixedHead = default;
                    rigging = mount ? FishermansStaysailRigging.For(Sail) : null;
                    if (wasBound && !rigging)
                    {
                        MastFrame.localPosition = Vector3.zero;
                        MastFrame.localRotation = Quaternion.identity;
                        var originalHinge = Sail.GetComponent<HingeJoint>();
                        originalHinge.axis = OriginalHingeAxis;
                        originalHinge.anchor = OriginalHingeAnchor;
                        originalHinge.autoConfigureConnectedAnchor = OriginalAutoAnchor;
                        refreshRequested = true;
                    }
                    boundToMast = false;
                    bindingDirty = true;
                }
                if (!rigging || !rigging.Bind(mount))
                    return false;

                if (!cutInitialized)
                    InitializeCut(rigging.HeadSlope);
                rigging.ForeSailFrame(out var forePoint, out var foreAxis);
                var scaleRoot = Sail.cloth.transform.parent;
                // Preserve the native saved installation coordinate. Offset the
                // model so its whole luff sits on the physical forward mast even
                // when the sail is narrower than the distance between the masts.
                var origin = new Vector3(0, 0, Sail.GetCurrentInstallHeight() - mount.mastHeight);
                var nextPivot = mount.transform.InverseTransformPoint(forePoint) - origin;
                var nextAxis = mount.transform.InverseTransformDirection(foreAxis).normalized;
                var aftDirection = mount.transform.InverseTransformDirection(
                    Vector3.ProjectOnPlane(rigging.AftReference - forePoint, foreAxis).normalized
                );
                var alignment =
                    Quaternion.LookRotation(aftDirection, Vector3.Cross(aftDirection, nextAxis))
                    * Quaternion.Inverse(scaleRoot.localRotation);
                MastFrame.localRotation = alignment;
                MastFrame.localPosition = FishermansStaysailFrameGeometry.ModelOffset(
                    nextPivot,
                    alignment
                        * (
                            scaleRoot.localPosition
                            + scaleRoot.localRotation
                                * Vector3.Scale(Corners[0], scaleRoot.localScale)
                        )
                );
                bool changed =
                    bindingDirty
                    || !boundToMast
                    || FishermansStaysailFrameGeometry.PositionChanged(nextPivot, pivot)
                    || FishermansStaysailFrameGeometry.PositionChanged(nextAxis, pivotAxis)
                    || FishermansStaysailFrameGeometry.PositionChanged(
                        lastScale,
                        scaleRoot.localScale
                    )
                    || FishermansStaysailFrameGeometry.PositionChanged(
                        lastFramePosition,
                        MastFrame.localPosition
                    )
                    || Quaternion.Angle(lastFrameRotation, MastFrame.localRotation) > 0.05f;
                if (changed)
                {
                    var body = Sail.GetComponent<Rigidbody>();
                    var hinge = Sail.GetComponent<HingeJoint>();
                    if (!boundToMast || GameState.currentShipyard)
                        body.rotation = mount.transform.rotation;
                    body.position = forePoint - body.rotation * nextPivot;
                    RefreshCollisionStrips();
                    // The gathered panel stays within its deployed vertical bounds.
                    var clothRenderer = Sail.cloth.GetComponent<SkinnedMeshRenderer>();
                    var bounds = clothRenderer.sharedMesh.bounds;
                    bounds.Expand(
                        new Vector3(
                            -Corners[0].z * 0.3f,
                            -Corners[0].z * 2.5f,
                            -Corners[0].z * 0.4f
                        )
                    );
                    clothRenderer.localBounds = bounds;
                    ReefedRenderer.localBounds = bounds;
                    hinge.autoConfigureConnectedAnchor = false;
                    hinge.connectedBody = mount.shipRigidbody;
                    hinge.axis = nextAxis;
                    hinge.anchor = nextPivot;
                    hinge.connectedAnchor = mount.shipRigidbody.transform.InverseTransformPoint(
                        forePoint
                    );
                    if (!boundToMast)
                        Sail.GetComponent<JibAngleMaster>().UpdateInitialAngle();
                    pivot = nextPivot;
                    pivotAxis = nextAxis;
                    lastScale = scaleRoot.localScale;
                    lastFramePosition = MastFrame.localPosition;
                    lastFrameRotation = MastFrame.localRotation;
                    boundToMast = true;
                    bindingDirty = false;
                    refreshRequested = true;
                }
                return true;
            }
        }

        // This runs once per installed/preview instance, before its Cloth is enabled.
        // Each shared inactive prefab retains its mark's nominal cut.
        private void InitializeCut(float slope)
        {
            Sail.cloth.enabled = false;
            var data = GetComponent<FishermansStaysailShape>().Create(-Corners[0].z, slope);
            Corners = data.Corners;
            var renderer = Sail.cloth.GetComponent<SkinnedMeshRenderer>();
            instanceMesh = UnityEngine.Object.Instantiate(renderer.sharedMesh);
            instanceMesh.name =
                GetComponent<FishermansStaysailShape>().ObjectPrefix + " fitted cloth";
            instanceMesh.vertices = data.Vertices;
            var poses = new Matrix4x4[Bones.Length];
            for (int i = 0; i < Bones.Length; i++)
            {
                Bones[i].localPosition = data.BonePositions[i];
                poses[i] = Bones[i].worldToLocalMatrix * Sail.cloth.transform.localToWorldMatrix;
            }
            instanceMesh.bindposes = poses;
            instanceMesh.RecalculateNormals();
            instanceMesh.RecalculateTangents();
            instanceMesh.RecalculateBounds();
            renderer.sharedMesh = instanceMesh;
            ReefedRenderer.sharedMesh = instanceMesh;
            Sail.cloth.coefficients = data.Constraints;
            if (Shadow)
            {
                var filter = Shadow.GetComponent<MeshFilter>();
                instanceShadow = UnityEngine.Object.Instantiate(filter.sharedMesh);
                var samples = new Vector3[9];
                for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                {
                    samples[row * 3 + col] = data.Vertices[
                        row
                            * (FishermansStaysailGeometry.Rows / 2)
                            * (FishermansStaysailGeometry.Columns + 1)
                            + col * (FishermansStaysailGeometry.Columns / 2)
                    ];
                    samples[row * 3 + col].y = 0;
                }
                instanceShadow.vertices = samples;
                instanceShadow.RecalculateBounds();
                filter.sharedMesh = instanceShadow;
                var box = Shadow.GetComponent<BoxCollider>();
                box.center = new Vector3(
                    instanceMesh.bounds.center.x,
                    0,
                    instanceMesh.bounds.center.z
                );
                box.size = new Vector3(
                    instanceMesh.bounds.size.x,
                    instanceMesh.bounds.size.y * 2 + 0.1f,
                    instanceMesh.bounds.size.z
                );
            }
            cutInitialized = true;
            Sail.SetSailArea();
        }

        private void OnDestroy()
        {
            if (instanceMesh)
                UnityEngine.Object.Destroy(instanceMesh);
            if (instanceShadow)
                UnityEngine.Object.Destroy(instanceShadow);
        }

        private void RefreshCollisionStrips()
        {
            // Cloth attaches to the supporting mast's axis. Its contact with the
            // mast and the roots of its fittings is intentional, not obstruction.
            var mastCollider = rigging.Pair.Fore.GetComponent<CapsuleCollider>();
            var mastScale = mastCollider.transform.lossyScale;
            float radius =
                mastCollider.radius
                * Mathf.Max(
                    mastCollider.direction == 0 ? mastScale.y : mastScale.x,
                    mastCollider.direction == 2 ? mastScale.y : mastScale.z
                );
            float widthScale = Sail.cloth.transform.TransformVector(Vector3.forward).magnitude;
            float clearance = (radius + 0.02f) / Mathf.Max(0.0001f, widthScale);
            var boxes = Sail.GetComponent<SailConnections>()
                .colChecker.GetComponentsInChildren<BoxCollider>(true);
            for (int i = 0; i < boxes.Length; i++)
            {
                boxes[i].enabled = FishermansStaysailInstallationGeometry.CollisionStrip(
                    Corners,
                    i,
                    clearance,
                    out var center,
                    out var size
                );
                boxes[i].center = center;
                boxes[i].size = size;
            }
        }

        internal bool PositionCollisionChecker(
            Transform checker,
            float angle,
            out Quaternion neutralRotation
        )
        {
            neutralRotation = Quaternion.identity;
            var walk = lastMount.walkColMast;
            if (!walk || checker.parent != walk)
                return false;
            var scaleRoot = Sail.cloth.transform.parent;
            var rotation = Quaternion.AngleAxis(angle, pivotAxis);
            var origin = new Vector3(0, 0, Sail.GetCurrentInstallHeight() - lastMount.mastHeight);
            var modelOffset =
                MastFrame.localPosition + MastFrame.localRotation * scaleRoot.localPosition;
            var position =
                origin
                + FishermansStaysailFrameGeometry.RotateAroundMast(
                    modelOffset,
                    pivot,
                    pivotAxis,
                    angle
                );
            neutralRotation = MastFrame.localRotation * scaleRoot.localRotation;
            checker.SetPositionAndRotation(
                walk.TransformPoint(position),
                walk.rotation * rotation * neutralRotation
            );
            checker.localScale = scaleRoot.localScale;
            return true;
        }

        internal bool RefreshAerodynamics()
        {
            using (
                PerformanceProfile.Measure(
                    target: ProfileTarget.Aerodynamics,
                    family: ProfileFamily.Staysail
                )
            )
            {
                if (!Sail || !Sail.windcenter || Bones == null || Bones.Length < 4)
                    return false;
                if (
                    !FishermansStaysailAerodynamics.TryFrame(
                        Bones[0].position,
                        Bones[2].position,
                        Bones[1].position,
                        Bones[3].position,
                        out var frame
                    )
                )
                    return false;
                Sail.windcenter.SetPositionAndRotation(
                    frame.Center,
                    Quaternion.LookRotation(frame.MastAxis, frame.Normal)
                );
                return true;
            }
        }

        private void UpdateShapeBones()
        {
            var normal = FishermansStaysailBillow.CamberNormal(
                Bones[0].localPosition,
                Bones[2].localPosition,
                Bones[1].localPosition,
                Bones[3].localPosition
            );
            var flow = Sail.cloth.transform.InverseTransformDirection(Sail.apparentWind);
            camberSide = FishermansStaysailBillow.CamberSide(
                camberSide,
                Vector3.Dot(flow, normal) * Sail.GetCurrentShadowMult()
            );
            camber = FishermansStaysailBillow.SmoothLoad(camber, camberSide, Time.deltaTime);
            float deployedCamber = camber * FishermansStaysailBillow.Deployment(Sail.currentUnroll);
            for (int row = 0; row <= FishermansStaysailGeometry.Rows; row++)
            {
                float v = (float)row / FishermansStaysailGeometry.Rows;
                var fore = Vector3.Lerp(Bones[0].localPosition, Bones[2].localPosition, v);
                if (rigging != null && rigging.Pair != null && rigging.Pair.Active)
                    fore = Sail.cloth.transform.InverseTransformPoint(
                        rigging.LuffPoint(Sail.cloth.transform.TransformPoint(fore))
                    );
                var aft = Bones[FishermansStaysailGeometry.LeechBone(row)].localPosition;
                for (int column = 0; column < FishermansStaysailGeometry.ShapeColumns; column++)
                {
                    int bone = FishermansStaysailGeometry.ShapeBone(row, column);
                    if (bone == 0 || bone == 2)
                        continue;
                    Bones[bone].localPosition = FishermansStaysailBillow.ShapePoint(
                        fore,
                        aft,
                        normal,
                        -Corners[0].z,
                        (float)column / FishermansStaysailGeometry.ShapeColumns,
                        v,
                        deployedCamber
                    );
                }
            }
        }

        private Vector3 NeutralPoint(Vector3 point)
        {
            var scaleRoot = Sail.cloth.transform.parent;
            var origin = new Vector3(0, 0, Sail.GetCurrentInstallHeight() - lastMount.mastHeight);
            return lastMount.transform.TransformPoint(
                origin
                    + MastFrame.localPosition
                    + MastFrame.localRotation
                        * (
                            scaleRoot.localPosition
                            + scaleRoot.localRotation * Vector3.Scale(point, scaleRoot.localScale)
                        )
            );
        }

        internal bool TrySheetAngle(out float angle)
        {
            angle = 0;
            if (!boundToMast || !lastMount || rigging?.Pair == null)
                return false;
            angle = Mathf.Abs(
                FishermansStaysailTravel.SignedAngle(
                    rigging.AftReference - rigging.ForePoint,
                    Sail.cloth.transform.forward,
                    rigging.ForeAxis
                )
            );
            return true;
        }

        private void LateUpdate()
        {
            using (
                PerformanceProfile.Measure(
                    target: ProfileTarget.Rig,
                    family: ProfileFamily.Staysail
                )
            )
            {
                if (!Sail || Bones == null || Bones.Length != FishermansStaysailGeometry.BoneCount)
                    return;
                bool supported = RefreshFrame();
                if (Shadow)
                {
                    var scale = Sail.cloth.transform.parent;
                    Shadow.localPosition = scale.localPosition;
                    Shadow.localRotation = scale.localRotation;
                    Shadow.localScale = scale.localScale;
                }
                int state;
                using (PerformanceProfile.Measure(target: ProfileTarget.Shape))
                {
                    float targetLoad = !supported
                        ? 0
                        : Sail.cloth.transform.InverseTransformDirection(Sail.apparentWind).y
                            / 8f
                            * Sail.GetCurrentShadowMult();
                    clothLoad = FishermansStaysailBillow.SmoothLoad(
                        clothLoad,
                        targetLoad,
                        Time.deltaTime
                    );
                    Reefing.Sample(Sail.currentUnroll);
                    for (int i = 0; i < Corners.Length; i++)
                        Bones[i].localPosition = Reefing.Pose(Corners[i], Corners[0], Corners[1]);

                    if (supported)
                        for (int index = 0; index <= 2; index += 2)
                            Bones[index].position = rigging.LuffPoint(Bones[index].position);
                    state = FishermansStaysailGeometry.RenderState(Sail.currentUnroll);
                    for (int row = 1; row < FishermansStaysailGeometry.Rows; row++)
                        Bones[FishermansStaysailGeometry.LeechBone(row)].localPosition =
                            Vector3.Lerp(
                                Bones[1].localPosition,
                                Bones[3].localPosition,
                                (float)row / FishermansStaysailGeometry.Rows
                            );
                    if (supported)
                    {
                        var neutralHead = NeutralPoint(Corners[1]);
                        var clothTransform = Sail.cloth.transform;
                        rigging.ForeSailFrame(out var forePoint, out var mastAxis);
                        var shape = GetComponent<FishermansStaysailShape>();
                        var fixedAngle = shape.FixedUpperHeadAngle;
                        fixedHead.Update(
                            Sail.apparentWind,
                            mastAxis,
                            rigging.AftReference - forePoint,
                            Time.deltaTime
                        );
                        var head = FishermansStaysailFixedHead.Position(
                            neutralHead,
                            forePoint,
                            mastAxis,
                            fixedHead.Side,
                            fixedAngle,
                            Sail.currentUnroll
                        );
                        var localHead = clothTransform.InverseTransformPoint(head);
                        var normal = Vector3
                            .Cross(mastAxis, rigging.AftReference - forePoint)
                            .normalized;
                        var clew = Bones[3].localPosition;
                        // As the panel gathers, bring its foot back under the neutral
                        // head. A short reefed leech cannot span a fully eased sheet.
                        var neutralClew = NeutralPoint(clew);
                        float sheetAngle = FishermansStaysailTravel.SignedAngle(
                            rigging.AftReference - forePoint,
                            clothTransform.forward,
                            mastAxis
                        );
                        clew = clothTransform.InverseTransformPoint(
                            FishermansStaysailFrameGeometry.RotateAroundMast(
                                neutralClew,
                                forePoint,
                                mastAxis,
                                FishermansStaysailFixedHead.LowerAngle(
                                    sheetAngle,
                                    fixedHead.Side * fixedAngle,
                                    Sail.currentUnroll
                                )
                            )
                        );
                        var tack = Bones[2].localPosition;
                        bool fitted = FishermansStaysailEdgeFit.Fit(
                            localHead,
                            clew,
                            tack,
                            clothTransform.InverseTransformDirection(normal),
                            clothTransform.InverseTransformDirection(Vector3.down),
                            -Corners[0].z,
                            clothLoad,
                            Sail.currentUnroll,
                            (Corners[1] - Corners[3]).magnitude * Mathf.Max(0.015f, Reefing.Lift),
                            (clew - tack).magnitude,
                            leechPoints
                        );
                        if (!fitted && !tensionWarning && state == 2)
                        {
                            Plugin.Log.LogWarning(
                                "FishermansStaysail corner span exceeds available foot/leech lengths; check sail fit."
                            );
                            tensionWarning = true;
                        }
                        for (int row = 0; row <= FishermansStaysailGeometry.Rows; row++)
                            Bones[FishermansStaysailGeometry.LeechBone(row)].localPosition =
                                leechPoints[FishermansStaysailGeometry.Rows - row];
                        HalyardAttachment.localPosition = Vector3.zero;
                        SheetAttachment.localPosition = Vector3.zero;
                        rigging.UpdateHalyard(HalyardAttachment);
                    }
                    UpdateShapeBones();
                }
                RefreshAerodynamics();
                if (refreshRequested || state != lastRenderState)
                {
                    // Refresh after applying our corner poses. The donor animator's
                    // Start never runs, so its RefreshCloth must not be invoked.
                    Sail.cloth.enabled = false;
                    Sail.cloth.ClearTransformMotion();
                    refreshRequested = false;
                }
                Sail.cloth.enabled = supported && state == 2;
                var clothRenderer = Sail.cloth.GetComponent<SkinnedMeshRenderer>();
                bool visible = supported && !GameState.currentlyLoading;
                // WindCloth writes renderer.enabled in Update; select the correct
                // renderer here in LateUpdate so the disabled solver cannot leave
                // stale full-size triangles visible when the sail is struck.
                clothRenderer.enabled = visible && state == 2;
                ReefedRenderer.sharedMaterial = clothRenderer.sharedMaterial;
                FurledColorReference.sharedMaterial = clothRenderer.sharedMaterial;
                ReefedRenderer.enabled = visible && state == 1;
                Reefing.DrawBundle(
                    Bones[0].position,
                    Bones[1].position,
                    supported ? rigging.ForeAxis : Sail.cloth.transform.right,
                    Sail.cloth.transform.parent.localScale.x,
                    clothRenderer.sharedMaterial,
                    visible && state == 0
                );
                lastRenderState = state;
            }
        }
    }
}
