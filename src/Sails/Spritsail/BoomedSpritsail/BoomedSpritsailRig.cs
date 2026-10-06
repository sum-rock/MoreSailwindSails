using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Owns deployment, mast alignment and cloth/render lifecycles; serialized fields clone with the prefab.
    [DefaultExecutionOrder(100)]
    internal sealed class BoomedSpritsailRig : MonoBehaviour
    {
        public Sail Sail;
        public Transform[] Bones;
        public Vector3[] Corners;
        public Transform SheetAttachment;
        public Transform SpritHoistAttachment;
        public SpritsailSpar Spar;
        public BoomedSpritsailBoom Boom;
        public SpritsailObstruction Obstruction;
        public BoomedSpritsailLines Lines;
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
        private int lastRenderState = -1;
        private BoomedSpritsailRigging rigging;
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
            BoomedSpritsailMeshData data,
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
                throw new InvalidOperationException(message: "Expected the gaff's furl animator.");
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

            var rig = sail.gameObject.AddComponent<BoomedSpritsailRig>();
            rig.Sail = sail;
            rig.Obstruction = sail.gameObject.AddComponent<SpritsailObstruction>();
            rig.Obstruction.StarboardAffected = true;
            rig.Corners = data.Corners;
            var originalHinge = sail.GetComponent<HingeJoint>();
            rig.OriginalHingeAxis = originalHinge.axis;
            rig.OriginalHingeAnchor = originalHinge.anchor;
            rig.OriginalAutoAnchor = originalHinge.autoConfigureConnectedAnchor;
            rig.MastFrame = new GameObject(name: "BoomedSpritsail mast pivot frame").transform;
            rig.MastFrame.SetParent(parent: sail.transform, worldPositionStays: false);
            scaleRoot.SetParent(parent: rig.MastFrame, worldPositionStays: false);
            rig.Bones = new Transform[data.BonePositions.Length];
            var poses = new Matrix4x4[rig.Bones.Length];
            for (int i = 0; i < rig.Bones.Length; i++)
            {
                var bone = new GameObject(name: "BoomedSpritsail corner " + i).transform;
                bone.SetParent(parent: cloth.transform, worldPositionStays: false);
                bone.localPosition = data.BonePositions[i];
                rig.Bones[i] = bone;
                poses[i] = bone.worldToLocalMatrix * cloth.transform.localToWorldMatrix;
            }
            // RopeEffect.LookAt rotates both endpoint transforms. Never give
            // it a skin bone directly: use independent leaves beneath the bones.
            rig.SpritHoistAttachment = new GameObject(
                name: "BoomedSpritsail upper sprit hoist endpoint"
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
                // Retain the native gaff's serialized wind response.
                wind.minClothDamping = 0.08f;
                wind.maxClothDamping = 0.45f;
            }

            var connections = sail.GetComponent<SailConnections>();
            var deployment = connections.reefController as RopeControllerSailReef;
            if (!deployment)
                throw new InvalidOperationException(
                    message: "Expected the gaff's native reef controller."
                );
            // Paying out lowers the spars and deploys the panel; hauling in lifts them to reef.
            // Retain native weight/wind resistance on the slower hauling direction.
            deployment.reverseReefing = false;
            var sheet = connections.angleControllerMid as RopeControllerSailAngle;
            var sheetRoute = connections.midRopeAttachment
                ? connections.midRopeAttachment.GetComponent<RopeEffect>()
                : null;
            if (
                !sheet
                || !sheetRoute
                || connections.angleControllerLeft
                || connections.angleControllerRight
            )
                throw new InvalidOperationException(
                    message: "Expected gaff 15's single mid sheet and native route."
                );
            // A fresh endpoint has no donor topping-lift behaviour or skin children.
            rig.SheetAttachment = new GameObject(
                name: "BoomedSpritsail boom sheet endpoint"
            ).transform;
            sheetRoute.attachment = rig.SheetAttachment;
            sheet.limitBoth = true;
            sheet.limitBothClosestAngle = 0;
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
            var reefed = new GameObject(name: "BoomedSpritsail reefing cloth");
            reefed.transform.SetParent(parent: scaleRoot, worldPositionStays: false);
            rig.ReefedRenderer = reefed.AddComponent<SkinnedMeshRenderer>();
            rig.ReefedRenderer.sharedMesh = mesh;
            rig.ReefedRenderer.bones = rig.Bones;
            rig.ReefedRenderer.rootBone = cloth.transform;
            rig.ReefedRenderer.quality = SkinQuality.Bone4;
            rig.ReefedRenderer.localBounds = renderer.localBounds;
            rig.ReefedRenderer.sharedMaterials = renderer.sharedMaterials;
            rig.ReefedRenderer.enabled = false;
            var furled = new GameObject(name: "BoomedSpritsail furled color reference");
            furled.transform.SetParent(parent: scaleRoot, worldPositionStays: false);
            rig.FurledColorReference = furled.AddComponent<MeshRenderer>();
            rig.FurledColorReference.sharedMaterials = renderer.sharedMaterials;
            rig.FurledColorReference.enabled = false;
            reef.furledSail = rig.FurledColorReference;
            rig.Lines = BoomedSpritsailLines.Create(sail: sail);
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
            // Native Awake initializes every direct child and unconditionally reads its Collider.
            // Remove entire donor shapes, not just their components: empty children abort Awake.
            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(obj: root.GetChild(index: i).gameObject);
            var strips = new BoxCollider[BoomedSpritsailGeometry.Columns];
            for (int i = 0; i < BoomedSpritsailGeometry.Columns; i++)
            {
                var box = new GameObject(
                    name: "BoomedSpritsail collision strip " + i
                ).AddComponent<BoxCollider>();
                box.transform.SetParent(parent: root, worldPositionStays: false);
                box.transform.localPosition = Vector3.zero;
                box.transform.localRotation = Quaternion.identity;
                box.transform.localScale = Vector3.one;
                BoomedSpritsailMastInstallationGeometry.CollisionStrip(
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
            // All shapes must exist before native Awake adds sub-checkers, tags and rigidbodies.
            CreateSparCollider(parent: root, name: "BoomedSpritsail deployed sprit");
            CreateSparCollider(parent: root, name: "BoomedSpritsail deployed boom");
            return strips;
        }

        private static void CreateSparCollider(Transform parent, string name)
        {
            var box = new GameObject(name: name).AddComponent<BoxCollider>();
            box.transform.SetParent(parent: parent, worldPositionStays: false);
            box.isTrigger = true;
        }

        private void RefreshSparCollision()
        {
            var checker = Sail.GetComponent<SailConnections>().colChecker.transform;
            var scale = Sail.cloth.transform.lossyScale;
            // Native gaff/junk checks use fixed deployed shapes, independent of reefing or billow.
            var pose = BoomedSpritsailDeployment.Evaluate(
                corners: ScaledCorners(scale: scale),
                unroll: 1
            );
            var box = checker.Find(n: "BoomedSpritsail deployed sprit").GetComponent<BoxCollider>();
            var a = Unscale(point: pose.Heel, scale: scale);
            var b = Unscale(point: pose.Tip, scale: scale);
            box.transform.localPosition = (a + b) * 0.5f;
            box.transform.localRotation = Quaternion.LookRotation(forward: b - a);
            box.center = Vector3.zero;
            box.size = new Vector3(
                -Corners[0].z * 0.04f * SpritsailSpritGeometry.ThicknessMultiplier,
                -Corners[0].z * 0.04f * SpritsailSpritGeometry.ThicknessMultiplier,
                (b - a).magnitude
            );
            var boomBox = checker
                .Find(n: "BoomedSpritsail deployed boom")
                .GetComponent<BoxCollider>();
            a = Unscale(point: pose.Tack, scale: scale);
            b = Unscale(point: pose.Clew, scale: scale);
            boomBox.transform.localPosition = (a + b) * 0.5f;
            boomBox.transform.localRotation = Quaternion.LookRotation(forward: b - a);
            boomBox.center = Vector3.zero;
            boomBox.size = new Vector3(
                -Corners[0].z * 0.04f,
                -Corners[0].z * 0.04f,
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
                var pose = BoomedSpritsailDeployment.Evaluate(corners: corners, unroll: i / 8f);
                // Include the raised peak and complete gathered skin in culling bounds.
                envelope.Encapsulate(point: Unscale(point: pose.Throat, scale: scale));
                envelope.Encapsulate(point: Unscale(point: pose.Peak, scale: scale));
                envelope.Encapsulate(point: Unscale(point: pose.Tack, scale: scale));
                envelope.Encapsulate(point: Unscale(point: pose.Clew, scale: scale));
                for (int row = 0; row <= BoomedSpritsailGeometry.Rows; row++)
                for (int column = 0; column <= BoomedSpritsailGeometry.ShapeColumns; column++)
                    envelope.Encapsulate(
                        point: Unscale(
                            point: BoomedSpritsailGathering.Point(
                                corners: corners,
                                pose: pose,
                                amount: SpritsailDeployment.Amount(unroll: i / 8f),
                                u: (float)column / BoomedSpritsailGeometry.ShapeColumns,
                                v: (float)row / BoomedSpritsailGeometry.Rows,
                                camber: camber,
                                normalScale: scale.y / scale.z
                            ),
                            scale: scale
                        )
                    );
            }
            envelope.Expand(amount: -Corners[0].z * 0.1f);
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
                || Bones.Length != BoomedSpritsailGeometry.BoneCount
            )
                return false;
            var mount = Sail.transform.parent ? Sail.transform.parent.GetComponent<Mast>() : null;
            if (mount != lastMount)
            {
                bool wasBound = boundToMast;
                lastMount = mount;
                rigging = mount ? BoomedSpritsailRigging.For(sail: Sail) : null;
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

            rigging.LuffSailFrame(point: out var forePoint, axis: out var foreAxis);
            var scaleRoot = Sail.cloth.transform.parent;
            if (!ValidScale(scale: scaleRoot.localScale))
                return false;
            // Preserve the native saved installation coordinate. Offset the
            // model and hinge together onto the fixed offset luff line. Sheet
            // rotation leaves the entire pinned luff stationary.
            var origin = new Vector3(0, 0, Sail.GetCurrentInstallHeight() - mount.mastHeight);
            var nextPivot = mount.transform.InverseTransformPoint(position: forePoint) - origin;
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
            MastFrame.localPosition = BoomedSpritsailFrameGeometry.ModelOffset(
                pivot: nextPivot,
                alignedHead: alignment
                    * (
                        scaleRoot.localPosition
                        + scaleRoot.localRotation * Vector3.Scale(Corners[0], scaleRoot.localScale)
                    )
            );
            bool changed =
                bindingDirty
                || !boundToMast
                || BoomedSpritsailFrameGeometry.PositionChanged(a: nextPivot, b: pivot)
                || BoomedSpritsailFrameGeometry.PositionChanged(a: nextAxis, b: pivotAxis)
                || BoomedSpritsailFrameGeometry.PositionChanged(
                    a: lastScale,
                    b: scaleRoot.localScale
                )
                || BoomedSpritsailFrameGeometry.PositionChanged(
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
                body.position = forePoint - body.rotation * nextPivot;
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
                    position: forePoint
                );
                if (!boundToMast)
                    Sail.GetComponent<SailConnections>().angleControllerMid.UpdateSailAttachment();
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
            for (int i = 0; i < BoomedSpritsailGeometry.Columns; i++)
            {
                boxes[i].enabled = BoomedSpritsailMastInstallationGeometry.CollisionStrip(
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
            for (int row = 0; row <= BoomedSpritsailGeometry.Rows; row++)
            for (int col = 0; col <= BoomedSpritsailGeometry.Columns; col++)
            {
                bool pinned =
                    col == 0
                    || row == BoomedSpritsailGeometry.Rows
                    || (
                        col == BoomedSpritsailGeometry.Columns
                        && (row == 0 || row == BoomedSpritsailGeometry.Rows)
                    );
                coefficients[row * (BoomedSpritsailGeometry.Columns + 1) + col].maxDistance = pinned
                    ? 0
                    : BoomedSpritsailBillow.ClothTravel(
                        width: -Corners[0].z,
                        u: (float)col / BoomedSpritsailGeometry.Columns,
                        v: (float)row / BoomedSpritsailGeometry.Rows
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
                + BoomedSpritsailFrameGeometry.RotateAroundMast(
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
                !BoomedSpritsailAerodynamics.TryFrame(
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
            Vector3 scale
        )
        {
            float flow = Sail
                .cloth.transform.InverseTransformDirection(direction: Sail.apparentWind)
                .y;
            float target =
                flow > 0.6f ? 1
                : flow < -0.6f ? -1
                : camber;
            camber = BoomedSpritsailBillow.SmoothLoad(
                previous: camber,
                target: target,
                seconds: Time.deltaTime
            );
            float amount = SpritsailDeployment.Amount(unroll: Sail.currentUnroll);
            for (int row = 0; row <= BoomedSpritsailGeometry.Rows; row++)
            for (int column = 0; column <= BoomedSpritsailGeometry.ShapeColumns; column++)
                Bones[BoomedSpritsailGeometry.ShapeBone(row: row, column: column)].localPosition =
                    Unscale(
                        point: BoomedSpritsailGathering.Point(
                            corners: corners,
                            pose: pose,
                            amount: amount,
                            u: (float)column / BoomedSpritsailGeometry.ShapeColumns,
                            v: (float)row / BoomedSpritsailGeometry.Rows,
                            camber: camber,
                            normalScale: scale.y / scale.z,
                            obstruction: Obstruction ? Obstruction.Amount : 0
                        ),
                        scale: scale
                    );
        }

        private void OnDisable()
        {
            if (Obstruction)
                Obstruction.ResetState();
            ExposedAreaFraction = 0;
            if (Lines)
                Lines.Hide();
            if (Spar)
                Spar.SetVisible(visible: false);
            if (Boom && Boom.Renderer)
                Boom.Renderer.enabled = false;
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
            if (!Sail || Bones == null || Bones.Length != BoomedSpritsailGeometry.BoneCount)
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
            int state = BoomedSpritsailGeometry.RenderState(unroll: Sail.currentUnroll);
            var scale = Sail.cloth.transform.lossyScale;
            if (!ValidScale(scale: scale))
            {
                OnDisable();
                return;
            }
            var corners = ScaledCorners(scale: scale);
            var pose = BoomedSpritsailDeployment.Evaluate(
                corners: corners,
                unroll: Sail.currentUnroll
            );
            Bones[0].localPosition = Unscale(point: pose.Throat, scale: scale);
            Bones[1].localPosition = Unscale(point: pose.Peak, scale: scale);
            Bones[2].localPosition = Unscale(point: pose.Tack, scale: scale);
            Bones[3].localPosition = Unscale(point: pose.Clew, scale: scale);
            if (Obstruction)
                Obstruction.UpdateState(
                    boat: supported ? rigging.Support.Boat.transform : null,
                    apparentWind: Sail.apparentWind,
                    valid: visible && state != 0
                );
            UpdateShapeBones(corners: corners, pose: pose, scale: scale);
            SheetAttachment.localPosition = Vector3.zero;
            var heel = Sail.cloth.transform.TransformPoint(
                position: Unscale(point: pose.Heel, scale: scale)
            );
            var tip = Sail.cloth.transform.TransformPoint(
                position: Unscale(point: pose.Tip, scale: scale)
            );
            SpritHoistAttachment.position = SpritsailDeployment.PurchasePoint(heel: heel, tip: tip);
            Spar.Pose(heel: heel, tip: tip, radius: -Corners[0].z * scale.z * 0.018f);
            Spar.SetVisible(visible: visible);
            Boom.Pose(
                heel: Bones[2].position,
                tip: Bones[3].position,
                radius: -Corners[0].z * scale.z * 0.018f,
                visible: visible
            );
            if (supported)
            {
                rigging.UpdateHalyard(attachment: SpritHoistAttachment);
                Spar.Snotter.Pose(
                    mast: rigging.Support.Mast.GetComponent<CapsuleCollider>(),
                    heel: heel,
                    tip: tip,
                    sparRadius: -Corners[0].z
                        * scale.z
                        * 0.018f
                        * SpritsailSpritGeometry.ThicknessMultiplier,
                    guide: rigging.Support.Guide.position,
                    fallbackDirection: rigging.AftDirection,
                    mastRadius: rigging.SocketRadius,
                    visible: visible
                );
            }
            if (visible)
                Lines.Draw(
                    bones: Bones,
                    tip: tip,
                    mast: rigging.Support.Mast.GetComponent<CapsuleCollider>(),
                    aft: rigging.AftDirection,
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
            // The boomed tack stays down, so the struck bundle spans the complete luff.
            Spar.Furled.Pose(
                heel: Bones[2].position,
                tip: tip,
                radial: supported ? rigging.AftDirection : Vector3.forward,
                heelGap: supported ? rigging.SocketGap : 0,
                cloth: renderer.sharedMaterial,
                visible: visible && state == 0
            );
            FurledColorReference.enabled = false;
            lastRenderState = state;
        }
    }
}
