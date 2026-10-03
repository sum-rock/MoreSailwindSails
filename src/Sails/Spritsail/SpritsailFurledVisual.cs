using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Reuses the brig gaff's native furled cloth and rope mesh as a separate struck-only renderer.
    internal sealed class SpritsailFurledVisual : MonoBehaviour
    {
        public MeshRenderer Renderer;
        public Bounds SourceBounds;
        public Vector3 SourceScale;
        private Material clothMaterial;

        internal static SpritsailFurledVisual Create(Transform parent, PrefabsDirectory directory)
        {
            var donor = directory.sails.Length > 119 ? directory.sails[119] : null;
            var sail = donor ? donor.GetComponent<Sail>() : null;
            var reef = donor ? donor.GetComponent<ReefEffectAnimUniversal>() : null;
            var original = reef ? reef.furledSail : null;
            var filter = original ? original.GetComponent<MeshFilter>() : null;
            if (
                !sail
                || sail.category != SailCategory.gaff
                || sail.prefabIndex != 119
                || !filter
                || !filter.sharedMesh
                || filter.sharedMesh.name != "furled__sail_cloth_back"
                || original.sharedMaterials.Length != 2
            )
                throw new InvalidOperationException(
                    message: "Expected brig gaff 119's native furled cloth/rope mesh."
                );
            var root = new GameObject(name: "Spritsail native brig gaff furled cloth");
            root.transform.SetParent(parent: parent, worldPositionStays: false);
            root.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var visual = root.AddComponent<SpritsailFurledVisual>();
            visual.SourceBounds = filter.sharedMesh.bounds;
            visual.SourceScale = original.transform.localScale;
            visual.Renderer = root.AddComponent<MeshRenderer>();
            visual.Renderer.sharedMaterials = original.sharedMaterials;
            visual.Renderer.enabled = false;
            return visual;
        }

        internal void Pose(
            Vector3 heel,
            Vector3 tip,
            Vector3 radial,
            float heelGap,
            Material cloth,
            bool visible
        )
        {
            Renderer.enabled = visible;
            if (!visible)
                return;
            if (clothMaterial != cloth)
            {
                var materials = Renderer.sharedMaterials;
                materials[0] = cloth;
                Renderer.sharedMaterials = materials;
                clothMaterial = cloth;
            }
            var axis = (tip - heel).normalized;
            var rotation = Quaternion.LookRotation(forward: axis, upwards: radial);
            float lengthScale = (tip - heel).magnitude / (SourceBounds.size.z * SourceScale.z);
            var scale = SourceScale * lengthScale;
            // The native bundle's long axis is Z. Seat its inward Y face against
            // the mast while keeping its rope submesh and native proportions.
            var center =
                (heel + tip) * 0.5f
                + radial * (SourceBounds.extents.y * scale.y + 0.003f - heelGap);
            transform.SetPositionAndRotation(
                position: center - rotation * Vector3.Scale(a: SourceBounds.center, b: scale),
                rotation: rotation
            );
            var parentScale = transform.parent.lossyScale;
            transform.localScale = new Vector3(
                scale.x / parentScale.x,
                scale.y / parentScale.y,
                scale.z / parentScale.z
            );
        }

        internal void Hide()
        {
            if (Renderer)
                Renderer.enabled = false;
        }

        private void OnDisable() => Hide();
    }
}
