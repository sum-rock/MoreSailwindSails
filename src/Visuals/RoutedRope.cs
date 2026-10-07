using UnityEngine;

namespace MoreSailwindSails.Visuals
{
    // Renders a family-owned route as a native-material tube or the original flat line.
    // Meshes are allocated lazily per live instance, never shared through prefab cloning.
    internal sealed class RoutedRope : MonoBehaviour
    {
        public LineRenderer Line;
        private Mesh mesh;
        private MeshRenderer tube;
        private Vector3[] points;
        private Vector3[] vertices;
        private Vector3[] normals;
        private static bool warnedMissingMaterial;

        internal float Width => Line.startWidth;

        internal static RoutedRope Attach(LineRenderer line)
        {
            var rope = line.gameObject.AddComponent<RoutedRope>();
            rope.Line = line;
            line.enabled = false;
            return rope;
        }

        internal void SetPosition(int index, Vector3 position) =>
            Line.SetPosition(index: index, position: position);

        internal void SetPositions(Vector3[] positions) => Line.SetPositions(positions: positions);

        internal void SetVisible(bool visible)
        {
            if (Line)
                Line.enabled = false;
            if (tube)
                tube.enabled = false;
            if (!visible || !Line || !isActiveAndEnabled)
                return;
            if (!Settings.clothRopes || !EnsureTube())
            {
                Line.enabled = true;
                return;
            }

            int count = Line.positionCount;
            if (count < 2)
                return;
            bool resize = points == null || points.Length != count;
            if (resize)
            {
                points = new Vector3[count];
                vertices = new Vector3[count * RoutedRopeGeometry.Sides];
                normals = new Vector3[vertices.Length];
                mesh.Clear();
            }
            Line.GetPositions(positions: points);
            if (
                !RoutedRopeGeometry.Pose(
                    points: points,
                    diameter: Width,
                    vertices: vertices,
                    normals: normals
                )
            )
                return;

            // Compensate for the complete parent transform, including nonuniform sail scaling.
            // The line's coordinates and diameter are world-space, as in the native fallback.
            var worldToLocal = tube.transform.worldToLocalMatrix;
            var normalToLocal = tube.transform.localToWorldMatrix.transpose;
            var origin = points[0];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = worldToLocal.MultiplyPoint3x4(point: origin + vertices[i]);
                normals[i] = normalToLocal.MultiplyVector(vector: normals[i]).normalized;
            }
            mesh.vertices = vertices;
            mesh.normals = normals;
            if (resize)
                mesh.triangles = RoutedRopeGeometry.Triangles(count: count);
            mesh.RecalculateBounds();
            tube.enabled = true;
        }

        private bool EnsureTube()
        {
            if (tube)
                return true;
            var directory = RefsDirectory.instance;
            var prefab = directory ? directory.clothRopePrefab : null;
            var native = prefab ? prefab.GetComponent<ClothRope>() : null;
            var renderer = native ? native.skinned : null;
            if (!renderer || !renderer.sharedMaterial)
            {
                if (!warnedMissingMaterial)
                {
                    warnedMissingMaterial = true;
                    Plugin.Log.LogWarning(
                        data: "Custom 3D rope material unavailable; retaining flat rope visuals and retrying."
                    );
                }
                return false;
            }
            var root = new GameObject(name: name + " 3D rope");
            root.layer = gameObject.layer;
            root.transform.SetParent(parent: transform, worldPositionStays: false);
            mesh = new Mesh { name = name + " rope surface" };
            mesh.MarkDynamic();
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            tube = root.AddComponent<MeshRenderer>();
            tube.sharedMaterial = renderer.sharedMaterial;
            tube.shadowCastingMode = renderer.shadowCastingMode;
            tube.receiveShadows = renderer.receiveShadows;
            tube.enabled = false;
            return true;
        }

        private void OnDisable() => SetVisible(visible: false);

        private void OnDestroy()
        {
            if (mesh)
                Destroy(obj: mesh);
        }
    }
}
