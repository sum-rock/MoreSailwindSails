using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Renders the rigid foot spar with a template-owned mesh and read-only native timber material.
    internal sealed class BoomedSpritsailBoom : MonoBehaviour
    {
        public MeshRenderer Renderer;

        internal static BoomedSpritsailBoom Create(Transform parent, Mesh mesh, Material timber)
        {
            var root = new GameObject(name: "BoomedSpritsail foot boom");
            root.transform.SetParent(parent: parent, worldPositionStays: false);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var boom = root.AddComponent<BoomedSpritsailBoom>();
            boom.Renderer = root.AddComponent<MeshRenderer>();
            boom.Renderer.sharedMaterial = timber;
            boom.Renderer.enabled = false;
            return boom;
        }

        internal void Pose(Vector3 heel, Vector3 tip, float radius, bool visible)
        {
            transform.SetPositionAndRotation(
                position: heel,
                rotation: Quaternion.LookRotation(forward: tip - heel)
            );
            var scale = transform.parent.lossyScale;
            transform.localScale = new Vector3(
                radius / scale.x,
                radius / scale.y,
                (tip - heel).magnitude / scale.z
            );
            Renderer.enabled = visible;
        }

        private void OnDisable()
        {
            if (Renderer)
                Renderer.enabled = false;
        }
    }
}
