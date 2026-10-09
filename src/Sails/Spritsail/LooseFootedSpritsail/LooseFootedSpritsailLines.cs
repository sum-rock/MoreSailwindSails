using MoreSailwindSails.Visuals;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail
{
    // Draws two clew sheets and a peak lashing while native controllers retain tension/input.
    internal sealed class LooseFootedSpritsailLines : MonoBehaviour
    {
        public RopeEffect[] Sources;
        public RoutedRope[] Sheets;
        public RoutedRope PeakLashing;
        public RopeEffect ReefSource;
        public SpritsailMount Mount;

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
            lines.Sheets = new RoutedRope[2];
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
            var rig = sail.GetComponent<LooseFootedSpritsailRig>();
            var luff = new Transform[LooseFootedSpritsailGeometry.Rows + 1];
            for (int row = 0; row < luff.Length; row++)
                luff[row] = rig.Bones[LooseFootedSpritsailGeometry.ShapeBone(row: row, column: 0)];
            lines.Mount = SpritsailMount.Create(sail: sail, luffBones: luff);
            return lines;
        }

        private static RoutedRope CreateLine(Transform parent, RopeEffect source, string name)
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
            return RoutedRope.Attach(line: renderer);
        }

        internal void Draw(
            Transform[] bones,
            Vector3 tip,
            CapsuleCollider mast,
            bool struck,
            long revision
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
                if (!visible)
                    Sheets[i].SetVisible(visible: false);
                if (visible)
                    Span(
                        line: Sheets[i],
                        start: source.transform.position,
                        end: bones[3].position,
                        sag: 0.015f
                    );
            }
            Span(line: PeakLashing, start: tip, end: bones[1].position, sag: 0.01f);
            Mount.Draw(
                revision: revision,
                mast: mast,
                fallbackRadius: LooseFootedSpritsailRigging.MastRadius(
                    mast: mast.GetComponent<Mast>()
                )
            );
        }

        private static void Span(RoutedRope line, Vector3 start, Vector3 end, float sag)
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
            line.SetVisible(visible: true);
        }

        internal void Hide()
        {
            if (Sheets != null)
                foreach (var line in Sheets)
                    if (line)
                        line.SetVisible(visible: false);
            if (PeakLashing)
                PeakLashing.SetVisible(visible: false);
        }

        private void OnDisable() => Hide();
    }
}
