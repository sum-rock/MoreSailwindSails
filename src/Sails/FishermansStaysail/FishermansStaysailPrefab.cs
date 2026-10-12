using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal static class FishermansStaysailPrefab
    {
        internal const int SourceIndex = 110;
        internal const float TemplateHeadSlope = 20f;

        internal static GameObject Register<TShape>(
            PrefabsDirectory directory,
            GameObject current,
            int prefabIndex,
            string displayName
        )
            where TShape : FishermansStaysailShape
        {
            GameObject container = null;
            Mesh mesh = null;
            Mesh shadowMesh = null;
            try
            {
                if (
                    current
                    && directory.sails.Length > prefabIndex
                    && directory.sails[prefabIndex] == current
                )
                    return current;
                if (directory.sails.Length > prefabIndex && directory.sails[prefabIndex])
                    throw new InvalidOperationException(
                        $"Sail index {prefabIndex} is already occupied; no sail was replaced."
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
                        "The staysail donor requires Cloth with a SkinnedMeshRenderer."
                    );

                // An inactive parent prevents Awake/Start from running on our
                // template. Installed copies retain activeSelf=true and initialize normally.
                container = new GameObject("FishermansStaysail Prefabs");
                container.SetActive(false);
                container.transform.SetParent(directory.transform, false);
                var clone = Object.Instantiate(source, container.transform, false);
                var shape = clone.AddComponent<TShape>();
                var objectPrefix = shape.ObjectPrefix;
                container.name = objectPrefix + " Prefabs";
                var geometry = shape.Create(sourceSail.installHeight, TemplateHeadSlope);
                clone.name = $"{prefabIndex} SAIL {displayName}";
                var sail = clone.GetComponent<Sail>();
                sail.prefabIndex = prefabIndex;
                sail.sailName = displayName;
                sail.category = SailCategory.staysail;
                sail.obsolete = false;
                sail.minAngle = -FishermansStaysailTravel.MaximumAngle;
                sail.maxAngle = FishermansStaysailTravel.MaximumAngle;
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

                mesh = new Mesh { name = objectPrefix + " Trapezoid Cloth" };
                mesh.vertices = geometry.Vertices;
                mesh.triangles = geometry.Triangles;
                mesh.uv = geometry.UV;
                mesh.boneWeights = geometry.Weights;
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                shadowMesh = new Mesh { name = objectPrefix + " Shadow Samples" };
                shadowMesh.vertices = FishermansStaysailShadowGeometry.Samples(
                    vertices: geometry.Vertices
                );
                // SailShadowCol reads only vertices; its separate BoxCollider receives rays.
                shadowMesh.RecalculateBounds();
                FishermansStaysailRig.Configure(sail, geometry, mesh, shadowMesh);
                FishermansStaysailAppearance.Configure(sail);
                clone.SetActive(true);
                sail.SetSailArea();

                string registrationMessage =
                    $"Registered {displayName}: source={SourceIndex}, index={prefabIndex}, "
                    + $"vertices={mesh.vertexCount}, corners=4, pinnedLuff=true, "
                    + $"area={sail.sailArea:F2}.";

                if (directory.sails.Length <= prefabIndex)
                    Array.Resize(ref directory.sails, prefabIndex + 1);
                directory.sails[prefabIndex] = clone;
                container.AddComponent<FishermansStaysailAssets>().Meshes = new[]
                {
                    mesh,
                    shadowMesh,
                };
                Plugin.Log.LogInfo(registrationMessage);
                return clone;
            }
            catch (Exception exception)
            {
                if (container)
                    Object.Destroy(container);
                if (mesh)
                    Object.Destroy(mesh);
                if (shadowMesh)
                    Object.Destroy(shadowMesh);
                Plugin.Log.LogError($"Could not register {displayName}: {exception}");
                return null;
            }
        }

        internal static void AddToShipyard(Shipyard shipyard, GameObject prefab, string displayName)
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
            Plugin.Log.LogInfo($"Added {displayName} to {shipyard.name}.");
        }
    }
}
