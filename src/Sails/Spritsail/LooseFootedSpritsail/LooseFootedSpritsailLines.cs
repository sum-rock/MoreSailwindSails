using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail
{
    // Draws two clew sheets and a peak lashing while native controllers retain tension/input.
    internal sealed class LooseFootedSpritsailLines : MonoBehaviour
    {
        public RopeEffect[] Sources;
        public LineRenderer[] Sheets;
        public LineRenderer PeakLashing;
        public RopeEffect ReefSource;
        public LineRenderer[] LuffTies;
        public SpritsailRopeCollar[] LuffCollars;
        private CapsuleCollider surfaceMast;
        private SpritsailMastSurface surface;

        internal static LooseFootedSpritsailLines Create(
            Sail sail,
            RopeEffect left,
            RopeEffect right
        )
        {
            var root = new GameObject(name: "LooseFootedSpritsail rigging visuals");
            root.transform.SetParent(parent: sail.transform, worldPositionStays: false);
            var lines = root.AddComponent<LooseFootedSpritsailLines>();
            lines.Sources = new[] { left, right };
            lines.Sheets = new LineRenderer[2];
            for (int i = 0; i < 2; i++)
            {
                lines.Sources[i].gameObject.AddComponent<LooseFootedSpritsailNativeSheetVisual>();
                lines.Sheets[i] = CreateLine(
                    parent: root.transform,
                    source: left,
                    name: i == 0 ? "Port clew sheet" : "Starboard clew sheet"
                );
            }
            var connections = sail.GetComponent<SailConnections>();
            lines.ReefSource = connections.reefController.GetComponent<RopeEffect>();
            var reefEffects = new[]
            {
                lines.ReefSource,
                connections.mastReefAttachment.GetComponent<RopeEffect>(),
                connections.mastReefAttExtension.GetComponent<RopeEffect>(),
            };
            foreach (var effect in reefEffects)
            {
                if (!effect || !effect.transform.IsChildOf(parent: sail.transform))
                    throw new System.InvalidOperationException(
                        message: "Spritsail reef endpoints must belong to the cloned sail."
                    );
                effect.gameObject.AddComponent<LooseFootedSpritsailNativeSheetVisual>();
            }
            lines.PeakLashing = CreateLine(
                parent: root.transform,
                source: left,
                name: "Peak lashing"
            );
            lines.LuffTies = new LineRenderer[7];
            lines.LuffCollars = new SpritsailRopeCollar[7];
            for (int i = 0; i < lines.LuffTies.Length; i++)
            {
                lines.LuffCollars[i] = SpritsailRopeCollar.Create(
                    parent: root.transform,
                    source: left,
                    name: "Luff mast collar " + i,
                    thicknessScale: 0.8f
                );
                lines.LuffTies[i] = CreateLine(
                    parent: root.transform,
                    source: left,
                    name: "Luff tie " + i
                );
            }
            return lines;
        }

        private static LineRenderer CreateLine(Transform parent, RopeEffect source, string name)
        {
            var root = new GameObject(name: name);
            root.transform.SetParent(parent: parent, worldPositionStays: false);
            var renderer = root.AddComponent<LineRenderer>();
            var original = source.GetComponent<LineRenderer>();
            renderer.sharedMaterials = original.sharedMaterials;
            renderer.startWidth = renderer.endWidth = source.ropeWidth;
            renderer.startColor = original.startColor;
            renderer.endColor = original.endColor;
            renderer.textureMode = LineTextureMode.Tile;
            renderer.useWorldSpace = true;
            renderer.positionCount = 9;
            renderer.enabled = false;
            return renderer;
        }

        internal void Draw(
            Transform[] bones,
            Vector3 tip,
            CapsuleCollider mast,
            Vector3 aft,
            bool struck
        )
        {
            if (struck || !mast)
            {
                // The native furled mesh supplies its own rope bindings.
                Hide();
                return;
            }
            for (int i = 0; i < 2; i++)
            {
                var source = Sources[i];
                bool visible = source && source.gameObject.activeInHierarchy;
                Sheets[i].enabled = visible;
                if (visible)
                    Span(
                        line: Sheets[i],
                        start: source.transform.position,
                        end: bones[3].position,
                        sag: 0.015f
                    );
            }
            Span(line: PeakLashing, start: tip, end: bones[1].position, sag: 0.01f);
            if (surfaceMast != mast)
            {
                surfaceMast = mast;
                surface = new SpritsailMastSurface(mast: mast.GetComponent<Mast>());
            }
            for (int i = 0; i < LuffTies.Length; i++)
            {
                float row = i * LooseFootedSpritsailGeometry.Rows / (float)(LuffTies.Length - 1);
                int lower = Mathf.FloorToInt(row);
                var point = Vector3.Lerp(
                    a: bones[
                        LooseFootedSpritsailGeometry.ShapeBone(row: lower, column: 0)
                    ].position,
                    b: bones[
                        LooseFootedSpritsailGeometry.ShapeBone(
                            row: Mathf.Min(lower + 1, LooseFootedSpritsailGeometry.Rows),
                            column: 0
                        )
                    ].position,
                    t: row - lower
                );
                var localAxis =
                    mast.direction == 0 ? Vector3.right
                    : mast.direction == 1 ? Vector3.up
                    : Vector3.forward;
                var axis = mast.transform.TransformDirection(direction: localAxis).normalized;
                var origin = mast.transform.TransformPoint(position: mast.center);
                var center = origin + axis * Vector3.Dot(point - origin, axis);
                var outward = Vector3.ProjectOnPlane(vector: aft, planeNormal: axis).normalized;
                float radius = surface.Radius(
                    center: center,
                    direction: outward,
                    fallback: LooseFootedSpritsailRigging.MastRadius(
                        mast: mast.GetComponent<Mast>()
                    )
                );
                var start = LuffCollars[i]
                    .Pose(center: center, axis: axis, outward: outward, radius: radius);
                Span(line: LuffTies[i], start: start, end: point, sag: 0);
            }
        }

        private static void Span(LineRenderer line, Vector3 start, Vector3 end, float sag)
        {
            float depth = (end - start).magnitude * sag;
            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f;
                line.SetPosition(
                    index: i,
                    position: Vector3.Lerp(start, end, t) + Vector3.down * (depth * 4 * t * (1 - t))
                );
            }
            line.enabled = true;
        }

        internal void Hide()
        {
            if (Sheets != null)
                foreach (var line in Sheets)
                    if (line)
                        line.enabled = false;
            if (LuffTies != null)
                foreach (var line in LuffTies)
                    if (line)
                        line.enabled = false;
            if (LuffCollars != null)
                foreach (var collar in LuffCollars)
                    if (collar)
                        collar.Hide();
            if (PeakLashing)
                PeakLashing.enabled = false;
        }

        private void OnDisable() => Hide();
    }
}
