using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Draws mast ties and the peak lashing; native gaff ropes render the single boom sheet.
    internal sealed class BoomedSpritsailLines : MonoBehaviour
    {
        public LineRenderer PeakLashing;
        public LineRenderer[] LuffTies;
        public SpritsailRopeCollar[] LuffCollars;
        private CapsuleCollider surfaceMast;
        private SpritsailMastSurface surface;

        internal static BoomedSpritsailLines Create(Sail sail)
        {
            var root = new GameObject(name: "BoomedSpritsail rigging visuals");
            root.transform.SetParent(parent: sail.transform, worldPositionStays: false);
            var lines = root.AddComponent<BoomedSpritsailLines>();
            var connections = sail.GetComponent<SailConnections>();
            var source = connections.angleControllerMid.GetComponent<RopeEffect>();
            var route = connections.midRopeAttachment.GetComponent<RopeEffect>();
            // Retain the native two-segment single sheet. The coordinated purchase
            // replaces reef visuals, and the old gaff topping lift is no longer used.
            foreach (var effect in sail.GetComponentsInChildren<RopeEffect>(includeInactive: true))
                if (effect != source && effect != route)
                    effect.gameObject.AddComponent<BoomedSpritsailReplacedRopeVisual>();
            lines.PeakLashing = CreateLine(
                parent: root.transform,
                source: source,
                name: "Peak lashing"
            );
            lines.LuffTies = new LineRenderer[7];
            lines.LuffCollars = new SpritsailRopeCollar[7];
            for (int i = 0; i < lines.LuffTies.Length; i++)
            {
                lines.LuffCollars[i] = SpritsailRopeCollar.Create(
                    parent: root.transform,
                    source: source,
                    name: "Luff mast collar " + i,
                    thicknessScale: 0.8f
                );
                lines.LuffTies[i] = CreateLine(
                    parent: root.transform,
                    source: source,
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
            Span(line: PeakLashing, start: tip, end: bones[1].position, sag: 0.01f);
            if (surfaceMast != mast)
            {
                surfaceMast = mast;
                surface = new SpritsailMastSurface(mast: mast.GetComponent<Mast>());
            }
            for (int i = 0; i < LuffTies.Length; i++)
            {
                float row = i * BoomedSpritsailGeometry.Rows / (float)(LuffTies.Length - 1);
                int lower = Mathf.FloorToInt(row);
                var point = Vector3.Lerp(
                    a: bones[BoomedSpritsailGeometry.ShapeBone(row: lower, column: 0)].position,
                    b: bones[
                        BoomedSpritsailGeometry.ShapeBone(
                            row: Mathf.Min(lower + 1, BoomedSpritsailGeometry.Rows),
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
                    fallback: BoomedSpritsailRigging.MastRadius(mast: mast.GetComponent<Mast>())
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
