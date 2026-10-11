using System;
using MoreSailwindSails.Utils.Profiling;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansFlyingSail
{
    // Only the inactive template container owns meshes. Installed copies share
    // these read-only assets and must not destroy them when an individual sail is removed.
    internal sealed class FishermansFlyingSailAssets : MonoBehaviour
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
    internal sealed class FishermansFlyingSailRig : MonoBehaviour
    {
        public Sail Sail;
        public Transform[] Bones;
        public Vector3[] Corners;
        public Transform SheetAttachment;
        public Transform[] HalyardAttachments;
        public SkinnedMeshRenderer ReefedRenderer;
        public MeshRenderer FurledColorReference;
        public Transform FlyingFrame;
        public Vector3 OriginalHingeAxis;
        public Vector3 OriginalHingeAnchor;
        public bool OriginalAutoAnchor;
        public FishermansFlyingSailSupportLine SupportLine;
        public Transform Shadow;
        private float clothLoad;
        private float camber = 1;
        private int camberSide = 1;
        private bool tensionWarning;
        private readonly Vector3[] leechPoints = new Vector3[FishermansFlyingSailGeometry.Rows + 1];
        private int lastRenderState = -1;
        private FishermansFlyingSailRigging rigging;
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

        private void FixedUpdate() => RefreshFlyingFrame();

        internal void RefreshCloth() => refreshRequested = true;

        internal static void Configure(
            Sail sail,
            FishermansFlyingSailMeshData data,
            Mesh mesh,
            Mesh shadowMesh
        )
        {
            float width = data.Corners[1].z - data.Corners[0].z;
            var cloth = sail.cloth;
            var renderer = cloth.GetComponent<SkinnedMeshRenderer>();
            var scaleRoot = cloth.transform.parent;
            var reef = sail.GetComponent<ReefEffectAnimUniversal>();
            if (!reef || !reef.overrideAnimator)
                throw new InvalidOperationException("Expected the brig jib's furl animator.");
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

            var rig = sail.gameObject.AddComponent<FishermansFlyingSailRig>();
            rig.Sail = sail;
            rig.Corners = data.Corners;
            var originalHinge = sail.GetComponent<HingeJoint>();
            rig.OriginalHingeAxis = originalHinge.axis;
            rig.OriginalHingeAnchor = originalHinge.anchor;
            rig.OriginalAutoAnchor = originalHinge.autoConfigureConnectedAnchor;
            rig.FlyingFrame = new GameObject("FishermansFlyingSail mast pivot frame").transform;
            rig.FlyingFrame.SetParent(sail.transform, false);
            scaleRoot.SetParent(rig.FlyingFrame, false);
            rig.Bones = new Transform[data.BonePositions.Length];
            var poses = new Matrix4x4[rig.Bones.Length];
            for (int i = 0; i < rig.Bones.Length; i++)
            {
                var bone = new GameObject("FishermansFlyingSail corner " + i).transform;
                bone.SetParent(cloth.transform, false);
                bone.localPosition = data.BonePositions[i];
                rig.Bones[i] = bone;
                poses[i] = bone.worldToLocalMatrix * cloth.transform.localToWorldMatrix;
            }
            // RopeEffect.LookAt rotates both endpoint transforms. Never give
            // it a skin bone directly: use independent leaves beneath the bones.
            rig.HalyardAttachments = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var attachment = new GameObject("FishermansFlyingSail head halyard " + i).transform;
                attachment.SetParent(rig.Bones[i], false);
                rig.HalyardAttachments[i] = attachment;
            }
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
                    mesh.bounds.size.x + width * 0.25f,
                    mesh.bounds.size.x * 2.25f,
                    width * 1.25f
                )
            );
            renderer.updateWhenOffscreen = true;
            cloth = clothObject.AddComponent<Cloth>();
            cloth.enabled = false;
            sail.cloth = cloth;
            cloth.bendingStiffness = 0.15f;
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
            cloth.enabled = true;
            var wind = clothObject.GetComponent<WindCloth>();
            if (wind)
            {
                // Keep the donor's serialized wind response (5 on the brig
                // jib). The previous 0.6 override let gravity dominate it.
                wind.minClothDamping = 0.08f;
                wind.maxClothDamping = 0.45f;
            }

            var connections = sail.GetComponent<SailConnections>();
            var left = connections.angleControllerLeft.GetComponent<RopeEffect>();
            var right = connections.angleControllerRight.GetComponent<RopeEffect>();
            rig.SupportLine = FishermansFlyingSailSupportLine.Create(
                sail.transform,
                left,
                right,
                rig.Bones
            );
            rig.SheetAttachment = left.attachment;
            if (!rig.SheetAttachment || right.attachment != rig.SheetAttachment)
                throw new InvalidOperationException("Expected a shared brig jib sheet attachment.");
            rig.SheetAttachment.SetParent(rig.Bones[3], false);
            rig.SheetAttachment.localPosition = Vector3.zero;
            sail.windcenter.SetParent(scaleRoot, false);
            sail.windcenter.localPosition = data.Center;
            // The donor windcenter also carries SailFlapAudio, whose Awake only
            // searches two parents up for Sail. Keep its initial world pose;
            // RefreshAerodynamics supplies the posed center after scaling/hoisting.
            sail.windcenter.SetParent(rig.FlyingFrame, true);
            connections.colChecker.transform.SetParent(scaleRoot, false);
            ConfigureCollision(connections.colChecker, width);
            foreach (var visual in scaleRoot.GetComponentsInChildren<MeshRenderer>(true))
                visual.enabled = false;
            var shadow = sail.GetComponentInChildren<SailShadowCol>(true);
            if (shadow)
            {
                // SailShadowCol.Awake resolves its Sail exactly two parents up.
                // Keep that contract despite the added mast-pivot frame.
                shadow.transform.SetParent(rig.FlyingFrame, false);
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
            var reefed = new GameObject("FishermansFlyingSail reefing cloth");
            reefed.transform.SetParent(scaleRoot, false);
            rig.ReefedRenderer = reefed.AddComponent<SkinnedMeshRenderer>();
            rig.ReefedRenderer.sharedMesh = mesh;
            rig.ReefedRenderer.bones = rig.Bones;
            rig.ReefedRenderer.rootBone = cloth.transform;
            rig.ReefedRenderer.quality = SkinQuality.Bone4;
            rig.ReefedRenderer.localBounds = renderer.localBounds;
            rig.ReefedRenderer.sharedMaterials = renderer.sharedMaterials;
            rig.ReefedRenderer.enabled = false;
            var furled = new GameObject("FishermansFlyingSail furled color reference");
            furled.transform.SetParent(scaleRoot, false);
            rig.FurledColorReference = furled.AddComponent<MeshRenderer>();
            rig.FurledColorReference.sharedMaterials = renderer.sharedMaterials;
            rig.FurledColorReference.enabled = false;
            reef.furledSail = rig.FurledColorReference;
        }

        private static void ConfigureCollision(ShipyardSailColChecker checker, float width)
        {
            checker.startMinAngle = -FishermansFlyingSailTravel.MaximumAngle;
            checker.startMaxAngle = FishermansFlyingSailTravel.MaximumAngle;
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
            for (int i = 0; i < FishermansFlyingSailGeometry.Columns; i++)
            {
                var box =
                    i == 0
                        ? old[0]
                        : new GameObject(
                            "FishermansFlyingSail collision strip " + i
                        ).AddComponent<BoxCollider>();
                box.transform.SetParent(root, false);
                box.transform.localPosition = Vector3.zero;
                box.transform.localRotation = Quaternion.identity;
                box.transform.localScale = Vector3.one;
                FishermansFlyingSailMastInstallationGeometry.CollisionStrip(
                    width,
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

        internal bool RefreshFlyingFrame()
        {
            using (
                PerformanceProfile.Measure(
                    target: ProfileTarget.Frame,
                    family: ProfileFamily.FlyingSail
                )
            )
            {
                if (
                    !Sail
                    || !FlyingFrame
                    || Bones == null
                    || Bones.Length != FishermansFlyingSailGeometry.BoneCount
                )
                    return false;
                var mount = Sail.transform.parent
                    ? Sail.transform.parent.GetComponent<Mast>()
                    : null;
                if (mount != lastMount)
                {
                    bool wasBound = boundToMast;
                    lastMount = mount;
                    rigging = mount ? FishermansFlyingSailRigging.For(Sail) : null;
                    if (wasBound && !rigging)
                    {
                        FlyingFrame.localPosition = Vector3.zero;
                        FlyingFrame.localRotation = Quaternion.identity;
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

                rigging.LuffSailFrame(out var forePoint, out var foreAxis);
                var scaleRoot = Sail.cloth.transform.parent;
                // Preserve the native saved installation coordinate. Offset the
                // model and hinge together onto the fixed offset luff line. Sheet
                // rotation then leaves both short mast ties stationary.
                var origin = new Vector3(0, 0, Sail.GetCurrentInstallHeight() - mount.mastHeight);
                var nextPivot = mount.transform.InverseTransformPoint(forePoint) - origin;
                var nextAxis = mount.transform.InverseTransformDirection(foreAxis).normalized;
                var aftDirection = mount.transform.InverseTransformDirection(
                    Vector3.ProjectOnPlane(rigging.AftReference - forePoint, foreAxis).normalized
                );
                var alignment =
                    Quaternion.LookRotation(aftDirection, Vector3.Cross(aftDirection, nextAxis))
                    * Quaternion.Inverse(scaleRoot.localRotation);
                FlyingFrame.localRotation = alignment;
                FlyingFrame.localPosition = FishermansFlyingSailFrameGeometry.ModelOffset(
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
                    || FishermansFlyingSailFrameGeometry.PositionChanged(nextPivot, pivot)
                    || FishermansFlyingSailFrameGeometry.PositionChanged(nextAxis, pivotAxis)
                    || FishermansFlyingSailFrameGeometry.PositionChanged(
                        lastScale,
                        scaleRoot.localScale
                    )
                    || FishermansFlyingSailFrameGeometry.PositionChanged(
                        lastFramePosition,
                        FlyingFrame.localPosition
                    )
                    || Quaternion.Angle(lastFrameRotation, FlyingFrame.localRotation) > 0.05f;
                if (changed)
                {
                    var body = Sail.GetComponent<Rigidbody>();
                    var hinge = Sail.GetComponent<HingeJoint>();
                    if (!boundToMast || GameState.currentShipyard)
                        body.rotation = mount.transform.rotation;
                    body.position = forePoint - body.rotation * nextPivot;
                    RefreshCollisionStrips();
                    RefreshClothTravel();
                    // Hoisting can start well below the fully set panel's original
                    // bounds, especially after fitting a smaller sail high on a mast.
                    var clothRenderer = Sail.cloth.GetComponent<SkinnedMeshRenderer>();
                    var bounds = clothRenderer.sharedMesh.bounds;
                    bounds.Encapsulate(
                        Sail.cloth.transform.InverseTransformPoint(rigging.DeckPoint)
                    );
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
                    lastFramePosition = FlyingFrame.localPosition;
                    lastFrameRotation = FlyingFrame.localRotation;
                    boundToMast = true;
                    bindingDirty = false;
                    refreshRequested = true;
                }
                return true;
            }
        }

        private void RefreshCollisionStrips()
        {
            // The entire neutral panel is outside the mast rim. Keep its first
            // strip: clipping a mast radius here would hide real obstructions.
            var boxes = Sail.GetComponent<SailConnections>()
                .colChecker.GetComponentsInChildren<BoxCollider>(true);
            for (int i = 0; i < boxes.Length; i++)
            {
                boxes[i].enabled = FishermansFlyingSailMastInstallationGeometry.CollisionStrip(
                    -Corners[0].z,
                    i,
                    0,
                    out var center,
                    out var size
                );
                boxes[i].center = center;
                boxes[i].size = size;
            }
        }

        private void ClothScales(out float minimum, out float maximum)
        {
            var scale = Sail.cloth.transform.lossyScale;
            minimum = Mathf.Max(
                0.0001f,
                Mathf.Min(Mathf.Abs(scale.x), Mathf.Min(Mathf.Abs(scale.y), Mathf.Abs(scale.z)))
            );
            maximum = Mathf.Max(
                minimum,
                Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)))
            );
        }

        // Only fitting/scaling updates coefficients; tacks move existing bones.
        private void RefreshClothTravel()
        {
            ClothScales(out var minimum, out var maximum);
            var coefficients = Sail.cloth.coefficients;
            for (int row = 0; row <= FishermansFlyingSailGeometry.Rows; row++)
            for (int col = 0; col <= FishermansFlyingSailGeometry.Columns; col++)
            {
                bool pinned =
                    (row == 0 || row == FishermansFlyingSailGeometry.Rows)
                    && (col == 0 || col == FishermansFlyingSailGeometry.Columns);
                coefficients[row * (FishermansFlyingSailGeometry.Columns + 1) + col].maxDistance =
                    pinned
                        ? 0
                        : FishermansFlyingSailBillow.ClothTravel(
                            -Corners[0].z,
                            (float)col / FishermansFlyingSailGeometry.Columns,
                            (float)row / FishermansFlyingSailGeometry.Rows,
                            minimum,
                            maximum
                        );
            }
            Sail.cloth.coefficients = coefficients;
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
                FlyingFrame.localPosition + FlyingFrame.localRotation * scaleRoot.localPosition;
            var position =
                origin
                + FishermansFlyingSailFrameGeometry.RotateAroundMast(
                    modelOffset,
                    pivot,
                    pivotAxis,
                    angle
                );
            neutralRotation = FlyingFrame.localRotation * scaleRoot.localRotation;
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
                    family: ProfileFamily.FlyingSail
                )
            )
            {
                if (!Sail || !Sail.windcenter || Bones == null || Bones.Length < 4)
                    return false;
                if (
                    !FishermansFlyingSailAerodynamics.TryFrame(
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

        private void UpdateShapeBones(bool supported)
        {
            var normal = FishermansFlyingSailBillow.CamberNormal(
                Bones[0].localPosition,
                Bones[2].localPosition,
                Bones[1].localPosition,
                Bones[3].localPosition
            );
            var flow = Sail.cloth.transform.InverseTransformDirection(Sail.apparentWind);
            camberSide = FishermansFlyingSailBillow.CamberSide(
                camberSide,
                Vector3.Dot(flow, normal) * Sail.GetCurrentShadowMult()
            );
            camber = FishermansFlyingSailBillow.SmoothLoad(camber, camberSide, Time.deltaTime);
            float deployedCamber =
                camber * FishermansFlyingSailBillow.Deployment(Sail.currentUnroll);
            var mastward = Vector3.back;
            float minimumScale = 1;
            if (supported)
            {
                rigging.LuffSailFrame(out var luffPoint, out var axis);
                var aft = FishermansFlyingSailFrameGeometry.AftDirection(
                    luffPoint,
                    axis,
                    rigging.AftReference
                );
                ClothScales(out minimumScale, out _);
                // Never ask for more luff arc length than the scaled rest mesh
                // contains, and never scale the inward arch beyond nine inches.
                mastward = Sail.cloth.transform.InverseTransformVector(-aft);
            }
            for (int row = 0; row <= FishermansFlyingSailGeometry.Rows; row++)
            {
                float v = (float)row / FishermansFlyingSailGeometry.Rows;
                var fore = Vector3.Lerp(Bones[0].localPosition, Bones[2].localPosition, v);
                var aft = Bones[FishermansFlyingSailGeometry.LeechBone(row)].localPosition;
                for (int column = 0; column < FishermansFlyingSailGeometry.ShapeColumns; column++)
                {
                    int bone = FishermansFlyingSailGeometry.ShapeBone(row, column);
                    if (bone == 0 || bone == 2)
                        continue;
                    Bones[bone].localPosition = FishermansFlyingSailBillow.ShapePoint(
                        fore,
                        aft,
                        normal,
                        -Corners[0].z,
                        (float)column / FishermansFlyingSailGeometry.ShapeColumns,
                        v,
                        deployedCamber,
                        mastward,
                        FishermansFlyingSailBillow.Deployment(Sail.currentUnroll),
                        minimumScale
                    );
                }
            }
        }

        private void OnDisable()
        {
            if (SupportLine)
                SupportLine.Hide();
        }

        private void LateUpdate()
        {
            using (
                PerformanceProfile.Measure(
                    target: ProfileTarget.Rig,
                    family: ProfileFamily.FlyingSail
                )
            )
            {
                if (
                    !Sail
                    || Bones == null
                    || Bones.Length != FishermansFlyingSailGeometry.BoneCount
                )
                    return;
                bool supported = RefreshFlyingFrame();
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
                    clothLoad = FishermansFlyingSailBillow.SmoothLoad(
                        clothLoad,
                        targetLoad,
                        Time.deltaTime
                    );
                    var deck = supported
                        ? Sail.cloth.transform.InverseTransformPoint(rigging.DeckPoint)
                        : Corners[2];
                    for (int i = 0; i < Corners.Length; i++)
                        Bones[i].localPosition =
                            FishermansFlyingSailMastInstallationGeometry.HoistCorner(
                                Corners[i],
                                Corners[0],
                                deck,
                                Sail.currentUnroll
                            );

                    state = FishermansFlyingSailGeometry.RenderState(Sail.currentUnroll);
                    for (int row = 1; row < FishermansFlyingSailGeometry.Rows; row++)
                        Bones[FishermansFlyingSailGeometry.LeechBone(row)].localPosition =
                            Vector3.Lerp(
                                Bones[1].localPosition,
                                Bones[3].localPosition,
                                (float)row / FishermansFlyingSailGeometry.Rows
                            );
                    if (supported)
                    {
                        var scaleRoot = Sail.cloth.transform.parent;
                        var origin = new Vector3(
                            0,
                            0,
                            Sail.GetCurrentInstallHeight() - lastMount.mastHeight
                        );
                        var neutralHead = lastMount.transform.TransformPoint(
                            origin
                                + FlyingFrame.localPosition
                                + FlyingFrame.localRotation
                                    * (
                                        scaleRoot.localPosition
                                        + scaleRoot.localRotation
                                            * Vector3.Scale(
                                                FishermansFlyingSailMastInstallationGeometry.HoistCorner(
                                                    Corners[1],
                                                    Corners[0],
                                                    deck,
                                                    Sail.currentUnroll
                                                ),
                                                scaleRoot.localScale
                                            )
                                    )
                        );
                        var clothTransform = Sail.cloth.transform;
                        rigging.LuffSailFrame(out var forePoint, out var mastAxis);
                        var head = FishermansFlyingSailFrameGeometry.UpperHead(
                            neutralHead,
                            clothTransform.TransformPoint(Corners[1]),
                            clothTransform.TransformPoint(Bones[0].localPosition),
                            mastAxis,
                            Sail.currentUnroll
                        );
                        var localHead = clothTransform.InverseTransformPoint(head);
                        var normal = Vector3
                            .Cross(mastAxis, rigging.AftReference - forePoint)
                            .normalized;
                        var clew = Bones[3].localPosition;
                        var bow = FishermansFlyingSailBillow.SupportBow(
                            clew,
                            localHead,
                            clothTransform.InverseTransformDirection(normal),
                            clothTransform.InverseTransformDirection(Vector3.down),
                            -Corners[0].z,
                            clothLoad
                        );
                        var tack = Bones[2].localPosition;
                        bool fitted = FishermansFlyingSailTension.Fit(
                            clew,
                            localHead,
                            tack,
                            bow * FishermansFlyingSailBillow.Deployment(Sail.currentUnroll),
                            (Corners[1] - Corners[3]).magnitude
                                * FishermansFlyingSailMastInstallationGeometry.HoistScale(
                                    Sail.currentUnroll
                                ),
                            (clew - tack).magnitude,
                            FishermansFlyingSailBillow.Deployment(Sail.currentUnroll),
                            leechPoints
                        );
                        if (!fitted && !tensionWarning && state == 2)
                        {
                            Plugin.Log.LogWarning(
                                "FishermansFlyingSail corner span exceeds available foot/leech lengths; check sail fit."
                            );
                            tensionWarning = true;
                        }
                        for (int row = 0; row <= FishermansFlyingSailGeometry.Rows; row++)
                            Bones[FishermansFlyingSailGeometry.LeechBone(row)].localPosition =
                                leechPoints[FishermansFlyingSailGeometry.Rows - row];
                        HalyardAttachments[0].localPosition = Vector3.zero;
                        SheetAttachment.localPosition = Vector3.zero;
                        rigging.UpdateHalyard(HalyardAttachments, state == 0);
                        if (state == 0)
                            SheetAttachment.position = rigging.Pair.ForeGuide.position;
                    }
                    UpdateShapeBones(supported);
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
                if (visible)
                {
                    rigging.LuffSailFrame(out var luffPoint, out var axis);
                    SupportLine.Draw(
                        Bones,
                        rigging.Pair.AftGuide.position,
                        rigging.Pair.ForeGuide.position,
                        FishermansFlyingSailFrameGeometry.AftDirection(
                            luffPoint,
                            axis,
                            rigging.AftReference
                        ),
                        state == 0
                    );
                }
                else
                    SupportLine.Hide();
                // WindCloth writes renderer.enabled in Update; select the correct
                // renderer here in LateUpdate so the disabled solver cannot leave
                // stale full-size triangles visible when the sail is struck.
                clothRenderer.enabled = visible && state == 2;
                ReefedRenderer.sharedMaterial = clothRenderer.sharedMaterial;
                FurledColorReference.sharedMaterial = clothRenderer.sharedMaterial;
                ReefedRenderer.enabled = visible && state == 1;
                // Kept only as the native ChangeSailColor/SE reference; never display it.
                FurledColorReference.enabled = false;
                lastRenderState = state;
            }
        }
    }
}
