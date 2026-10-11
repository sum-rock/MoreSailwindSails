using MoreSailwindSails.Utils.Profiling;
using MoreSailwindSails.Visuals;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Draws the authored mount and peak lashing; native gaff ropes render the single boom sheet.
    internal sealed class BoomedSpritsailLines : MonoBehaviour
    {
        public RoutedRope PeakLashing;
        public SpritsailMount Mount;

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
            var rig = sail.GetComponent<BoomedSpritsailRig>();
            var luff = new Transform[BoomedSpritsailGeometry.Rows + 1];
            for (int row = 0; row < luff.Length; row++)
                luff[row] = rig.Bones[BoomedSpritsailGeometry.ShapeBone(row: row, column: 0)];
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
            if (PerformanceProfile.IsBypassed(target: ProfileBypass.SpritsailLiveRopes))
                Hide();
            else
            {
                Span(line: PeakLashing, start: tip, end: bones[1].position, sag: 0.01f);
            }
            Mount.Draw(
                revision: revision,
                mast: mast,
                fallbackRadius: BoomedSpritsailRigging.MastRadius(mast: mast.GetComponent<Mast>())
            );
        }

        private static void Span(RoutedRope line, Vector3 start, Vector3 end, float sag)
        {
            using (PerformanceProfile.Measure(target: ProfileTarget.Ropes))
            {
                float depth = (end - start).magnitude * sag;
                for (int i = 0; i < 9; i++)
                {
                    float t = i / 8f;
                    line.SetPosition(
                        index: i,
                        position: Vector3.Lerp(start, end, t)
                            + Vector3.down * (depth * 4 * t * (1 - t))
                    );
                }
                line.SetVisible(visible: true);
            }
        }

        internal void Hide()
        {
            if (PeakLashing)
                PeakLashing.SetVisible(visible: false);
        }

        private void OnDisable() => Hide();
    }
}
