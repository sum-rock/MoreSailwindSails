// Prepares the physical pose separately from the once-per-frame visual cloth upload.
using HarmonyLib;
using MoreSailwindSails.Utils.Profiling;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal sealed class LooseFootedSpritsailCloth
    {
        private static readonly System.Func<Sail, Vector3, float> windAngle =
            AccessTools.MethodDelegate<System.Func<Sail, Vector3, float>>(
                AccessTools.Method(type: typeof(Sail), name: "GetWindAngle")
            );
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
        private float gatheredStrength;
        private Vector3 lastGathered;
        private Vector4 lastFlutter;
        private bool visualDirty = true;
        private float load;
        private Vector3 apparentWind;
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
                bool struck = rig.IsStruck;
                renderer.enabled = !struck;
                rig.SpritRenderer.enabled = !struck;
                rig.StruckRenderer.enabled = struck;
                if (struck)
                {
                    Geometry.Pose(deployment: 0, load: 0);
                    rig.ReefLineAttachment.position = rig.Struck.TransformPoint(
                        position: rig.Data.StruckReefLineAttachment
                    );
                    rig.SheetLineAttachment.position = rig.Struck.TransformPoint(
                        position: rig.Data.StruckSheetLineAttachment
                    );
                    lastSet = -1;
                    return;
                }
                rig.ReefLineAttachment.localPosition = rig.Data.ReefLineAttachment;
                bool preview = !rig.Sail.IsInstalled();
                float set = preview ? 1 : Mathf.Clamp01(value: rig.Sail.currentUnroll);
                if (windFrame != Time.frameCount)
                {
                    windFrame = Time.frameCount;
                    apparentWind =
                        Wind.currentWind
                        - (rig.Sail.shipRigidbody ? rig.Sail.shipRigidbody.velocity : Vector3.zero);
                    float target = LooseFootedSpritsailClothGeometry.WindLoad(
                        wind: apparentWind,
                        normal: -rig.Frame.right
                    );
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
                if (Geometry == null || rig.IsStruck || renderFrame == Time.frameCount)
                    return;
                renderFrame = Time.frameCount;
                if (Time.deltaTime <= 0 && !visualDirty)
                    return;
                bool preview = GameState.currentShipyard && rig.Mast && rig.Boat;
                bool active = LooseFootedSpritsailFlutter.CanAnimate(
                    sailing: rig.SailingReady && GameState.playing,
                    preview: preview,
                    loading: GameState.currentlyLoading,
                    updatingOrder: GameState.shipyardUpdatingOrder,
                    sunk: rig.Sail.damage && rig.Sail.damage.sunk
                );
                if (active)
                {
                    float shadow = rig.Sail.GetCurrentShadowMult();
                    // Preview physics clears the native wind caches. Read the native angle
                    // from our posed sail and visual wind without calculating propulsion.
                    if (preview)
                        rig.RefreshAerodynamics();
                    float target = LooseFootedSpritsailFlutter.Strength(
                        capturedWind: preview
                            ? windAngle(rig.Sail, apparentWind) / 90 * shadow
                            : rig.Sail.GetCapturedWindFraction(),
                        windSpeed: preview
                            ? apparentWind.magnitude
                            : rig.Sail.apparentWind.magnitude,
                        shadow: shadow
                    );
                    flutterStrength = LooseFootedSpritsailFlutter.SmoothStrength(
                        current: flutterStrength,
                        target: target,
                        deltaTime: Time.deltaTime
                    );
                    gatheredStrength = LooseFootedSpritsailFlutter.SmoothStrength(
                        current: gatheredStrength,
                        target: LooseFootedSpritsailFlutter.GatheredStrength(
                            pressure: LooseFootedSpritsailClothGeometry.WindLoad(
                                wind: apparentWind,
                                normal: -rig.Frame.right
                            ),
                            shadow: shadow,
                            deployment: lastSet
                        ),
                        deltaTime: Time.deltaTime
                    );
                    flutterTime += Time.deltaTime;
                }
                else
                    flutterStrength = gatheredStrength = 0;
                var flutter = LooseFootedSpritsailFlutter.Waves(
                    time: flutterTime,
                    phase: phase,
                    strength: flutterStrength * lastSet
                );
                var gathered = LooseFootedSpritsailFlutter.GatheredWaves(
                    time: flutterTime,
                    phase: phase,
                    strength: gatheredStrength
                );
                if (
                    !visualDirty
                    && gathered.x == lastGathered.x
                    && gathered.y == lastGathered.y
                    && gathered.z == lastGathered.z
                    && flutter.x == lastFlutter.x
                    && flutter.y == lastFlutter.y
                    && flutter.z == lastFlutter.z
                    && flutter.w == lastFlutter.w
                )
                    return;
                visualDirty = false;
                lastFlutter = flutter;
                lastGathered = gathered;
                var frameToRenderer =
                    renderer.transform.worldToLocalMatrix * rig.Frame.localToWorldMatrix;
                var bounds = new Bounds();
                for (int i = 0; i < localVertices.Length; i++)
                {
                    var point = Geometry.VisualVertex(
                        index: i,
                        flutter: flutter,
                        gathered: gathered
                    );
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
