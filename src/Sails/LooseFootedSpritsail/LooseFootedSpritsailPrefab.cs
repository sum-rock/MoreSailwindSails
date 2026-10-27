// Builds cut-specific spritsails with native sheet controllers and owned baked meshes.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal static class LooseFootedSpritsailPrefab
    {
        internal const int SourceIndex = 110;
        internal const int FullGaffIndex = 15;
        private static readonly Dictionary<int, LooseFootedSpritsailDefinition> definitions =
            new Dictionary<int, LooseFootedSpritsailDefinition>();

        internal static LooseFootedSpritsailAssetData CreateAsset(int prefabIndex) =>
            definitions[prefabIndex].CreateAsset();

        internal static GameObject Register(
            PrefabsDirectory directory,
            LooseFootedSpritsailDefinition definition,
            GameObject existing
        )
        {
            GameObject container = null;
            LooseFootedSpritsailAssets assets = null;
            try
            {
                if (
                    existing
                    && directory.sails.Length > definition.PrefabIndex
                    && directory.sails[definition.PrefabIndex] == existing
                )
                    return existing;
                if (
                    directory.sails.Length > definition.PrefabIndex
                    && directory.sails[definition.PrefabIndex]
                )
                    throw new InvalidOperationException(
                        $"Sail index {definition.PrefabIndex} is occupied; no sail was replaced."
                    );
                var donor = directory.sails[SourceIndex].GetComponent<Sail>();
                var fullGaff = directory.sails[FullGaffIndex].GetComponent<Sail>();
                if (
                    donor.prefabIndex != SourceIndex
                    || fullGaff.prefabIndex != FullGaffIndex
                    || !donor.cloth
                )
                    throw new InvalidOperationException(
                        "Expected brig jib and Full Gaff native donors."
                    );
                var wood = fullGaff
                    .GetComponentsInChildren<MeshRenderer>(includeInactive: true)
                    .First(r => r.name == "boom_gaff")
                    .sharedMaterial;
                var metal = Resources
                    .FindObjectsOfTypeAll<Material>()
                    .First(m => m.name == "metal2");
                var data = definition.CreateAsset();
                float scale = LooseFootedSpritsailFittingGeometry.BaseScale(
                    data: data,
                    fullGaffHeight: fullGaff.installHeight
                );
                container = new GameObject(name: definition.DisplayName + " Prefabs");
                // Activate the asset owner before making its template container inactive.
                // Unity only sends OnDestroy to components that have been active.
                assets = container.AddComponent<LooseFootedSpritsailAssets>();
                // Sanbuq capture: structure/mast_1 uses dhow_medium_paint. Keep the
                // plate's authored UV islands inside clean mast wood, above the painted band.
                var plateWood = assets.Own(
                    material: new Material(
                        Resources
                            .FindObjectsOfTypeAll<Material>()
                            .First(m => m.name == "dhow_medium_paint")
                    )
                    {
                        name = "Spritsail rotation plate wood",
                        mainTextureScale = new Vector2(x: 0.004f, y: 0.04f),
                        mainTextureOffset = new Vector2(x: 0.945f, y: 0.12f),
                    }
                );
                container.SetActive(value: false);
                container.transform.SetParent(
                    parent: directory.transform,
                    worldPositionStays: false
                );
                var clone = Object.Instantiate(
                    original: donor.gameObject,
                    parent: container.transform,
                    worldPositionStays: false
                );
                clone.name = $"{definition.PrefabIndex} SAIL {definition.DisplayName}";
                clone.transform.localScale = Vector3.one;
                var sail = clone.GetComponent<Sail>();
                sail.prefabIndex = definition.PrefabIndex;
                sail.sailName = definition.DisplayName;
                sail.category = SailCategory.other;
                sail.obsolete = false;
                sail.squareSail = false;
                sail.junkType = false;
                sail.installHeight = fullGaff.installHeight;
                sail.upwindEfficiency = fullGaff.upwindEfficiency;
                sail.currentUnroll = 1;
                var rig = clone.AddComponent<LooseFootedSpritsailRig>();
                rig.Sail = sail;
                var rigging = clone.AddComponent<LooseFootedSpritsailRigging>();
                rigging.Rig = rig;
                var connections = sail.GetComponent<SailConnections>();
                rigging.Left = connections.angleControllerLeft as RopeControllerSailAngleJib;
                rigging.Right = connections.angleControllerRight as RopeControllerSailAngleJib;
                rigging.AngleMaster = clone.GetComponent<JibAngleMaster>();
                if (!rigging.Left || !rigging.Right || !rigging.AngleMaster)
                    throw new InvalidOperationException(
                        "Expected paired native jib sheet controllers."
                    );
                rigging.AngleMaster.sailHinge = clone.GetComponent<HingeJoint>();
                rigging.AngleMaster.enabled = false;
                rigging.Left.gameObject.SetActive(value: false);
                rigging.Right.gameObject.SetActive(value: false);
                rig.BaseScale = scale;
                rig.BaseLuff = fullGaff.installHeight;
                rig.BaseArea = Area(data: data) * scale * scale;
                rig.Data = data;
                rig.ScaleRoot = sail.cloth.transform.parent;
                rig.ScaleRoot.localPosition = Vector3.zero;
                rig.ScaleRoot.localRotation = Quaternion.identity;
                rig.ScaleRoot.localScale = Vector3.one;
                StripDonor(sail: sail);
                var body = clone.GetComponent<Rigidbody>();
                var gaffBody = fullGaff.GetComponent<Rigidbody>();
                body.mass = gaffBody.mass;
                body.angularDrag = gaffBody.angularDrag;
                sail.windcenter = NewNode(name: "Spritsail wind center", parent: sail.transform);
                var clothMaterial = assets.Own(
                    material: new Material(
                        donor.cloth.GetComponent<SkinnedMeshRenderer>().sharedMaterial
                    )
                );
                clothMaterial.name = definition.DisplayName + " Plain Cloth";
                clothMaterial.mainTexture = Texture2D.whiteTexture;
                clothMaterial.color = directory.sailColors[11];
                sail.activeColor = 11;
                rig.TemplateMaterial = clothMaterial;
                rig.Frame = NewNode(name: "Mast fitting frame", parent: sail.transform);
                rig.ClothBone = NewNode(name: "Neutral cloth", parent: rig.Frame);
                rig.Sprit = NewNode(name: "Sprit", parent: rig.Frame);
                rig.SnotterAssembly = NewNode(name: "Snotter and mounting rod", parent: rig.Frame);
                rig.RotationPlate = NewNode(name: "Rotation plate", parent: rig.Frame);
                var clothMesh = CreateMesh(
                    assets: assets,
                    name: "Spritsail cloth",
                    data: data.Sail
                );
                var weights = new BoneWeight[clothMesh.vertexCount];
                for (int i = 0; i < weights.Length; i++)
                    weights[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1 };
                clothMesh.boneWeights = weights;
                clothMesh.bindposes = new[] { Matrix4x4.identity };
                var renderer = sail.cloth.GetComponent<SkinnedMeshRenderer>();
                sail.cloth.enabled = false;
                Object.DestroyImmediate(obj: sail.cloth);
                renderer.sharedMesh = clothMesh;
                renderer.bones = new[] { rig.ClothBone };
                renderer.rootBone = rig.ClothBone;
                renderer.sharedMaterial = clothMaterial;
                renderer.enabled = true;
                renderer.updateWhenOffscreen = true;
                sail.cloth = renderer.gameObject.AddComponent<Cloth>();
                sail.cloth.enabled = false;
                sail.cloth.coefficients = new ClothSkinningCoefficient[clothMesh.vertexCount];
                AddHardware(
                    assets: assets,
                    node: rig.Sprit,
                    data: data.Sprit,
                    wood: wood,
                    metal: metal
                );
                AddHardware(
                    assets: assets,
                    node: rig.SnotterAssembly,
                    data: data.SnotterAssembly,
                    wood: wood,
                    metal: metal
                );
                AddHardware(
                    assets: assets,
                    node: rig.RotationPlate,
                    data: data.RotationPlate,
                    wood: plateWood,
                    metal: metal
                );
                rig.ReefLineAttachment = NewNode(name: "ReefLineAttachment", parent: rig.Sprit);
                rig.ReefLineAttachment.localPosition = data.ReefLineAttachment;
                rig.SheetLineAttachment = NewNode(
                    name: "SheetLineAttachment",
                    parent: rig.ClothBone
                );
                rig.SheetLineAttachment.localPosition = data.SheetLineAttachment;
                ConfigureSheet(controller: rigging.Left, rigging: rigging);
                ConfigureSheet(controller: rigging.Right, rigging: rigging);
                var colorReference = NewNode(
                    name: "Furled color reference",
                    parent: sail.transform
                );
                rig.ColorReference = colorReference.gameObject.AddComponent<MeshRenderer>();
                rig.ColorReference.sharedMaterial = clothMaterial;
                rig.ColorReference.enabled = false;
                sail.GetComponent<ReefEffectAnimUniversal>().furledSail = rig.ColorReference;
                ConfigureCollision(rig: rig);
                definitions[definition.PrefabIndex] = definition;
                clone.SetActive(value: true);
                if (directory.sails.Length <= definition.PrefabIndex)
                    Array.Resize(array: ref directory.sails, newSize: definition.PrefabIndex + 1);
                directory.sails[definition.PrefabIndex] = clone;
                Plugin.Log.LogInfo(
                    $"Registered {definition.DisplayName}: index={definition.PrefabIndex}, base luff={sail.installHeight:F2}m, paired sheets and native propulsion."
                );
                return clone;
            }
            catch (Exception error)
            {
                if (assets)
                    assets.Release();
                if (container)
                    Object.Destroy(obj: container);
                Plugin.Log.LogError($"Could not register {definition.DisplayName}: {error}");
                return null;
            }
        }

        private static float Area(LooseFootedSpritsailAssetData data)
        {
            float area = 0;
            foreach (var triangles in data.Sail.SubmeshTriangles)
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var a = data.Sail.Vertices[triangles[i]];
                    var b = data.Sail.Vertices[triangles[i + 1]];
                    var c = data.Sail.Vertices[triangles[i + 2]];
                    area += Vector3.Cross(lhs: b - a, rhs: c - a).magnitude / 2;
                }
            return area;
        }

        private static void StripDonor(Sail sail)
        {
            foreach (var renderer in sail.GetComponentsInChildren<Renderer>(includeInactive: true))
                renderer.enabled = false;
            foreach (var collider in sail.GetComponentsInChildren<Collider>(includeInactive: true))
                collider.enabled = false;
            foreach (var animator in sail.GetComponentsInChildren<Animator>(includeInactive: true))
                animator.enabled = false;
            foreach (var wind in sail.GetComponentsInChildren<WindCloth>(includeInactive: true))
                wind.enabled = false;
            foreach (
                var sound in sail.GetComponentsInChildren<SailFlapAudio>(includeInactive: true)
            )
                Object.DestroyImmediate(obj: sound);
            foreach (var sound in sail.GetComponentsInChildren<AudioSource>(includeInactive: true))
                sound.enabled = false;
            var reef = sail.GetComponent<ReefEffectAnimUniversal>();
            reef.enabled = false;
            var connections = sail.GetComponent<SailConnections>();
            foreach (
                var controller in new[]
                {
                    connections.reefController,
                    connections.angleControllerMid,
                }
            )
                if (controller)
                    Object.DestroyImmediate(obj: controller.gameObject);
            foreach (
                var attachment in new[]
                {
                    connections.midRopeAttachment,
                    connections.mastReefAttachment,
                    connections.mastReefAttExtension,
                }
            )
                if (attachment)
                    Object.DestroyImmediate(obj: attachment.gameObject);
            connections.reefController =
                connections.angleControllerLeft =
                connections.angleControllerRight =
                connections.angleControllerMid =
                    null;
            connections.midRopeAttachment =
                connections.mastReefAttachment =
                connections.mastReefAttExtension =
                    null;
            if (connections.colChecker)
                Object.DestroyImmediate(obj: connections.colChecker.gameObject);
            var shadow = sail.GetComponentInChildren<SailShadowCol>(includeInactive: true);
            if (shadow)
                shadow.gameObject.SetActive(value: false);
            var body = sail.GetComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
        }

        private static void ConfigureSheet(
            RopeControllerSailAngleJib controller,
            LooseFootedSpritsailRigging rigging
        )
        {
            controller.jibAngleMaster = rigging.AngleMaster;
            controller.currentLength = 1;
            controller.enabled = true;
            var rope = controller.GetComponent<RopeEffect>();
            // RopeEffect rotates its endpoint to draw slack. Give each sheet its own
            // endpoint at the clew so neither changes the other's rope frame.
            rope.attachment = NewNode(
                name: controller.side + " clew tie",
                parent: rigging.Rig.SheetLineAttachment
            );
            rope.sheet = false;
            rope.jibSheet = true;
            rope.enabled = true;
            rope.currentRopeLength = rope.totalRopeLength = 1;
        }

        private static Transform NewNode(string name, Transform parent)
        {
            var node = new GameObject(name: name).transform;
            node.SetParent(parent: parent, worldPositionStays: false);
            return node;
        }

        private static Mesh CreateMesh(
            LooseFootedSpritsailAssets assets,
            string name,
            LooseFootedSpritsailMeshPart data
        )
        {
            var mesh = assets.Own(mesh: new Mesh { name = name });
            mesh.vertices = data.Vertices;
            mesh.normals = data.Normals;
            mesh.uv = data.UV;
            mesh.subMeshCount = data.SubmeshTriangles.Length;
            for (int i = 0; i < data.SubmeshTriangles.Length; i++)
                mesh.SetTriangles(triangles: data.SubmeshTriangles[i], submesh: i);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddHardware(
            LooseFootedSpritsailAssets assets,
            Transform node,
            LooseFootedSpritsailMeshPart data,
            Material wood,
            Material metal
        )
        {
            node.gameObject.AddComponent<MeshFilter>().sharedMesh = CreateMesh(
                assets: assets,
                name: node.name,
                data: data
            );
            node.gameObject.AddComponent<MeshRenderer>().sharedMaterials = data
                .Materials.Select(m => m == LooseFootedSpritsailMaterial.Metal ? metal : wood)
                .ToArray();
        }

        private static void ConfigureCollision(LooseFootedSpritsailRig rig)
        {
            var root = NewNode(name: "Spritsail installation clearance", parent: rig.transform);
            var checker = root.gameObject.AddComponent<ShipyardSailColChecker>();
            checker.startMinAngle = -85;
            checker.startMaxAngle = 85;
            rig.Sail.GetComponent<SailConnections>().colChecker = checker;
            rig.CollisionBoxes = new BoxCollider[
                LooseFootedSpritsailFittingGeometry.CollisionStrips + 1
            ];
            for (int i = 0; i < rig.CollisionBoxes.Length; i++)
            {
                var node = NewNode(name: "Clearance " + i, parent: root);
                var box = node.gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                rig.CollisionBoxes[i] = box;
            }
        }

        internal static void AddToShipyard(Shipyard shipyard, GameObject prefab)
        {
            if (
                !prefab
                || !shipyard
                || shipyard.sailPrefabs == null
                || shipyard.sailPrefabs.Contains(value: prefab)
            )
                return;
            int index = shipyard.sailPrefabs.Length;
            Array.Resize(array: ref shipyard.sailPrefabs, newSize: index + 1);
            shipyard.sailPrefabs[index] = prefab;
        }
    }
}
