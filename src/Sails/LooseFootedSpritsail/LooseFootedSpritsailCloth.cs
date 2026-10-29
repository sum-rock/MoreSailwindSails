// Prepares the physical pose separately from the once-per-frame visual cloth upload.
using MoreSailwindSails.Utils.Profiling;
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
        private Matrix4x4 inverseRestCloth;
        private int windFrame = -1;
        private int renderFrame = -1;
        private readonly float phase;
        private double flutterTime;
        private float flutterStrength;
        private Vector4 lastFlutter;
        private bool visualDirty = true;
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
            phase = (rig.GetInstanceID() & 1023) * (2 * Mathf.PI / 1024);
        }

        internal void Fit(LooseFootedSpritsailPose rest)
        {
            restCloth = rest.Sail;
            inverseRestCloth = restCloth.inverse;
            Geometry = new LooseFootedSpritsailClothGeometry(data: rig.Data, rest: rest);
            lastSet = -1;
        }

        internal void Refresh()
        {
            using (
                var profile = PerformanceProfile.Measure(
                    target: ProfileTarget.Shape,
                    family: ProfileFamily.Spritsail
                )
            )
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
                rig.SheetLineAttachment.localPosition = LooseFootedSpritsailTransform.InversePoint(
                    matrix: restCloth,
                    point: Geometry.Vertices[rig.Data.SheetVertex]
                );
                visualDirty = true;
            }
        }

        internal void Render()
        {
            using (
                var profile = PerformanceProfile.Measure(
                    target: ProfileTarget.Shape,
                    family: ProfileFamily.Spritsail
                )
            )
            {
                if (Geometry == null || renderFrame == Time.frameCount)
                    return;
                renderFrame = Time.frameCount;
                if (Time.deltaTime <= 0 && !visualDirty)
                    return;
                bool active =
                    rig.SailingReady
                    && GameState.playing
                    && !GameState.currentlyLoading
                    && !GameState.currentShipyard
                    && !GameState.shipyardUpdatingOrder
                    && !(rig.Sail.damage && rig.Sail.damage.sunk);
                if (active)
                {
                    float target = LooseFootedSpritsailFlutter.Strength(
                        capturedWind: rig.Sail.GetCapturedWindFraction(),
                        windSpeed: rig.Sail.apparentWind.magnitude,
                        shadow: rig.Sail.GetCurrentShadowMult()
                    );
                    flutterStrength = LooseFootedSpritsailFlutter.SmoothStrength(
                        current: flutterStrength,
                        target: target,
                        deltaTime: Time.deltaTime
                    );
                    flutterTime += Time.deltaTime;
                }
                else
                    flutterStrength = 0;
                var flutter = LooseFootedSpritsailFlutter.Waves(
                    time: flutterTime,
                    phase: phase,
                    strength: flutterStrength * lastSet
                );
                if (
                    !visualDirty
                    && flutter.x == lastFlutter.x
                    && flutter.y == lastFlutter.y
                    && flutter.z == lastFlutter.z
                    && flutter.w == lastFlutter.w
                )
                    return;
                visualDirty = false;
                lastFlutter = flutter;
                var frameToRenderer =
                    renderer.transform.worldToLocalMatrix * rig.Frame.localToWorldMatrix;
                var bounds = new Bounds();
                for (int i = 0; i < localVertices.Length; i++)
                {
                    var point = Geometry.VisualVertex(index: i, flutter: flutter);
                    localVertices[i] = inverseRestCloth.MultiplyPoint3x4(point: point);
                    var renderedPoint = frameToRenderer.MultiplyPoint3x4(point: point);
                    if (i == 0)
                        bounds = new Bounds(center: renderedPoint, size: Vector3.zero);
                    else
                        bounds.Encapsulate(point: renderedPoint);
                }
                mesh.vertices = localVertices;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                UpdateBounds(bounds: bounds);
            }
        }

        private void UpdateBounds(Bounds bounds)
        {
            bounds.Expand(amount: 0.05f);
            renderer.localBounds = bounds;
        }

        internal void Release() => Object.Destroy(obj: mesh);
    }
}
