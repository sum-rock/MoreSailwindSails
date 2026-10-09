using System.Collections.Generic;
using MoreSailwindSails.Visuals;
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
        private readonly SailVisualCache cache = new SailVisualCache();
        private readonly Vector3[] eyelets = new Vector3[7];
        private int[] backVertices;
        private readonly float[] radii = new float[7];
        private readonly Material[] materials = new Material[4];
        private CapsuleCollider surfaceMast;
        private SpritsailMastSurface surface;
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

        internal void Draw(CapsuleCollider mast, float fallbackRadius, long revision)
        {
            if (SpritsailMountProfile.Bypass)
                return;
            long started = SpritsailMountProfile.Begin();
            long checkpoint = started;
            try
            {
                if (!isActiveAndEnabled || !mast || !Sail || !Sail.cloth || !ResolveMaterials())
                    return;
                EnsureMesh();
                var cloth = Sail.cloth.transform;
                // Work in a rigid metre-sized frame: boat motion needs only a draw matrix,
                // while resizing and reefing update the fitted mesh independently of scale.
                var frame = Matrix4x4.TRS(pos: cloth.position, q: cloth.rotation, s: Vector3.one);
                bool rebuild = cache.Needs(revision: revision);
                SpritsailMountProfile.Consumer(part: 0, rebuild: rebuild);
                if (rebuild)
                {
                    bool surfaceChanged =
                        surfaceMast != mast || surface == null || !surface.MeshUnchanged;
                    if (surfaceChanged)
                    {
                        surfaceMast = mast;
                        surface = new SpritsailMastSurface(
                            mast: mast.GetComponent<Mast>(),
                            sampleCount: 7 * Directions
                        );
                    }
                    var localAxis =
                        mast.direction == 0 ? Vector3.right
                        : mast.direction == 1 ? Vector3.up
                        : Vector3.forward;
                    Vector3 origin,
                        axis;
                    Matrix4x4 frameToSurface;
                    if (
                        SpritsailMountFrame.TryBuild(
                            cloth: cloth,
                            mast: mast.transform,
                            surface: surface.Frame,
                            clothToFrame: out var clothToFrame,
                            mastToFrame: out var mastToFrame,
                            frameToSurface: out frameToSurface,
                            mastRotation: out var mastRotation
                        )
                    )
                    {
                        origin = mastToFrame.MultiplyPoint3x4(point: mast.center);
                        axis = (mastRotation * localAxis).normalized;
                        for (int row = 0; row < LuffBones.Length; row++)
                            luff[row] = clothToFrame.MultiplyPoint3x4(
                                point: LuffBones[row].localPosition
                            );
                    }
                    else
                    {
                        // Preserve the established fallback for unrelated transform roots.
                        var inverse = frame.inverse;
                        origin = inverse.MultiplyPoint3x4(
                            point: mast.transform.TransformPoint(position: mast.center)
                        );
                        axis = inverse
                            .MultiplyVector(
                                vector: mast.transform.TransformDirection(direction: localAxis)
                            )
                            .normalized;
                        for (int row = 0; row < LuffBones.Length; row++)
                            luff[row] = inverse.MultiplyPoint3x4(point: LuffBones[row].position);
                        frameToSurface =
                            (
                                surface.Frame
                                    ? surface.Frame.worldToLocalMatrix
                                    : mast.transform.worldToLocalMatrix
                            ) * frame;
                    }
                    SpritsailMountProfile.Mark(
                        stage: SpritsailMountProfile.Setup,
                        checkpoint: ref checkpoint
                    );
                    var right = Vector3.Cross(lhs: axis, rhs: Vector3.forward).normalized;
                    if (right.sqrMagnitude < 0.001f)
                        right = Vector3.Cross(lhs: axis, rhs: Vector3.up).normalized;
                    var forward = Vector3.Cross(lhs: right, rhs: axis).normalized;
                    var sampleRight = frameToSurface.MultiplyVector(vector: right);
                    var sampleForward = frameToSurface.MultiplyVector(vector: forward);
                    for (int eye = 0; eye < 7; eye++)
                    {
                        var eyelet = SpritsailMountGeometry.Eyelet(
                            points: luff,
                            sourceX: SpritsailMountAsset.EyeletX[eye]
                        );
                        var center = origin + axis * Vector3.Dot(lhs: eyelet - origin, rhs: axis);
                        var sampleOrigin = frameToSurface.MultiplyPoint3x4(point: center);
                        SpritsailMountProfile.EnvelopeQuery(hit: false);
                        float radius = 0;
                        for (int direction = 0; direction < Directions; direction++)
                        {
                            float angle = direction * 2 * Mathf.PI / Directions;
                            radius = Mathf.Max(
                                a: radius,
                                b: surface.RadiusLocal(
                                    origin: sampleOrigin,
                                    ray: sampleRight * Mathf.Cos(f: angle)
                                        + sampleForward * Mathf.Sin(f: angle),
                                    fallback: fallbackRadius,
                                    sampleIndex: eye * Directions + direction
                                )
                            );
                        }
                        if (!SpritsailDeployment.Finite(value: radius) || radius <= 0)
                            return;
                        radii[eye] = radius;
                    }
                    SpritsailMountProfile.Mark(
                        stage: SpritsailMountProfile.Surface,
                        checkpoint: ref checkpoint
                    );
                    SpritsailMountProfile.Refit(
                        parts: SpritsailMountGeometry.AllParts,
                        reasons: surfaceChanged ? SpritsailMountFitState.Support : 0
                    );
                    SpritsailMountGeometry.Fit(
                        luff: luff,
                        mastOrigin: origin,
                        mastAxis: axis,
                        radii: radii,
                        vertices: vertices,
                        normals: normals,
                        eyelets: eyelets,
                        parts: SpritsailMountGeometry.AllParts
                    );
                    SpritsailMountProfile.Mark(
                        stage: SpritsailMountProfile.Fit,
                        checkpoint: ref checkpoint
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
                    cache.Commit(revision: revision);
                    SpritsailMountProfile.Mark(
                        stage: SpritsailMountProfile.Upload,
                        checkpoint: ref checkpoint
                    );
                }
                else
                    SpritsailMountProfile.Mark(
                        stage: SpritsailMountProfile.Setup,
                        checkpoint: ref checkpoint
                    );
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
                SpritsailMountProfile.Mark(
                    stage: SpritsailMountProfile.Submit,
                    checkpoint: ref checkpoint
                );
            }
            finally
            {
                SpritsailMountProfile.End(started: started);
            }
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

        private void OnDisable() => cache.Invalidate();

        private void OnDestroy()
        {
            if (mesh)
                Destroy(obj: mesh);
        }
    }
}
