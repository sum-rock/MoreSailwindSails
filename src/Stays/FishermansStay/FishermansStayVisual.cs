// Builds the decorative baked rope and two metal bands without collision geometry.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay
{
    internal sealed class FishermansStayVisual
    {
        private static Material lanternMetal;
        private readonly Transform owner;
        private readonly Transform[] parts = new Transform[3];

        internal FishermansStayVisual(
            Transform owner,
            Material ropeMaterial,
            int visualLayer,
            List<Mesh> ownedMeshes
        )
        {
            this.owner = owner;
            if (!lanternMetal)
                // Installed native lantern 114's root renderer uses sharedassets1/87.
                lanternMetal = Resources
                    .FindObjectsOfTypeAll<Material>()
                    .FirstOrDefault(material => material.name == "metal2");
            if (!lanternMetal || !ropeMaterial)
                throw new InvalidOperationException(
                    "Missing native lantern metal or stay rope material."
                );
            var rope = CreateMesh(
                data: FishermansStayBakedMesh.CreateRope(),
                name: "FishermansStay rope",
                ownedMeshes: ownedMeshes
            );
            var collar = CreateMesh(
                data: FishermansStayBakedMesh.CreateCollar(),
                name: "FishermansStay collar",
                ownedMeshes: ownedMeshes
            );
            for (int i = 0; i < parts.Length; i++)
            {
                string name =
                    i == 0 ? "Rope"
                    : i == 1 ? "Fore collar"
                    : "Aft collar";
                var mesh = i == 0 ? rope : collar;
                parts[i] = CreatePart(
                    name: name,
                    parent: owner,
                    mesh: mesh,
                    material: i == 0 ? ropeMaterial : lanternMetal,
                    layer: visualLayer
                );
            }
        }

        private static Mesh CreateMesh(
            FishermansStayMeshData data,
            string name,
            List<Mesh> ownedMeshes
        )
        {
            var mesh = new Mesh { name = name };
            ownedMeshes.Add(mesh);
            mesh.vertices = data.Vertices;
            mesh.normals = data.Normals;
            mesh.uv = data.UV;
            mesh.triangles = data.Triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static Transform CreatePart(
            string name,
            Transform parent,
            Mesh mesh,
            Material material,
            int layer
        )
        {
            var node = new GameObject(name: "FishermansStay " + name);
            node.layer = layer;
            node.transform.SetParent(parent: parent, worldPositionStays: false);
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            node.AddComponent<MeshRenderer>().sharedMaterial = material;
            return node.transform;
        }

        internal void Place(FishermansStayReferences references, float span)
        {
            parts[0].localScale = FishermansStayGeometry.RopeScale(span: span);
            PlaceCollar(
                node: parts[1],
                mast: references.Fore,
                point: references.Definition.ForePoint
            );
            PlaceCollar(
                node: parts[2],
                mast: references.Aft,
                point: references.Definition.AftPoint
            );
        }

        private void PlaceCollar(Transform node, Mast mast, Vector3 point)
        {
            var capsule = mast.GetComponent<CapsuleCollider>();
            var axis =
                capsule.direction == 0 ? Vector3.right
                : capsule.direction == 1 ? Vector3.up
                : Vector3.forward;
            var mastToOwner = owner.worldToLocalMatrix * mast.transform.localToWorldMatrix;
            var frame =
                mastToOwner
                * Matrix4x4.Rotate(
                    Quaternion.FromToRotation(fromDirection: Vector3.forward, toDirection: axis)
                );
            node.localPosition = mastToOwner.MultiplyPoint3x4(point);
            node.localRotation = frame.rotation;
            node.localScale = Vector3.Scale(
                frame.lossyScale,
                FishermansStayGeometry.CollarScale(radius: capsule.radius)
            );
        }
    }
}
