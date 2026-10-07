using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Owns one rigid fitting group; uploads surfaces only when its physical fit changes.
    internal sealed class SpritsailSnotterMesh : IDisposable
    {
        private readonly Mesh mesh;
        private readonly int[] sourceIndices;
        private readonly Vector3[] vertices;
        private readonly Vector3[] normals;

        internal SpritsailSnotterMesh(bool mounting, Vector3[] profile, Vector3[] profileNormals)
        {
            SpritsailSnotterGeometry.Group(
                mounting: mounting,
                sourceIndices: out sourceIndices,
                uv: out var uv,
                triangles: out var triangles
            );
            vertices = new Vector3[sourceIndices.Length];
            normals = new Vector3[sourceIndices.Length];
            mesh = new Mesh
            {
                name = mounting ? "Spritsail fixed mounting" : "Spritsail rotating sleeve and bolt",
            };
            UploadFit(profile: profile, profileNormals: profileNormals);
            mesh.uv = uv;
            mesh.subMeshCount = triangles.Length;
            for (int material = 0; material < triangles.Length; material++)
                mesh.SetTriangles(triangles: triangles[material], submesh: material);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        internal void Refit(Vector3[] profile, Vector3[] profileNormals)
        {
            UploadFit(profile: profile, profileNormals: profileNormals);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        private void UploadFit(Vector3[] profile, Vector3[] profileNormals)
        {
            for (int i = 0; i < sourceIndices.Length; i++)
            {
                vertices[i] = profile[sourceIndices[i]];
                normals[i] = profileNormals[sourceIndices[i]];
            }
            mesh.vertices = vertices;
            mesh.normals = normals;
        }

        internal void Draw(Matrix4x4 frame, Material[] materials, int layer)
        {
            // The caller submits once from the rig's visible LateUpdate pose.
            // Explicit world matrices preserve metre-sized geometry even below
            // nonuniformly scaled sail parents, without per-vertex compensation.
            for (int material = 0; material < materials.Length; material++)
                Graphics.DrawMesh(
                    mesh: mesh,
                    matrix: frame,
                    material: materials[material],
                    layer: layer,
                    camera: null,
                    submeshIndex: material,
                    properties: null,
                    castShadows: true,
                    receiveShadows: true,
                    useLightProbes: true
                );
        }

        public void Dispose() => UnityEngine.Object.Destroy(obj: mesh);
    }
}
