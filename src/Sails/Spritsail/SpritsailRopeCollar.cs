using MoreSailwindSails.Visuals;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Draws a close-wound rope collar without changing native rope endpoints or sail physics.
    internal sealed class SpritsailRopeCollar : MonoBehaviour
    {
        public RoutedRope Rope;
        private const int Segments = 96;

        internal static SpritsailRopeCollar Create(
            Transform parent,
            RopeEffect source,
            string name,
            float thicknessScale = 1f
        )
        {
            var root = new GameObject(name: name);
            root.transform.SetParent(parent: parent, worldPositionStays: false);
            var collar = root.AddComponent<SpritsailRopeCollar>();
            var line = root.AddComponent<LineRenderer>();
            var original = source.GetComponent<LineRenderer>();
            line.sharedMaterials = original.sharedMaterials;
            line.startColor = original.startColor;
            line.endColor = original.endColor;
            line.startWidth = line.endWidth =
                source.ropeWidth * SpritsailSpritGeometry.RopeThicknessMultiplier * thicknessScale;
            line.textureMode = LineTextureMode.Tile;
            line.useWorldSpace = true;
            line.positionCount = Segments + 1;
            line.numCapVertices = 3;
            collar.Rope = RoutedRope.Attach(line: line);
            return collar;
        }

        internal Vector3 Pose(Vector3 center, Vector3 axis, Vector3 outward, float radius)
        {
            axis.Normalize();
            outward = Vector3.ProjectOnPlane(vector: outward, planeNormal: axis).normalized;
            if (outward.sqrMagnitude < 0.001f)
                outward = Vector3
                    .Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right)
                    .normalized;
            var side = Vector3.Cross(axis, outward).normalized;
            float width = Rope.Width;
            for (int i = 0; i <= Segments; i++)
            {
                float t = i / (float)Segments;
                float angle = (t - 0.5f) * 6 * Mathf.PI;
                Rope.SetPosition(
                    index: i,
                    position: center
                        + (outward * Mathf.Cos(angle) + side * Mathf.Sin(angle))
                            * (radius + width * 0.55f)
                        + axis * ((t - 0.5f) * 3 * width * 1.05f)
                );
            }
            Rope.SetVisible(visible: true);
            return center + outward * (radius + width * 0.55f);
        }

        internal void Hide()
        {
            if (Rope)
                Rope.SetVisible(visible: false);
        }

        private void OnDisable() => Hide();
    }
}
