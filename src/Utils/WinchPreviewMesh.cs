// Caches one winch renderer's mesh and materials for camera-only diagnostic drawing.
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoreSailwindSails.Utils
{
    // Owns only baked meshes; shared game meshes, materials and renderers remain untouched.
    internal sealed class WinchPreviewMesh
    {
        private readonly Renderer renderer;
        private readonly Mesh mesh;
        private readonly bool ownsMesh;
        private readonly Material[] materials;
        private readonly MaterialPropertyBlock properties;
        private readonly Matrix4x4 meshToControl;

        internal bool Available =>
            renderer
            && mesh
            && mesh.subMeshCount > 0
            && Array.Exists(materials, material => material);

        internal bool Visible =>
            Available && renderer.enabled && renderer.gameObject.activeInHierarchy;

        internal WinchPreviewMesh(Renderer renderer, Mesh mesh, bool ownsMesh, Transform control)
        {
            this.renderer = renderer;
            this.mesh = mesh;
            this.ownsMesh = ownsMesh;
            materials = renderer.sharedMaterials;
            properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties: properties);
            meshToControl = control.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
        }

        internal void Draw(Camera camera, Matrix4x4? mount, Vector3 scale)
        {
            if (!Available)
                return;
            var matrix = mount.HasValue
                ? WinchMountOverlayGeometry.PreviewMatrix(
                    mount: mount.Value,
                    scale: scale,
                    meshToControl: meshToControl
                )
                : renderer.transform.localToWorldMatrix;
            for (int i = 0; i < materials.Length; i++)
            {
                if (!materials[i])
                    continue;
                Graphics.DrawMesh(
                    mesh: mesh,
                    matrix: matrix,
                    material: materials[i],
                    layer: renderer.gameObject.layer,
                    camera: camera,
                    submeshIndex: Mathf.Min(a: i, b: mesh.subMeshCount - 1),
                    properties: properties,
                    castShadows: ShadowCastingMode.Off,
                    receiveShadows: true
                );
            }
        }

        internal void Dispose()
        {
            if (ownsMesh && mesh)
                UnityEngine.Object.Destroy(obj: mesh);
        }
    }
}
