// Initializes plain cloth on a private native recolored material and shares it with reefed renderers.
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansFlyingSail
{
    internal static class FishermansFlyingSailAppearance
    {
        // Existing PrefabsDirectory palette swatch, not a new RGB color.
        internal const int WhiteColorIndex = 11;

        internal static void Configure(Sail sail)
        {
            sail.ChangeSailColor(WhiteColorIndex);
            var rig = sail.GetComponent<FishermansFlyingSailRig>();
            var material = sail.cloth.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
            // Native recoloring obtains Renderer.material, isolating this from the donor.
            material.mainTexture = Texture2D.whiteTexture;
            rig.ReefedRenderer.sharedMaterial = material;
            rig.FurledColorReference.sharedMaterial = material;
        }
    }
}
