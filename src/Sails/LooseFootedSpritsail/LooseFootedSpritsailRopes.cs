// Draws the family-owned halyard route and three sliding luff ties with native rope materials.
using MoreSailwindSails.Visuals;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal sealed class LooseFootedSpritsailRopes
    {
        private readonly LooseFootedSpritsailRig rig;
        private readonly RoutedRope halyard;
        private readonly RoutedRope[] ties = new RoutedRope[3];

        internal LooseFootedSpritsailRopes(LooseFootedSpritsailRig rig, RopeEffect source)
        {
            this.rig = rig;
            halyard = Create(
                parent: rig.transform,
                name: "Spritsail halyard",
                source: source,
                count: 3
            );
            for (int i = 0; i < ties.Length; i++)
                ties[i] = Create(
                    parent: rig.transform,
                    name: "Spritsail luff tie " + i,
                    source: source,
                    count: 19
                );
        }

        private static RoutedRope Create(
            Transform parent,
            string name,
            RopeEffect source,
            int count
        )
        {
            var root = new GameObject(name: name);
            root.transform.SetParent(parent: parent, worldPositionStays: false);
            var line = root.AddComponent<LineRenderer>();
            var original = source.GetComponent<LineRenderer>();
            line.sharedMaterials = original.sharedMaterials;
            line.startColor = original.startColor;
            line.endColor = original.endColor;
            line.startWidth = line.endWidth = source.ropeWidth;
            line.textureMode = LineTextureMode.Tile;
            line.useWorldSpace = true;
            line.numCornerVertices = line.numCapVertices = 2;
            line.shadowCastingMode = original.shadowCastingMode;
            line.receiveShadows = original.receiveShadows;
            line.positionCount = count;
            return RoutedRope.Attach(line: line);
        }

        internal void Draw(Transform winch, Transform guide)
        {
            bool routed = winch && guide;
            if (routed)
            {
                halyard.SetPosition(index: 0, position: winch.position);
                halyard.SetPosition(index: 1, position: guide.position);
                halyard.SetPosition(index: 2, position: rig.ReefLineAttachment.position);
            }
            halyard.SetVisible(visible: routed);
            var geometry = rig.ClothGeometry;
            if (geometry == null)
                return;
            for (int i = 0; i < ties.Length; i++)
            {
                var corner =
                    i == 0 ? geometry.Throat
                    : i == 1 ? geometry.SnotterTie
                    : geometry.Tack;
                float radius = Mathf.Max(a: 0.01f, b: -corner.z);
                ties[i].SetPosition(index: 0, position: rig.Frame.TransformPoint(position: corner));
                // Circumscribe the mast so straight rope segments do not cut through its surface.
                float ringRadius = radius / Mathf.Cos(f: Mathf.PI / 16);
                for (int segment = 0; segment <= 16; segment++)
                {
                    float angle = segment * Mathf.PI / 8;
                    var point = new Vector3(
                        x: Mathf.Sin(f: angle) * ringRadius,
                        y: corner.y,
                        z: -Mathf.Cos(f: angle) * ringRadius
                    );
                    ties[i]
                        .SetPosition(
                            index: segment + 1,
                            position: rig.Frame.TransformPoint(position: point)
                        );
                }
                ties[i]
                    .SetPosition(index: 18, position: rig.Frame.TransformPoint(position: corner));
                ties[i].SetVisible(visible: true);
            }
        }

        internal void Hide()
        {
            halyard.SetVisible(visible: false);
            foreach (var tie in ties)
                tie.SetVisible(visible: false);
        }
    }
}
