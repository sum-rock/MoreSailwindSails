using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Owns each live sail's fitted mount mesh; follows existing bones without modifying Cloth.
    internal sealed class SpritsailMount : MonoBehaviour
    {
        public Sail Sail;
        public Transform[] LuffBones;
        private Mesh mesh;
        private Vector3[] vertices;
        private Vector3[] normals;
        private Vector3[] luff;
        private Vector3[] previousLuff;
        private int[] backVertices;
        private readonly float[] radii = new float[7];
        private readonly float[] previousRadii = new float[7];
        private readonly Material[] materials = new Material[4];
        private CapsuleCollider surfaceMast;
        private SpritsailMastSurface surface;
        private Vector3 previousOrigin;
        private Vector3 previousAxis;
        private bool fitted;
        private bool materialWarning;
        private const int Directions = 16;

        internal static SpritsailMount Create(Sail sail, Transform[] luffBones)
        {
            var root = new GameObject(name: "Spritsail authored luff mount");
            root.transform.SetParent(parent: sail.transform, worldPositionStays: false);
            var mount = root.AddComponent<SpritsailMount>();
            mount.Sail = sail;
            mount.LuffBones = luffBones;
            return mount;
        }

        internal void Draw(CapsuleCollider mast, float fallbackRadius)
        {
            if (!isActiveAndEnabled || !mast || !Sail || !Sail.cloth || !ResolveMaterials())
                return;
            EnsureMesh();
            var cloth = Sail.cloth.transform;
            // Work in a rigid metre-sized frame: boat motion needs only a draw matrix,
            // while resizing and reefing update the fitted mesh independently of scale.
            var frame = Matrix4x4.TRS(pos: cloth.position, q: cloth.rotation, s: Vector3.one);
            var inverse = frame.inverse;
            var localAxis =
                mast.direction == 0 ? Vector3.right
                : mast.direction == 1 ? Vector3.up
                : Vector3.forward;
            var worldAxis = mast.transform.TransformDirection(direction: localAxis).normalized;
            var axis = inverse.MultiplyVector(vector: worldAxis).normalized;
            var origin = inverse.MultiplyPoint3x4(
                point: mast.transform.TransformPoint(position: mast.center)
            );
            bool changed =
                !fitted
                || (origin - previousOrigin).sqrMagnitude > 0.00000001f
                || (axis - previousAxis).sqrMagnitude > 0.00000001f;
            for (int row = 0; row < LuffBones.Length; row++)
            {
                luff[row] = inverse.MultiplyPoint3x4(point: LuffBones[row].position);
                changed |= (luff[row] - previousLuff[row]).sqrMagnitude > 0.00000001f;
            }
            if (surfaceMast != mast)
            {
                surfaceMast = mast;
                surface = new SpritsailMastSurface(
                    mast: mast.GetComponent<Mast>(),
                    sampleCount: 7 * Directions
                );
                changed = true;
            }
            var right = Vector3.Cross(lhs: worldAxis, rhs: cloth.forward).normalized;
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(lhs: worldAxis, rhs: cloth.up).normalized;
            var forward = Vector3.Cross(lhs: right, rhs: worldAxis).normalized;
            for (int eye = 0; eye < 7; eye++)
            {
                var eyelet = frame.MultiplyPoint3x4(
                    point: SpritsailMountGeometry.Eyelet(
                        points: luff,
                        sourceX: SpritsailMountAsset.EyeletX[eye]
                    )
                );
                var worldOrigin = mast.transform.TransformPoint(position: mast.center);
                var center =
                    worldOrigin
                    + worldAxis * Vector3.Dot(lhs: eyelet - worldOrigin, rhs: worldAxis);
                float radius = 0;
                for (int direction = 0; direction < Directions; direction++)
                {
                    float angle = direction * 2 * Mathf.PI / Directions;
                    radius = Mathf.Max(
                        a: radius,
                        b: surface.Radius(
                            center: center,
                            direction: right * Mathf.Cos(f: angle) + forward * Mathf.Sin(f: angle),
                            fallback: fallbackRadius,
                            sampleIndex: eye * Directions + direction
                        )
                    );
                }
                if (!SpritsailDeployment.Finite(value: radius) || radius <= 0)
                    return;
                radii[eye] = radius;
                changed |= Mathf.Abs(f: radius - previousRadii[eye]) > 0.0001f;
            }
            if (changed)
            {
                SpritsailMountGeometry.Fit(
                    luff: luff,
                    mastOrigin: origin,
                    mastAxis: axis,
                    radii: radii,
                    vertices: vertices,
                    normals: normals
                );
                int count = SpritsailMountAsset.Positions.Length;
                for (int i = 0; i < backVertices.Length; i++)
                {
                    vertices[count + i] = vertices[backVertices[i]];
                    normals[count + i] = -normals[backVertices[i]];
                }
                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                System.Array.Copy(
                    sourceArray: luff,
                    destinationArray: previousLuff,
                    length: luff.Length
                );
                System.Array.Copy(
                    sourceArray: radii,
                    destinationArray: previousRadii,
                    length: radii.Length
                );
                previousOrigin = origin;
                previousAxis = axis;
                fitted = true;
            }
            for (int material = 0; material < materials.Length; material++)
                Graphics.DrawMesh(
                    mesh: mesh,
                    matrix: frame,
                    material: materials[material],
                    layer: Sail.gameObject.layer,
                    camera: null,
                    submeshIndex: material,
                    properties: null,
                    castShadows: true,
                    receiveShadows: true,
                    useLightProbes: true
                );
        }

        private void EnsureMesh()
        {
            if (mesh)
                return;
            int count = SpritsailMountAsset.Positions.Length;
            var back = new List<int>();
            var map = new int[count];
            for (int i = 0; i < count; i++)
                if (SpritsailMountAsset.Parts[i] == 0)
                {
                    map[i] = count + back.Count;
                    back.Add(item: i);
                }
            backVertices = back.ToArray();
            vertices = new Vector3[count + back.Count];
            normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            System.Array.Copy(
                sourceArray: SpritsailMountAsset.UV,
                destinationArray: uv,
                length: count
            );
            for (int i = 0; i < back.Count; i++)
                uv[count + i] = uv[back[i]];
            mesh = new Mesh { name = "Spritsail fitted luff mount" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.subMeshCount = 4;
            for (int material = 0; material < 4; material++)
            {
                var indices = new List<int>(collection: SpritsailMountAsset.Triangles[material]);
                if (material == 0)
                    for (int i = 0; i < SpritsailMountAsset.Triangles[0].Length; i += 3)
                    {
                        indices.Add(item: map[SpritsailMountAsset.Triangles[0][i + 2]]);
                        indices.Add(item: map[SpritsailMountAsset.Triangles[0][i + 1]]);
                        indices.Add(item: map[SpritsailMountAsset.Triangles[0][i]]);
                    }
                mesh.SetTriangles(triangles: indices.ToArray(), submesh: material);
            }
            luff = new Vector3[LuffBones.Length];
            previousLuff = new Vector3[LuffBones.Length];
        }

        private bool ResolveMaterials()
        {
            materials[0] = Sail.cloth.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
            materials[2] = materials[0];
            if (!materials[1])
            {
                var directory = PrefabsDirectory.instance;
                var donor =
                    directory && directory.directory != null && directory.directory.Length > 103
                        ? directory.directory[103]
                        : null;
                if (donor)
                    foreach (
                        var renderer in donor.GetComponentsInChildren<MeshRenderer>(
                            includeInactive: true
                        )
                    )
                    foreach (var material in renderer.sharedMaterials)
                        if (material && material.name == "metal gold")
                            materials[1] = material;
            }
            if (!materials[3])
            {
                var directory = RefsDirectory.instance;
                var rope =
                    directory && directory.clothRopePrefab
                        ? directory.clothRopePrefab.GetComponent<ClothRope>()
                        : null;
                if (rope && rope.skinned)
                    materials[3] = rope.skinned.sharedMaterial;
            }
            bool ready = materials[0] && materials[1] && materials[3];
            if (!ready && !materialWarning)
            {
                Plugin.Log.LogWarning(
                    data: "Spritsail mount native cloth, gold mug 103 or 3D rope material unavailable; retrying."
                );
                materialWarning = true;
            }
            return ready;
        }

        private void OnDestroy()
        {
            if (mesh)
                Destroy(obj: mesh);
        }
    }
}
