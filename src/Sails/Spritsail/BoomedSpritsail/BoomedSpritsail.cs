using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Builds and registers the independent spritsail prefab without modifying donors.
    internal static class BoomedSpritsail
    {
        internal const int SourceIndex = 15;

        private static readonly System.Collections.Generic.Dictionary<int, GameObject> prefabs =
            new System.Collections.Generic.Dictionary<int, GameObject>();

        internal static string DisplayName(int prefabIndex) =>
            prefabIndex == MkA.BoomedSpritsailMkA.PrefabIndex ? MkA.BoomedSpritsailMkA.DisplayName
            : prefabIndex == MkB.BoomedSpritsailMkB.PrefabIndex ? MkB.BoomedSpritsailMkB.DisplayName
            : "Boomed Spritsail";

        internal static void Register(PrefabsDirectory directory)
        {
            RegisterMark(directory: directory, definition: MkA.BoomedSpritsailMkA.Definition);
            RegisterMark(directory: directory, definition: MkB.BoomedSpritsailMkB.Definition);
        }

        private static void RegisterMark(
            PrefabsDirectory directory,
            BoomedSpritsailDefinition definition
        )
        {
            int prefabIndex = definition.PrefabIndex;
            string displayName = definition.DisplayName;
            prefabs.TryGetValue(key: prefabIndex, value: out var prefab);
            GameObject container = null;
            GameObject candidate = null;
            bool ownsMeshes = false;
            Mesh mesh = null;
            Mesh shadowMesh = null;
            Mesh sparMesh = null;
            try
            {
                SpritsailCatalog.ValidateCategory(sails: directory.sails);
                if (
                    prefab
                    && directory.sails.Length > prefabIndex
                    && directory.sails[prefabIndex] == prefab
                )
                    return;
                prefabs.Remove(key: prefabIndex);
                if (directory.sails.Length > prefabIndex && directory.sails[prefabIndex])
                    throw new InvalidOperationException(
                        message: $"Sail index {prefabIndex} is already occupied; no sail was replaced."
                    );

                var source =
                    directory.sails.Length > SourceIndex ? directory.sails[SourceIndex] : null;
                var sourceSail = source ? source.GetComponent<Sail>() : null;
                if (
                    !sourceSail
                    || sourceSail.prefabIndex != SourceIndex
                    || sourceSail.category != SailCategory.gaff
                )
                    throw new InvalidOperationException(
                        message: $"Expected the full gaff at index {SourceIndex}."
                    );
                var sourceRenderer = sourceSail.cloth
                    ? sourceSail.cloth.GetComponent<SkinnedMeshRenderer>()
                    : null;
                if (
                    !sourceRenderer
                    || !sourceRenderer.sharedMesh
                    || !sourceRenderer.sharedMesh.isReadable
                )
                    throw new InvalidOperationException(
                        message: "The full gaff has no readable skinned cloth mesh."
                    );

                var sourceMesh = sourceRenderer.sharedMesh;
                var sizingDonor =
                    directory.sails.Length > 110
                        ? directory.sails[110]?.GetComponent<Sail>()
                        : null;
                if (
                    !sizingDonor
                    || sizingDonor.prefabIndex != 110
                    || sizingDonor.sailName != "brig jib"
                )
                    throw new InvalidOperationException(
                        message: "Expected brig jib 110 for companion dimensions and wind response."
                    );
                // Bake the smaller size into the fabric so it is the new 100%.
                var geometry = BoomedSpritsailGeometry.Create(
                    width: sizingDonor.installHeight / 3f,
                    definition: definition
                );

                // An inactive parent prevents Awake/Start from running on our
                // template. Installed copies retain activeSelf=true and initialize normally.
                container = new GameObject(name: "BoomedSpritsail Prefabs");
                container.SetActive(value: false);
                container.transform.SetParent(
                    parent: directory.transform,
                    worldPositionStays: false
                );
                var clone = Object.Instantiate(
                    original: source,
                    parent: container.transform,
                    worldPositionStays: false
                );
                candidate = clone;
                clone.name = $"{prefabIndex} SAIL {displayName}";
                var sail = clone.GetComponent<Sail>();
                sail.installHeight = geometry.Corners[0].x - geometry.Corners[2].x;
                sail.prefabIndex = prefabIndex;
                sail.sailName = displayName;
                SpritsailCategory.Initialize(
                    sail: sail,
                    upwindEfficiency: sizingDonor.upwindEfficiency
                );
                sail.obsolete = false;
                sail.sailAmplifier = sizingDonor.sailAmplifier;
                sail.minAngle = -SpritsailTravel.MaximumAngle;
                sail.maxAngle = SpritsailTravel.MaximumAngle;
                var hinge = sail.GetComponent<HingeJoint>();
                var limits = hinge.limits;
                limits.min = sail.minAngle;
                limits.max = sail.maxAngle;
                hinge.limits = limits;
                hinge.useLimits = true;
                // Shared handling baseline; independent of category boat mass and propulsion.
                var body = sail.GetComponent<Rigidbody>();
                body.mass = 0.1f;
                body.angularDrag = 1f;

                mesh = new Mesh { name = "BoomedSpritsail Cloth" };
                mesh.vertices = geometry.Vertices;
                mesh.triangles = geometry.Triangles;
                mesh.uv = geometry.UV;
                mesh.boneWeights = geometry.Weights;
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                shadowMesh = new Mesh { name = "BoomedSpritsail Shadow Samples" };
                // Native shadow checking casts one ray per vertex per frame.
                // Use a coarse 3x3 sample grid, not all 825 cloth vertices.
                var shadowPoints = new Vector3[9];
                for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                {
                    shadowPoints[row * 3 + col] = geometry.Vertices[
                        row
                            * (BoomedSpritsailGeometry.Rows / 2)
                            * (BoomedSpritsailGeometry.Columns + 1)
                            + col * (BoomedSpritsailGeometry.Columns / 2)
                    ];
                    // Fixed center-plane samples are neutral between tacks.
                    shadowPoints[row * 3 + col].y = 0;
                }
                shadowMesh.vertices = shadowPoints;
                shadowMesh.triangles = new[]
                {
                    0,
                    3,
                    1,
                    1,
                    3,
                    4,
                    1,
                    4,
                    2,
                    2,
                    4,
                    5,
                    3,
                    6,
                    4,
                    4,
                    6,
                    7,
                    4,
                    7,
                    5,
                    5,
                    7,
                    8,
                };
                shadowMesh.RecalculateBounds();
                BoomedSpritsailRig.Configure(
                    sail: sail,
                    data: geometry,
                    mesh: mesh,
                    shadowMesh: shadowMesh
                );
                sparMesh = SpritsailSpar.CreateMesh();
                clone.GetComponent<BoomedSpritsailRig>().Spar = SpritsailSpar.Create(
                    parent: clone.transform,
                    mesh: sparMesh,
                    directory: directory
                );
                clone.GetComponent<BoomedSpritsailRig>().Boom = BoomedSpritsailBoom.Create(
                    parent: clone.transform,
                    mesh: sparMesh,
                    timber: clone.GetComponent<BoomedSpritsailRig>().Spar.Renderer.sharedMaterial
                );
                BoomedSpritsailAppearance.Configure(sail: sail);
                var renderer = sail.cloth.GetComponent<SkinnedMeshRenderer>();
                clone.SetActive(value: true);
                sail.SetSailArea();

                // Verify that the source still points at its original mesh.
                if (sourceRenderer.sharedMesh != sourceMesh || renderer.sharedMesh == sourceMesh)
                    throw new InvalidOperationException(
                        message: "The Boomed Spritsail must own a separate cloth mesh."
                    );

                string registrationMessage =
                    $"Registered prototype {displayName}: source={SourceIndex}, index={prefabIndex}, "
                    + $"category=Spritsails (6), vertices={mesh.vertexCount}, rigid sprit, mast luff, single boom sheet, coordinated boom/sprit hoist.";

                if (directory.sails.Length <= prefabIndex)
                    Array.Resize(array: ref directory.sails, newSize: prefabIndex + 1);
                container.AddComponent<BoomedSpritsailAssets>().Meshes = new[]
                {
                    mesh,
                    shadowMesh,
                    sparMesh,
                };
                ownsMeshes = true;
                directory.sails[prefabIndex] = clone;
                SpritsailCatalog.Register(sail: sail);
                prefabs[prefabIndex] = clone;
                Plugin.Log.LogInfo(data: registrationMessage);
            }
            catch (Exception exception)
            {
                SpritsailCatalog.Unregister(candidate: candidate);
                if (
                    candidate
                    && directory.sails.Length > prefabIndex
                    && directory.sails[prefabIndex] == candidate
                )
                    directory.sails[prefabIndex] = null;
                prefabs.Remove(key: prefabIndex);
                if (container)
                    Object.Destroy(obj: container);
                if (!ownsMeshes && mesh)
                    Object.Destroy(obj: mesh);
                if (!ownsMeshes && shadowMesh)
                    Object.Destroy(obj: shadowMesh);
                if (!ownsMeshes && sparMesh)
                    Object.Destroy(obj: sparMesh);
                Plugin.Log.LogError(data: $"Could not register {displayName}: {exception}");
            }
        }
    }
}
