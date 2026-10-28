// Owns each instance's deforming mesh and applies the native reef state before sail physics.
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal sealed class LooseFootedSpritsailCloth
    {
        private readonly LooseFootedSpritsailRig rig;
        private readonly SkinnedMeshRenderer renderer;
        private readonly Mesh mesh;
        private readonly Vector3[] localVertices;
        private Matrix4x4 restCloth;
        private int windFrame = -1;
        private float load;
        private float lastSet = -1,
            lastLoad = float.NaN;
        internal LooseFootedSpritsailClothGeometry Geometry { get; private set; }

        internal LooseFootedSpritsailCloth(
            LooseFootedSpritsailRig rig,
            SkinnedMeshRenderer renderer
        )
        {
            this.rig = rig;
            this.renderer = renderer;
            mesh = Object.Instantiate(original: renderer.sharedMesh);
            mesh.name = "Spritsail instance cloth";
            mesh.MarkDynamic();
            renderer.sharedMesh = mesh;
            localVertices = new Vector3[mesh.vertexCount];
        }

        internal void Fit(LooseFootedSpritsailPose rest)
        {
            restCloth = rest.Sail;
            Geometry = new LooseFootedSpritsailClothGeometry(data: rig.Data, rest: rest);
            lastSet = -1;
        }

        internal void Refresh()
        {
            if (Geometry == null)
                return;
            bool preview = !rig.Sail.IsInstalled();
            float set = preview ? 1 : Mathf.Clamp01(value: rig.Sail.currentUnroll);
            if (windFrame != Time.frameCount)
            {
                windFrame = Time.frameCount;
                var wind =
                    Wind.currentWind
                    - (rig.Sail.shipRigidbody ? rig.Sail.shipRigidbody.velocity : Vector3.zero);
                float pressure = Vector3.Dot(lhs: wind, rhs: -rig.Frame.right);
                float target =
                    preview || Mathf.Abs(f: pressure) < 0.6f
                        ? 0
                        : Mathf.Clamp(value: pressure / 8, min: -1, max: 1);
                load = LooseFootedSpritsailClothGeometry.SmoothLoad(
                    current: load,
                    target: target,
                    deltaTime: Time.deltaTime
                );
            }
            if (set == lastSet && load == lastLoad)
                return;
            lastSet = set;
            lastLoad = load;
            Geometry.Pose(deployment: set, load: load);
            rig.Sprit.localPosition = Geometry.Pivot;
            rig.Sprit.localRotation = Geometry.Sprit.rotation;
            rig.Sprit.localScale = Geometry.Sprit.lossyScale;
            for (int i = 0; i < localVertices.Length; i++)
                localVertices[i] = LooseFootedSpritsailTransform.InversePoint(
                    matrix: restCloth,
                    point: Geometry.Vertices[i]
                );
            rig.SheetLineAttachment.localPosition = localVertices[rig.Data.SheetVertex];
            mesh.vertices = localVertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        internal void UpdateBounds()
        {
            if (Geometry == null)
                return;
            var bounds = new Bounds(
                center: renderer.transform.InverseTransformPoint(
                    position: rig.Frame.TransformPoint(position: Geometry.Vertices[0])
                ),
                size: Vector3.zero
            );
            foreach (var point in Geometry.Vertices)
                bounds.Encapsulate(
                    point: renderer.transform.InverseTransformPoint(
                        position: rig.Frame.TransformPoint(position: point)
                    )
                );
            bounds.Expand(amount: 0.05f);
            renderer.localBounds = bounds;
        }

        internal void Release() => Object.Destroy(obj: mesh);
    }
}
