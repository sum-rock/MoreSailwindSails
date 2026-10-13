// Sets native color and plain cloth default, sharing the result with reefed renderers.
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal static class FishermansStaysailAppearance
    {
        // Existing PrefabsDirectory palette swatch, not a new RGB color.
        internal const int WhiteColorIndex = 11;

        internal static void Configure(Sail sail)
        {
            sail.ChangeSailColor(WhiteColorIndex);
            var rig = sail.GetComponent<FishermansStaysailRig>();
            var material = sail.cloth.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
            // Native ChangeSailColor uses Renderer.material, so this is already
            // a private recolored material. Keep its shader; avoid the brig atlas
            // on planar UVs. Optional SE material updates have their own white restriction.
            material.mainTexture = Texture2D.whiteTexture;
            rig.ReefedRenderer.sharedMaterial = material;
            rig.FurledColorReference.sharedMaterial = material;
        }
    }
}
