// Builds and registers the Flying Sail prefab and adds it to shipyard inventories.
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Sails.FishermansFlyingSail
{
    internal static class FishermansFlyingSailPrefab
    {
        internal const int SourceIndex = 110;

        // Stable across launches: Sailwind stores this index in boat saves.
        internal const int PrefabIndex = 400;
        internal const string DisplayName = "Fisherman's Flying Sail";
        private static GameObject prefab;

        internal static void Register(PrefabsDirectory directory)
        {
            GameObject container = null;
            Mesh mesh = null;
            Mesh shadowMesh = null;
            Mesh knotMesh = null;
            try
            {
                if (
                    prefab
                    && directory.sails.Length > PrefabIndex
                    && directory.sails[PrefabIndex] == prefab
                )
                    return;
                prefab = null;
                if (directory.sails.Length > PrefabIndex && directory.sails[PrefabIndex])
                    throw new InvalidOperationException(
                        $"Sail index {PrefabIndex} is already occupied; no sail was replaced."
                    );

                var source =
                    directory.sails.Length > SourceIndex ? directory.sails[SourceIndex] : null;
                var sourceSail = source ? source.GetComponent<Sail>() : null;
                if (!sourceSail || sourceSail.prefabIndex != SourceIndex)
                    throw new InvalidOperationException(
                        $"Expected a Sail component with prefab index {SourceIndex} on the donor."
                    );
                var sourceRenderer = sourceSail.cloth
                    ? sourceSail.cloth.GetComponent<SkinnedMeshRenderer>()
                    : null;
                if (!sourceRenderer)
                    throw new InvalidOperationException(
                        "The Flying Sail donor requires Cloth with a SkinnedMeshRenderer."
                    );

                // Bake the smaller size into the fabric so it is the new 100%.
                var geometry = FishermansFlyingSailGeometry.Create(sourceSail.installHeight / 3f);

                // An inactive parent prevents Awake/Start from running on our
                // template. Installed copies retain activeSelf=true and initialize normally.
                container = new GameObject("FishermansFlyingSail Prefabs");
                container.SetActive(false);
                container.transform.SetParent(directory.transform, false);
                var clone = Object.Instantiate(source, container.transform, false);
                clone.name = $"{PrefabIndex} SAIL {DisplayName}";
                var sail = clone.GetComponent<Sail>();
                sail.installHeight = geometry.Corners[0].x - geometry.Corners[2].x;
                sail.prefabIndex = PrefabIndex;
                sail.sailName = DisplayName;
                sail.category = SailCategory.other;
                sail.obsolete = false;
                sail.minAngle = -FishermansFlyingSailTravel.MaximumAngle;
                sail.maxAngle = FishermansFlyingSailTravel.MaximumAngle;
                var hinge = sail.GetComponent<HingeJoint>();
                var limits = hinge.limits;
                limits.min = sail.minAngle;
                limits.max = sail.maxAngle;
                hinge.limits = limits;
                hinge.useLimits = true;
                // Changing the menu category must not change the donor's trim dynamics.
                var body = sail.GetComponent<Rigidbody>();
                body.mass = 0.1f;
                body.angularDrag = 1f;

                mesh = new Mesh { name = "FishermansFlyingSail Trapezoid Cloth" };
                mesh.vertices = geometry.Vertices;
                mesh.triangles = geometry.Triangles;
                mesh.uv = geometry.UV;
                mesh.boneWeights = geometry.Weights;
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                shadowMesh = new Mesh { name = "FishermansFlyingSail Shadow Samples" };
                // Native shadow checking casts one ray per vertex per frame.
                // Use a coarse 3x3 sample grid, not all 825 cloth vertices.
                var shadowPoints = new Vector3[9];
                for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                {
                    shadowPoints[row * 3 + col] = geometry.Vertices[
                        row
                            * (FishermansFlyingSailGeometry.Rows / 2)
                            * (FishermansFlyingSailGeometry.Columns + 1)
                            + col * (FishermansFlyingSailGeometry.Columns / 2)
                    ];
                    // Fixed center-plane samples are neutral between tacks.
                    shadowPoints[row * 3 + col].y = 0;
                }
                shadowMesh.vertices = shadowPoints;
                // SailShadowCol reads only vertices; its separate BoxCollider receives rays.
                shadowMesh.RecalculateBounds();
                FishermansFlyingSailRig.Configure(sail, geometry, mesh, shadowMesh);
                var route = sail.GetComponent<FishermansFlyingSailRig>().SupportLine;
                route.Knots = FishermansFlyingSailKnots.TryCreate(
                    route.transform,
                    route.NativeSheets[0].ropeWidth,
                    out knotMesh
                );
                FishermansFlyingSailAppearance.Configure(sail);
                clone.SetActive(true);
                sail.SetSailArea();

                string registrationMessage =
                    $"Registered {DisplayName}: source={SourceIndex}, index={PrefabIndex}, "
                    + $"vertices={mesh.vertexCount}, corners=4, luffWidthRatio=2, edgeSlope=20, mastTies=0.4572m, camberDepth=0.20, "
                    + $"baseWidth={-geometry.Corners[0].z:F2}m, baseLuff={sail.installHeight:F2}m, "
                    + $"area={sail.sailArea:F2}.";

                if (directory.sails.Length <= PrefabIndex)
                    Array.Resize(ref directory.sails, PrefabIndex + 1);
                directory.sails[PrefabIndex] = clone;
                container.AddComponent<FishermansFlyingSailAssets>().Meshes = new[]
                {
                    mesh,
                    shadowMesh,
                    knotMesh,
                };
                prefab = clone;
                Plugin.Log.LogInfo(registrationMessage);
            }
            catch (Exception exception)
            {
                if (container)
                    Object.Destroy(container);
                if (mesh)
                    Object.Destroy(mesh);
                if (shadowMesh)
                    Object.Destroy(shadowMesh);
                if (knotMesh)
                    Object.Destroy(knotMesh);
                Plugin.Log.LogError($"Could not register {DisplayName}: {exception}");
            }
        }

        internal static void AddToShipyard(Shipyard shipyard)
        {
            if (!prefab || !shipyard || shipyard.sailPrefabs == null)
                return;
            // All Sails in All Shipyards may already have included our prefab.
            foreach (var existing in shipyard.sailPrefabs)
                if (existing == prefab)
                    return;

            int index = shipyard.sailPrefabs.Length;
            Array.Resize(ref shipyard.sailPrefabs, index + 1);
            shipyard.sailPrefabs[index] = prefab;
            Plugin.Log.LogInfo($"Added {DisplayName} to {shipyard.name}.");
        }
    }
}
