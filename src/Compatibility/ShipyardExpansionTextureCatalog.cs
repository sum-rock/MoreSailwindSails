using ShipyardExpansion.Scripts;
using UnityEngine;

namespace MoreSailwindSails.Compatibility
{
    // Registers the native plain texture in SE's name-based catalog without changing assets.
    internal static class ShipyardExpansionTextureCatalog
    {
        internal const string PlainTextureName = "ParticleCloudWhite";

        internal static bool HasPlainTexture =>
            SailTextureChanger.textures.TryGetValue(PlainTextureName, out var texture)
            && texture
            && texture.name == PlainTextureName;

        internal static void SeedPlain()
        {
            // SE 0.12.1 keys textures and saved selections by name, not discovery order.
            if (HasPlainTexture)
                return;
            var directory = PrefabsDirectory.instance;
            // The brig jib's cloth is painted, but its native furled bundle uses
            // the plain texture. Read the asset without instantiating a material.
            var source = directory && directory.sails.Length > 110 ? directory.sails[110] : null;
            var sail = source ? source.GetComponent<Sail>() : null;
            var reef = source ? source.GetComponent<ReefEffectAnimUniversal>() : null;
            var material = reef && reef.furledSail ? reef.furledSail.sharedMaterial : null;
            var texture =
                material && material.HasProperty("_MainTex")
                    ? material.GetTexture("_MainTex")
                    : null;
            if (!sail || sail.prefabIndex != 110 || !texture || texture.name != PlainTextureName)
            {
                Plugin.Log.LogError(
                    "Could not seed SE's plain sail texture: native brig jib bundle changed."
                );
                return;
            }
            SailTextureChanger.textures[PlainTextureName] = texture;
            Plugin.Log.LogInfo(
                "Registered native ParticleCloudWhite in SE's named sail texture catalog."
            );
        }
    }
}
