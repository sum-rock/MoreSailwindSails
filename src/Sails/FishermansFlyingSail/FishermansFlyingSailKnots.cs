using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Sails.FishermansFlyingSail
{
    // Builds template-owned native knot visuals and poses their independent corner leaves.
    internal sealed class FishermansFlyingSailKnots : MonoBehaviour
    {
        public MeshRenderer[] Renderers;

        // Called only while constructing the inactive template. Instances share
        // this generated mesh; the template asset owner handles its lifetime.
        internal static FishermansFlyingSailKnots TryCreate(
            Transform parent,
            float ropeWidth,
            out Mesh mesh
        )
        {
            mesh = null;
            GameObject donor = null,
                root = null;
            Mesh baked = null;
            string diagnostic = "donor=unavailable";
            try
            {
                if (parent.gameObject.activeInHierarchy)
                    throw new InvalidOperationException(
                        "Knot construction requires an inactive template."
                    );
                var directory = RefsDirectory.instance;
                if (!directory || !directory.clothRopeJibSheetPrefab)
                    throw new InvalidOperationException(
                        "Native jib-sheet rope prefab is unavailable."
                    );
                diagnostic = $"donor='{directory.clothRopeJibSheetPrefab.name}'";
                donor = Object.Instantiate(directory.clothRopeJibSheetPrefab, parent, false);
                var native = donor.GetComponent<ClothRope>();
                var renderer = native ? native.skinned : null;
                if (
                    !renderer
                    || !renderer.sharedMesh
                    || renderer.sharedMesh.subMeshCount != 1
                    || renderer.bones.Length < 2
                    || !renderer.sharedMaterial
                )
                    throw new InvalidOperationException("Native jib-sheet renderer is incomplete.");

                var sourceMesh = renderer.sharedMesh;
                var material = renderer.sharedMaterial;
                string shaderName = material.shader ? material.shader.name : null;
                bool hasAssignedTextures = HasAssignedTextures(material: material);
                diagnostic +=
                    $", mesh='{sourceMesh.name}', sourceVertices={sourceMesh.vertexCount}, sourceReadable={sourceMesh.isReadable}, "
                    + $"material='{material.name}', shader='{shaderName}', assignedTextures={hasAssignedTextures}";
                // The shipped mesh is not readable. Bake a private inactive copy;
                // never request vertices from or modify the original shared mesh.
                baked = new Mesh();
                renderer.BakeMesh(baked);
                var vertices = baked.vertices;
                var normals = baked.normals;
                var uv = baked.uv;
                diagnostic +=
                    $", bakedVertices={vertices.Length}, bakedNormals={normals.Length}, bakedUVs={uv.Length}, bakedTangents={baked.tangents.Length}";
                bool useUVs = FishermansFlyingSailKnotChannels.Validate(
                    vertexCount: vertices.Length,
                    normalCount: normals.Length,
                    uvCount: uv.Length,
                    shaderName: shaderName,
                    hasAssignedTextures: hasAssignedTextures
                );
                diagnostic += $", channelPolicy={(useUVs ? "preserve-UVs" : "untextured-UV-less")}";
                var anchor = renderer.transform.InverseTransformPoint(renderer.bones[0].position);
                var direction = renderer.transform.InverseTransformVector(
                    renderer.bones[1].position - renderer.bones[0].position
                );
                if (direction.sqrMagnitude < 1e-8f)
                    throw new InvalidOperationException(
                        "Native jib-sheet attachment has no direction."
                    );
                var rotation = Quaternion.FromToRotation(direction, Vector3.forward);
                var selected = FishermansFlyingSailKnotGeometry.Select(
                    vertices: vertices,
                    triangles: baked.triangles,
                    attachment: anchor,
                    indices: out var triangles
                );
                var knotVertices = new Vector3[selected.Length];
                var knotNormals = FishermansFlyingSailKnotChannels.Remap(
                    channel: normals,
                    selected: selected
                );
                for (int i = 0; i < selected.Length; i++)
                {
                    int source = selected[i];
                    knotVertices[i] = rotation * (vertices[source] - anchor);
                    knotNormals[i] = rotation * knotNormals[i];
                }
                mesh = new Mesh { name = "FishermansFlyingSail native jib knot" };
                mesh.vertices = knotVertices;
                mesh.normals = knotNormals;
                mesh.triangles = triangles;
                mesh.RecalculateBounds();
                // The installed untextured donor has no UVs. Preserve that layout;
                // tangent generation requires UVs and is unnecessary for its material.
                if (useUVs)
                    ApplyUVs(mesh: mesh, uv: uv, selected: selected);

                root = new GameObject("FishermansFlyingSail corner knots");
                root.transform.SetParent(parent, false);
                var knots = root.AddComponent<FishermansFlyingSailKnots>();
                knots.Renderers = new MeshRenderer[4];
                for (int i = 0; i < knots.Renderers.Length; i++)
                {
                    var knot = new GameObject("Corner knot " + i);
                    knot.transform.SetParent(root.transform, false);
                    // This parent is outside SE's fabric scaling hierarchy, just
                    // like the world-space ropes: resizing does not enlarge knots.
                    knot.transform.localScale = Vector3.one * (ropeWidth / 0.05f);
                    knot.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var visual = knot.AddComponent<MeshRenderer>();
                    visual.sharedMaterial = material;
                    visual.shadowCastingMode = renderer.shadowCastingMode;
                    visual.receiveShadows = renderer.receiveShadows;
                    visual.enabled = false;
                    knots.Renderers[i] = visual;
                }
                Plugin.Log.LogInfo($"Flying Sail corner knots created: {diagnostic}.");
                return knots;
            }
            catch (Exception exception)
            {
                if (root)
                    Object.DestroyImmediate(root);
                if (mesh)
                    Object.DestroyImmediate(mesh);
                mesh = null;
                Plugin.Log.LogWarning(
                    $"Flying Sail corner knots unavailable: {exception.Message} ({diagnostic})."
                );
                return null;
            }
            finally
            {
                if (donor)
                    Object.DestroyImmediate(donor);
                if (baked)
                    Object.DestroyImmediate(baked);
            }
        }

        private static bool HasAssignedTextures(Material material)
        {
            foreach (string property in material.GetTexturePropertyNames())
                if (material.GetTexture(name: property))
                    return true;
            return false;
        }

        private static void ApplyUVs(Mesh mesh, Vector2[] uv, int[] selected)
        {
            mesh.uv = FishermansFlyingSailKnotChannels.Remap(channel: uv, selected: selected);
            mesh.RecalculateTangents();
        }

        internal void Draw(Transform[] bones, Vector3 guide, Vector3 controls, Vector3 aftDirection)
        {
            for (int i = 0; i < Renderers.Length; i++)
            {
                var position = bones[i].position;
                var direction =
                    i == 1 ? guide - position
                    : i == 3 ? controls - position
                    : -aftDirection;
                if (direction.sqrMagnitude < 1e-8f)
                    direction = aftDirection;
                var up = bones[0].position - bones[2].position;
                if (Vector3.Cross(direction, up).sqrMagnitude < 1e-8f)
                    up = aftDirection;
                if (Vector3.Cross(direction, up).sqrMagnitude < 1e-8f)
                    up = Vector3.right;
                Renderers[i]
                    .transform.SetPositionAndRotation(
                        position,
                        Quaternion.LookRotation(direction, up)
                    );
                Renderers[i].enabled = true;
            }
        }

        internal void Hide()
        {
            if (Renderers == null)
                return;
            foreach (var renderer in Renderers)
                if (renderer)
                    renderer.enabled = false;
        }

        private void OnDisable() => Hide();
    }
}
