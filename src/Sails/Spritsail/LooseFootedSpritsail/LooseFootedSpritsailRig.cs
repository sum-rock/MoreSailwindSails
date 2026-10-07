using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail
{
    // Owns deployment, mast alignment and cloth/render lifecycles; serialized fields clone with the prefab.
    [DefaultExecutionOrder(100)]
    internal sealed class LooseFootedSpritsailRig : MonoBehaviour
    {
        public Sail Sail;
        public Transform[] Bones;
        public Vector3[] Corners;
        public Transform SheetAttachment;
        public Transform SpritHoistAttachment;
        public SpritsailSpar Spar;
        public SpritsailObstruction Obstruction;
        public LooseFootedSpritsailLines Lines;
        public float ExposedAreaFraction;
        public SkinnedMeshRenderer ReefedRenderer;
        public MeshRenderer FurledColorReference;
        public Transform MastFrame;
        public Vector3 OriginalHingeAxis;
        public Vector3 OriginalHingeAnchor;
        public bool OriginalAutoAnchor;
        public Transform Shadow;
        public BoxCollider[] PanelCollisionStrips;
        private float camber = 1;
        private Vector3 sheetPull;
        private readonly Vector3[] flexFoot = new Vector3[
            LooseFootedSpritsailGeometry.ShapeColumns + 1
        ];
        private readonly Vector3[] flexLeech = new Vector3[LooseFootedSpritsailGeometry.Rows + 1];
        private bool tensionWarning;
        private readonly Vector3[] leechPoints = new Vector3[LooseFootedSpritsailGeometry.Rows + 1];
        private int lastRenderState = -1;
        private LooseFootedSpritsailRigging rigging;
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

        private void FixedUpdate() => RefreshMastFrame();

        internal void RefreshCloth() => refreshRequested = true;

        internal static void Configure(
            Sail sail,
            LooseFootedSpritsailMeshData data,
            Mesh mesh,
            Mesh shadowMesh
        )
        {
            float width = data.Corners[3].z - data.Corners[2].z;
            var cloth = sail.cloth;
            var renderer = cloth.GetComponent<SkinnedMeshRenderer>();
            var scaleRoot = cloth.transform.parent;
            var reef = sail.GetComponent<ReefEffectAnimUniversal>();
            if (!reef || !reef.overrideAnimator)
                throw new InvalidOperationException(
                    message: "Expected the brig jib's furl animator."
                );
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

            var rig = sail.gameObject.AddComponent<LooseFootedSpritsailRig>();
            rig.Sail = sail;
            rig.Obstruction = sail.gameObject.AddComponent<SpritsailObstruction>();
            rig.Obstruction.StarboardAffected = true;
            rig.Corners = data.Corners;
            var originalHinge = sail.GetComponent<HingeJoint>();
            rig.OriginalHingeAxis = originalHinge.axis;
            rig.OriginalHingeAnchor = originalHinge.anchor;
            rig.OriginalAutoAnchor = originalHinge.autoConfigureConnectedAnchor;
            rig.MastFrame = new GameObject(name: "LooseFootedSpritsail mast pivot frame").transform;
            rig.MastFrame.SetParent(parent: sail.transform, worldPositionStays: false);
            scaleRoot.SetParent(parent: rig.MastFrame, worldPositionStays: false);
            rig.Bones = new Transform[data.BonePositions.Length];
            var poses = new Matrix4x4[rig.Bones.Length];
            for (int i = 0; i < rig.Bones.Length; i++)
            {
                var bone = new GameObject(name: "LooseFootedSpritsail corner " + i).transform;
                bone.SetParent(parent: cloth.transform, worldPositionStays: false);
                bone.localPosition = data.BonePositions[i];
                rig.Bones[i] = bone;
                poses[i] = bone.worldToLocalMatrix * cloth.transform.localToWorldMatrix;
            }
            // RopeEffect.LookAt rotates both endpoint transforms. Never give
            // it a skin bone directly: use independent leaves beneath the bones.
            rig.SpritHoistAttachment = new GameObject(
                name: "LooseFootedSpritsail upper sprit hoist endpoint"
            ).transform;
            rig.SpritHoistAttachment.SetParent(parent: rig.MastFrame, worldPositionStays: false);
            mesh.bindposes = poses;
            // The donor Cloth contains serialized simulation data for a different
            // topology. Recreate only that component on the inactive clone, after
            // saving its physical settings; WindCloth resolves it in Awake later.
            var clothObject = cloth.gameObject;
            float damping = cloth.damping,
                friction = cloth.friction;
            bool gravity = cloth.useGravity;
            cloth.enabled = false;
            UnityEngine.Object.DestroyImmediate(obj: cloth);

            renderer.sharedMesh = mesh;
            renderer.bones = rig.Bones;
            renderer.rootBone = clothObject.transform;
            renderer.quality = SkinQuality.Bone4;
            renderer.localBounds = new Bounds(
                center: mesh.bounds.center,
                size: new Vector3(
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
                    message: "The new Cloth does not match the spritsail vertex count."
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
            rig.SheetAttachment = left.attachment;
            if (!rig.SheetAttachment || right.attachment != rig.SheetAttachment)
                throw new InvalidOperationException(
                    message: "Expected a shared brig jib sheet attachment."
                );
            rig.SheetAttachment.SetParent(parent: rig.Bones[3], worldPositionStays: false);
            rig.SheetAttachment.localPosition = Vector3.zero;
            sail.windcenter.SetParent(parent: scaleRoot, worldPositionStays: false);
            sail.windcenter.localPosition = data.Center;
            // The donor windcenter also carries SailFlapAudio, whose Awake only
            // searches two parents up for Sail. Keep its initial world pose;
            // RefreshAerodynamics supplies the posed center after scaling/hoisting.
            sail.windcenter.SetParent(parent: rig.MastFrame, worldPositionStays: true);
            connections.colChecker.transform.SetParent(
                parent: scaleRoot,
                worldPositionStays: false
            );
            rig.PanelCollisionStrips = ConfigureCollision(
                checker: connections.colChecker,
                width: width,
                corners: data.Corners
            );
            rig.RefreshSparCollision();
            foreach (
                var visual in scaleRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true)
            )
                visual.enabled = false;
            var shadow = sail.GetComponentInChildren<SailShadowCol>(includeInactive: true);
            if (shadow)
            {
                // SailShadowCol.Awake resolves its Sail exactly two parents up.
                // Keep that contract despite the added mast-pivot frame.
                shadow.transform.SetParent(parent: rig.MastFrame, worldPositionStays: false);
                rig.Shadow = shadow.transform;
                shadow.transform.localPosition = Vector3.zero;
                shadow.transform.localRotation = Quaternion.identity;
                shadow.transform.localScale = scaleRoot.localScale;
                shadow.GetComponent<MeshFilter>().sharedMesh = shadowMesh;
                var box = shadow.GetComponent<BoxCollider>();
                if (box)
                {
                    // The new family uses ordinary shadow triggers even before native Awake runs.
                    box.isTrigger = true;
                    box.center = new Vector3(mesh.bounds.center.x, 0, mesh.bounds.center.z);
                    box.size = new Vector3(
                        mesh.bounds.size.x,
                        mesh.bounds.size.y * 2 + 0.1f,
                        mesh.bounds.size.z
                    );
                }
            }
            var reefed = new GameObject(name: "LooseFootedSpritsail reefing cloth");
            reefed.transform.SetParent(parent: scaleRoot, worldPositionStays: false);
            rig.ReefedRenderer = reefed.AddComponent<SkinnedMeshRenderer>();
            rig.ReefedRenderer.sharedMesh = mesh;
            rig.ReefedRenderer.bones = rig.Bones;
            rig.ReefedRenderer.rootBone = cloth.transform;
            rig.ReefedRenderer.quality = SkinQuality.Bone4;
            rig.ReefedRenderer.localBounds = renderer.localBounds;
            rig.ReefedRenderer.sharedMaterials = renderer.sharedMaterials;
            rig.ReefedRenderer.enabled = false;
            var furled = new GameObject(name: "LooseFootedSpritsail furled color reference");
            furled.transform.SetParent(parent: scaleRoot, worldPositionStays: false);
            rig.FurledColorReference = furled.AddComponent<MeshRenderer>();
            rig.FurledColorReference.sharedMaterials = renderer.sharedMaterials;
            rig.FurledColorReference.enabled = false;
            reef.furledSail = rig.FurledColorReference;
            rig.Lines = LooseFootedSpritsailLines.Create(sail: sail, left: left, right: right);
        }

        private static BoxCollider[] ConfigureCollision(
            ShipyardSailColChecker checker,
            float width,
            Vector3[] corners
        )
        {
            checker.startMinAngle = -SpritsailTravel.MaximumAngle;
            checker.startMaxAngle = SpritsailTravel.MaximumAngle;
            checker.colAngleMin = checker.startMinAngle;
            checker.colAngleMax = checker.startMaxAngle;
            // Use narrow inscribed strips rather than the old triangular clew box.
            // Keep the checker outside the animated bones: it measures the fully set sail.
            var root = checker.transform;
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            var old = checker.GetComponentsInChildren<BoxCollider>(includeInactive: true);
            if (old.Length != 1 || old[0].transform.parent != root)
                throw new InvalidOperationException(
                    message: "Unexpected brig jib collision hierarchy."
                );
            var strips = new BoxCollider[LooseFootedSpritsailGeometry.Columns];
            for (int i = 0; i < LooseFootedSpritsailGeometry.Columns; i++)
            {
                var box =
                    i == 0
                        ? old[0]
                        : new GameObject(
                            name: "LooseFootedSpritsail collision strip " + i
                        ).AddComponent<BoxCollider>();
                box.transform.SetParent(parent: root, worldPositionStays: false);
                box.transform.localPosition = Vector3.zero;
                box.transform.localRotation = Quaternion.identity;
                box.transform.localScale = Vector3.one;
                LooseFootedSpritsailMastInstallationGeometry.CollisionStrip(
                    width: width,
                    corners: corners,
                    column: i,
                    center: out var center,
                    size: out var size
                );
                strips[i] = box;
                box.center = center;
                box.size = size;
                box.isTrigger = true;
                var visual = box.GetComponent<MeshRenderer>();
                if (visual)
                    visual.enabled = false;
            }
            // Native Awake initializes reporting on existing direct children only.
            var spar = new GameObject(
                name: "LooseFootedSpritsail deployed sprit"
            ).AddComponent<BoxCollider>();
            spar.transform.SetParent(parent: root, worldPositionStays: false);
            spar.isTrigger = true;
            return strips;
        }

        private void RefreshSparCollision()
        {
            var checker = Sail.GetComponent<SailConnections>().colChecker.transform;
            var scale = Sail.cloth.transform.lossyScale;
            // Native gaff/junk checks use fixed deployed shapes, independent of reefing or billow.
            var pose = SpritsailDeployment.Evaluate(
                corners: ScaledCorners(scale: scale),
                unroll: 1
            );
            var box = checker
                .Find(n: "LooseFootedSpritsail deployed sprit")
                .GetComponent<BoxCollider>();
            var a = Unscale(
                point: SpritsailSpritGeometry.ForwardEnd(pivot: pose.Heel, tip: pose.Tip),
                scale: scale
            );
            var b = Unscale(point: pose.Tip, scale: scale);
            box.transform.localPosition = (a + b) * 0.5f;
            box.transform.localRotation = Quaternion.LookRotation(forward: b - a);
            box.center = Vector3.zero;
            box.size = new Vector3(
                -Corners[0].z * 0.04f * SpritsailSpritGeometry.ThicknessMultiplier,
                -Corners[0].z * 0.04f * SpritsailSpritGeometry.ThicknessMultiplier,
                (b - a).magnitude
            );
        }

        private Bounds DeploymentBounds()
        {
            var scale = Sail.cloth.transform.lossyScale;
            var corners = ScaledCorners(scale: scale);
            var envelope = new Bounds(center: Corners[2], size: Vector3.zero);
            for (int i = 0; i <= 8; i++)
            {
                var pose = SpritsailDeployment.Evaluate(corners: corners, unroll: i / 8f);
                // Include the raised peak and complete gathered skin in culling bounds.
                envelope.Encapsulate(point: Unscale(point: pose.Throat, scale: scale));
                envelope.Encapsulate(point: Unscale(point: pose.Peak, scale: scale));
                envelope.Encapsulate(point: Unscale(point: pose.Tack, scale: scale));
                envelope.Encapsulate(point: Unscale(point: pose.Clew, scale: scale));
                for (int row = 0; row <= LooseFootedSpritsailGeometry.Rows; row++)
                for (int column = 0; column <= LooseFootedSpritsailGeometry.ShapeColumns; column++)
                    envelope.Encapsulate(
                        point: Unscale(
                            point: LooseFootedSpritsailGathering.Point(
                                corners: corners,
                                pose: pose,
                                amount: SpritsailDeployment.Amount(unroll: i / 8f),
                                u: (float)column / LooseFootedSpritsailGeometry.ShapeColumns,
                                v: (float)row / LooseFootedSpritsailGeometry.Rows,
                                camber: camber,
                                normalScale: scale.y / scale.z
                            ),
                            scale: scale
                        )
                    );
            }
            float flexLimit =
                (corners[3] - corners[2]).magnitude * LooseFootedSpritsailFlex.MaximumDisplacement;
            envelope.Expand(amount: Unscale(point: Vector3.one * (2 * flexLimit), scale: scale));
            return envelope;
        }

        private Vector3[] ScaledCorners(Vector3 scale) =>
            new[]
            {
                Vector3.Scale(Corners[0], scale),
                Vector3.Scale(Corners[1], scale),
                Vector3.Scale(Corners[2], scale),
                Vector3.Scale(Corners[3], scale),
            };

        private static bool ValidScale(Vector3 scale) =>
            SpritsailDeployment.Finite(value: scale.x)
            && SpritsailDeployment.Finite(value: scale.y)
            && SpritsailDeployment.Finite(value: scale.z)
            && scale.x > 0
            && scale.y > 0
            && scale.z > 0;

        internal bool HasActiveSupport =>
            enabled && rigging && rigging.Support != null && rigging.Support.Active;

        private static Vector3 Unscale(Vector3 point, Vector3 scale) =>
            new Vector3(point.x / scale.x, point.y / scale.y, point.z / scale.z);

        internal bool RefreshMastFrame()
        {
            if (
                !Sail
                || !MastFrame
                || Bones == null
                || Bones.Length != LooseFootedSpritsailGeometry.BoneCount
            )
                return false;
            var mount = Sail.transform.parent ? Sail.transform.parent.GetComponent<Mast>() : null;
            if (mount != lastMount)
            {
                bool wasBound = boundToMast;
                lastMount = mount;
                rigging = mount ? LooseFootedSpritsailRigging.For(sail: Sail) : null;
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
            if (!rigging || !rigging.Bind(mast: mount))
                return false;

            rigging.LuffSailFrame(
                point: out var forePoint,
                axis: out var foreAxis,
                hingePoint: out var hingePoint
            );
            var scaleRoot = Sail.cloth.transform.parent;
            if (!ValidScale(scale: scaleRoot.localScale))
                return false;
            // Preserve the native saved installation coordinate. Offset the
            // model onto the offset luff line, but hinge around the mast axis.
            // The ring, bolt and luff orbit together under native sheeting.
            var origin = new Vector3(0, 0, Sail.GetCurrentInstallHeight() - mount.mastHeight);
            var nextPivot = mount.transform.InverseTransformPoint(position: hingePoint) - origin;
            var luffPoint = mount.transform.InverseTransformPoint(position: forePoint) - origin;
            var nextAxis = mount
                .transform.InverseTransformDirection(direction: foreAxis)
                .normalized;
            var aftDirection = mount.transform.InverseTransformDirection(
                direction: rigging.AftDirection
            );
            var alignment =
                Quaternion.LookRotation(
                    forward: aftDirection,
                    upwards: Vector3.Cross(aftDirection, nextAxis)
                ) * Quaternion.Inverse(rotation: scaleRoot.localRotation);
            MastFrame.localRotation = alignment;
            MastFrame.localPosition = LooseFootedSpritsailFrameGeometry.ModelOffset(
                pivot: luffPoint,
                alignedHead: alignment
                    * (
                        scaleRoot.localPosition
                        + scaleRoot.localRotation * Vector3.Scale(Corners[0], scaleRoot.localScale)
                    )
            );
            bool changed =
                bindingDirty
                || !boundToMast
                || LooseFootedSpritsailFrameGeometry.PositionChanged(a: nextPivot, b: pivot)
                || LooseFootedSpritsailFrameGeometry.PositionChanged(a: nextAxis, b: pivotAxis)
                || LooseFootedSpritsailFrameGeometry.PositionChanged(
                    a: lastScale,
                    b: scaleRoot.localScale
                )
                || LooseFootedSpritsailFrameGeometry.PositionChanged(
                    a: lastFramePosition,
                    b: MastFrame.localPosition
                )
                || Quaternion.Angle(a: lastFrameRotation, b: MastFrame.localRotation) > 0.05f;
            if (changed)
            {
                var body = Sail.GetComponent<Rigidbody>();
                var hinge = Sail.GetComponent<HingeJoint>();
                if (!boundToMast || GameState.currentShipyard)
                    body.rotation = mount.transform.rotation;
                body.position = hingePoint - body.rotation * nextPivot;
                RefreshCollisionStrips();
                RefreshClothTravel();
                RefreshSparCollision();
                var deploymentBounds = DeploymentBounds();
                // The struck peak rises above the set head; retain the full bundle bounds.
                var clothRenderer = Sail.cloth.GetComponent<SkinnedMeshRenderer>();
                var bounds = clothRenderer.sharedMesh.bounds;
                bounds.Encapsulate(bounds: deploymentBounds);
                bounds.Expand(
                    amount: new Vector3(
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
                    position: hingePoint
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

        private void RefreshCollisionStrips()
        {
            // The entire neutral panel is outside the mast rim. Keep its first
            // strip: clipping a mast radius here would hide real obstructions.
            var boxes = PanelCollisionStrips;
            for (int i = 0; i < LooseFootedSpritsailGeometry.Columns; i++)
            {
                boxes[i].enabled = LooseFootedSpritsailMastInstallationGeometry.CollisionStrip(
                    width: -Corners[0].z,
                    corners: Corners,
                    column: i,
                    center: out var center,
                    size: out var size
                );
                boxes[i].center = center;
                boxes[i].size = size;
            }
        }

        // Only fitting/scaling updates coefficients; tacks move existing bones.
        private void RefreshClothTravel()
        {
            var coefficients = Sail.cloth.coefficients;
            for (int row = 0; row <= LooseFootedSpritsailGeometry.Rows; row++)
            for (int col = 0; col <= LooseFootedSpritsailGeometry.Columns; col++)
            {
                bool pinned =
                    col == 0
                    || (
                        col == LooseFootedSpritsailGeometry.Columns
                        && (row == 0 || row == LooseFootedSpritsailGeometry.Rows)
                    );
                coefficients[row * (LooseFootedSpritsailGeometry.Columns + 1) + col].maxDistance =
                    pinned
                        ? 0
                        : LooseFootedSpritsailBillow.ClothTravel(
                            width: -Corners[0].z,
                            u: (float)col / LooseFootedSpritsailGeometry.Columns,
                            v: (float)row / LooseFootedSpritsailGeometry.Rows
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
            var rotation = Quaternion.AngleAxis(angle: angle, axis: pivotAxis);
            var origin = new Vector3(0, 0, Sail.GetCurrentInstallHeight() - lastMount.mastHeight);
            var modelOffset =
                MastFrame.localPosition + MastFrame.localRotation * scaleRoot.localPosition;
            var position =
                origin
                + LooseFootedSpritsailFrameGeometry.RotateAroundMast(
                    point: modelOffset,
                    pivot: pivot,
                    axis: pivotAxis,
                    degrees: angle
                );
            neutralRotation = MastFrame.localRotation * scaleRoot.localRotation;
            checker.SetPositionAndRotation(
                position: walk.TransformPoint(position: position),
                rotation: walk.rotation * rotation * neutralRotation
            );
            checker.localScale = scaleRoot.localScale;
            return true;
        }

        internal bool RefreshAerodynamics()
        {
            if (!Sail || !Sail.windcenter || Bones == null || Bones.Length < 4)
                return false;
            if (
                !LooseFootedSpritsailAerodynamics.TryFrame(
                    foreHead: Bones[0].position,
                    foreTack: Bones[2].position,
                    aftHead: Bones[1].position,
                    clew: Bones[3].position,
                    frame: out var frame
                )
            )
                return false;
            Sail.windcenter.SetPositionAndRotation(
                position: frame.Center,
                rotation: Quaternion.LookRotation(forward: frame.MastAxis, upwards: frame.Normal)
            );
            return true;
        }

        private void UpdateShapeBones(
            Vector3[] corners,
            SpritsailDeploymentPose pose,
            Vector3 scale,
            int state
        )
        {
            float flow = Sail
                .cloth.transform.InverseTransformDirection(direction: Sail.apparentWind)
                .y;
            float target =
                flow > 0.6f ? 1
                : flow < -0.6f ? -1
                : camber;
            camber = LooseFootedSpritsailBillow.SmoothLoad(
                previous: camber,
                target: target,
                seconds: Time.deltaTime
            );
            if (state != 2)
            {
                float amount = SpritsailDeployment.Amount(unroll: Sail.currentUnroll);
                for (int row = 0; row <= LooseFootedSpritsailGeometry.Rows; row++)
                for (int column = 0; column <= LooseFootedSpritsailGeometry.ShapeColumns; column++)
                    Bones[
                        LooseFootedSpritsailGeometry.ShapeBone(row: row, column: column)
                    ].localPosition = Unscale(
                        point: LooseFootedSpritsailGathering.Point(
                            corners: corners,
                            pose: pose,
                            amount: amount,
                            u: (float)column / LooseFootedSpritsailGeometry.ShapeColumns,
                            v: (float)row / LooseFootedSpritsailGeometry.Rows,
                            camber: camber,
                            normalScale: scale.y / scale.z,
                            obstruction: Obstruction ? Obstruction.Amount : 0
                        ),
                        scale: scale
                    );
                return;
            }
            for (int row = 0; row <= LooseFootedSpritsailGeometry.Rows; row++)
            {
                float v = (float)row / LooseFootedSpritsailGeometry.Rows;
                var fore = Vector3.Lerp(Bones[0].localPosition, Bones[2].localPosition, v);
                var aft = Bones[LooseFootedSpritsailGeometry.LeechBone(row: row)].localPosition;
                for (int column = 0; column < LooseFootedSpritsailGeometry.ShapeColumns; column++)
                {
                    int bone = LooseFootedSpritsailGeometry.ShapeBone(row: row, column: column);
                    if (bone == 0 || bone == 2)
                        continue;
                    float u = (float)column / LooseFootedSpritsailGeometry.ShapeColumns;
                    Bones[bone].localPosition =
                        Vector3.Lerp(fore, aft, u)
                        + Vector3.up
                            * (
                                LooseFootedSpritsailGeometry.RestCamber(
                                    width: -Corners[0].z,
                                    u: u,
                                    v: v,
                                    headReach: (Corners[1].z - Corners[0].z) / -Corners[0].z
                                )
                                * camber
                                * SpritsailObstructionGeometry.Camber(
                                    point: Vector3.Scale(Vector3.Lerp(fore, aft, u), scale),
                                    heel: pose.Heel,
                                    tip: pose.Tip,
                                    width: -Corners[0].z * scale.z,
                                    obstruction: Obstruction ? Obstruction.Amount : 0
                                )
                                * SpritsailDeployment.Amount(unroll: Sail.currentUnroll)
                            );
                }
            }
        }

        private void ApplySheetFlex(Vector3 scale, int state, bool visible)
        {
            if (!visible || state == 0 || !Lines)
            {
                sheetPull = Vector3.zero;
                return;
            }
            var cloth = Sail.cloth.transform;
            var clew = Vector3.Scale(Bones[3].localPosition, scale);
            Vector3 Endpoint(int side)
            {
                var source = Lines.Sources[side];
                return source && source.gameObject.activeInHierarchy
                    ? Vector3.Scale(
                        cloth.InverseTransformPoint(position: source.transform.position),
                        scale
                    )
                    : clew;
            }
            float Load(int side)
            {
                var source = Lines.Sources[side];
                return source && source.gameObject.activeInHierarchy
                    ? LooseFootedSpritsailFlex.Load(
                        paidOut: source.currentRopeLength,
                        routed: source.totalRopeLength
                    )
                    : 0;
            }
            var requested = LooseFootedSpritsailFlex.Pull(
                clew: clew,
                port: Endpoint(side: 0),
                portLoad: Load(side: 0),
                starboard: Endpoint(side: 1),
                starboardLoad: Load(side: 1)
            );
            sheetPull = LooseFootedSpritsailFlex.Smooth(
                previous: sheetPull,
                target: requested,
                seconds: Time.deltaTime
            );
            for (int column = 0; column < flexFoot.Length; column++)
                flexFoot[column] = Vector3.Scale(
                    Bones[
                        LooseFootedSpritsailGeometry.ShapeBone(
                            row: LooseFootedSpritsailGeometry.Rows,
                            column: column
                        )
                    ].localPosition,
                    scale
                );
            for (int row = 0; row < flexLeech.Length; row++)
                flexLeech[row] = Vector3.Scale(
                    Bones[LooseFootedSpritsailGeometry.LeechBone(row: row)].localPosition,
                    scale
                );
            float limit =
                Vector3.Scale(Corners[3] - Corners[2], scale).magnitude
                * LooseFootedSpritsailFlex.MaximumDisplacement
                * SpritsailDeployment.Amount(unroll: Sail.currentUnroll);
            var delta = Unscale(
                point: LooseFootedSpritsailFlex.Fit(
                    requested: sheetPull * limit,
                    foot: flexFoot,
                    leech: flexLeech,
                    limit: limit
                ),
                scale: scale
            );
            for (int row = 0; row <= LooseFootedSpritsailGeometry.Rows; row++)
            for (int column = 0; column <= LooseFootedSpritsailGeometry.ShapeColumns; column++)
            {
                float u = column / (float)LooseFootedSpritsailGeometry.ShapeColumns;
                float v = row / (float)LooseFootedSpritsailGeometry.Rows;
                var rest = Vector3.Lerp(
                    Vector3.Lerp(Corners[0], Corners[2], v),
                    Vector3.Lerp(Corners[1], Corners[3], v),
                    u
                );
                float weight = LooseFootedSpritsailFlex.Weight(
                    point: rest,
                    peak: Corners[1],
                    tack: Corners[2],
                    clew: Corners[3]
                );
                Bones[
                    LooseFootedSpritsailGeometry.ShapeBone(row: row, column: column)
                ].localPosition += delta * weight;
            }
        }

        private void OnDisable()
        {
            sheetPull = Vector3.zero;
            if (Obstruction)
                Obstruction.ResetState();
            ExposedAreaFraction = 0;
            if (Lines)
                Lines.Hide();
            if (Spar)
                Spar.SetVisible(visible: false);
            if (Sail && Sail.cloth)
            {
                Sail.cloth.enabled = false;
                Sail.cloth.GetComponent<SkinnedMeshRenderer>().enabled = false;
            }
            if (ReefedRenderer)
                ReefedRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            if (!Sail || Bones == null || Bones.Length != LooseFootedSpritsailGeometry.BoneCount)
                return;
            bool supported = RefreshMastFrame();
            bool visible = supported && !GameState.currentlyLoading;
            if (Shadow)
            {
                var scaleRoot = Sail.cloth.transform.parent;
                Shadow.localPosition = scaleRoot.localPosition;
                Shadow.localRotation = scaleRoot.localRotation;
                Shadow.localScale = scaleRoot.localScale;
            }
            int state = LooseFootedSpritsailGeometry.RenderState(unroll: Sail.currentUnroll);
            var scale = Sail.cloth.transform.lossyScale;
            if (!ValidScale(scale: scale))
            {
                OnDisable();
                return;
            }
            var corners = ScaledCorners(scale: scale);
            var pose = SpritsailDeployment.Evaluate(corners: corners, unroll: Sail.currentUnroll);
            Bones[0].localPosition = Unscale(point: pose.Throat, scale: scale);
            Bones[1].localPosition = Unscale(point: pose.Peak, scale: scale);
            Bones[2].localPosition = Unscale(point: pose.Tack, scale: scale);
            Bones[3].localPosition = Unscale(point: pose.Clew, scale: scale);
            // At full deployment the existing coupled-edge solver preserves foot/leech budgets.
            // Partial deployment deliberately uses a gathered, non-simulated panel.
            if (state == 2)
            {
                bool fitted = LooseFootedSpritsailTension.Fit(
                    requested: Bones[3].localPosition,
                    head: Bones[1].localPosition,
                    tack: Bones[2].localPosition,
                    bow: Vector3.zero,
                    leechLength: (Corners[1] - Corners[3]).magnitude,
                    footLength: (Corners[2] - Corners[3]).magnitude,
                    points: leechPoints
                );
                if (!fitted && !tensionWarning)
                {
                    Plugin.Log.LogWarning(
                        data: "LooseFootedSpritsail edge fitting failed; using finite fallback."
                    );
                    tensionWarning = true;
                }
                for (int row = 0; row <= LooseFootedSpritsailGeometry.Rows; row++)
                    Bones[LooseFootedSpritsailGeometry.LeechBone(row: row)].localPosition =
                        leechPoints[LooseFootedSpritsailGeometry.Rows - row];
            }
            if (Obstruction)
                Obstruction.UpdateState(
                    boat: supported ? rigging.Support.Boat.transform : null,
                    apparentWind: Sail.apparentWind,
                    valid: visible && state != 0
                );
            UpdateShapeBones(corners: corners, pose: pose, scale: scale, state: state);
            ApplySheetFlex(scale: scale, state: state, visible: visible);
            SheetAttachment.localPosition = Vector3.zero;
            var heel = Sail.cloth.transform.TransformPoint(
                position: Unscale(point: pose.Heel, scale: scale)
            );
            var tip = Sail.cloth.transform.TransformPoint(
                position: Unscale(point: pose.Tip, scale: scale)
            );
            SpritHoistAttachment.position = SpritsailDeployment.PurchasePoint(heel: heel, tip: tip);
            Spar.Pose(
                heel: SpritsailSpritGeometry.ForwardEnd(pivot: heel, tip: tip),
                tip: tip,
                radius: -Corners[0].z * scale.z * SpritsailSpritGeometry.RadiusFraction
            );
            Spar.SetVisible(visible: visible);
            if (supported)
            {
                rigging.UpdateHalyard(attachment: SpritHoistAttachment);
                Spar.Snotter.Pose(
                    mast: rigging.Support.Mast.GetComponent<CapsuleCollider>(),
                    heel: heel,
                    tip: tip,
                    sparRadius: -Corners[0].z
                        * scale.z
                        * SpritsailSpritGeometry.RadiusFraction
                        * SpritsailSpritGeometry.ThicknessMultiplier,
                    guide: rigging.Support.Guide.position,
                    fallbackDirection: Sail.cloth.transform.TransformDirection(
                        direction: Vector3.up
                    ),
                    mountingDirection: rigging.Support.Boat.transform.right,
                    mastRadius: rigging.SocketRadius,
                    visible: visible
                );
            }
            if (visible)
                Lines.Draw(
                    bones: Bones,
                    tip: tip,
                    mast: rigging.Support.Mast.GetComponent<CapsuleCollider>(),
                    aft: Sail.cloth.transform.TransformDirection(direction: Vector3.up),
                    struck: state == 0
                );
            else
                Lines.Hide();
            float fullArea = SpritsailDeployment.Area(
                throat: corners[0],
                peak: corners[1],
                tack: corners[2],
                clew: corners[3]
            );
            float area = SpritsailDeployment.ExposedArea(pose: pose);
            ExposedAreaFraction = visible && state != 0 ? Mathf.Clamp01(value: area / fullArea) : 0;
            RefreshAerodynamics();
            if (refreshRequested || state != lastRenderState)
            {
                Sail.cloth.enabled = false;
                Sail.cloth.ClearTransformMotion();
                refreshRequested = false;
            }
            Sail.cloth.enabled = visible && state == 2;
            var renderer = Sail.cloth.GetComponent<SkinnedMeshRenderer>();
            renderer.enabled = visible && state == 2;
            ReefedRenderer.sharedMaterial = renderer.sharedMaterial;
            FurledColorReference.sharedMaterial = renderer.sharedMaterial;
            ReefedRenderer.enabled = visible && state == 1;
            Spar.Furled.Pose(
                heel: heel,
                tip: tip,
                radial: Sail.cloth.transform.TransformDirection(direction: Vector3.up),
                heelGap: supported ? rigging.SocketGap : 0,
                cloth: renderer.sharedMaterial,
                visible: visible && state == 0
            );
            FurledColorReference.enabled = false;
            lastRenderState = state;
        }
    }
}
