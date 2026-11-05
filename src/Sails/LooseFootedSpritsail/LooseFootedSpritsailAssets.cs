// Owns only prefab-created meshes and materials; installed instances borrow these assets.
using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal sealed class LooseFootedSpritsailAssets : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();

        internal Mesh Own(Mesh mesh)
        {
            meshes.Add(item: mesh);
            return mesh;
        }

        internal Material Own(Material material)
        {
            materials.Add(item: material);
            return material;
        }

        private void OnDestroy() => Release();

        internal void Release()
        {
            foreach (var mesh in meshes)
                if (mesh)
                    Destroy(obj: mesh);
            foreach (var material in materials)
                if (material)
                    Destroy(obj: material);
            meshes.Clear();
            materials.Clear();
        }
    }
}
