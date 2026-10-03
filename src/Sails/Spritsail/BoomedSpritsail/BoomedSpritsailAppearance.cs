using System;
using System.Collections.Generic;
using ShipyardExpansion.Scripts;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Keeps spritsail paint and plain texture independent of donor assets.
    internal static class BoomedSpritsailAppearance
    {
        // Existing PrefabsDirectory palette swatch, not a new RGB color.
        internal const int WhiteColorIndex = 11;

        // The SE compatibility patch seeds the native plain texture before discovery.
        internal const int PlainTextureIndex = 0;

        internal static void Configure(Sail sail)
        {
            var changer = sail.GetComponent<SailTextureChanger>();
            if (!changer || !Compatibility.ShipyardExpansionTextureCatalog.HasPlainFirst)
                throw new InvalidOperationException(
                    message: "Shipyard Expansion's plain sail texture is unavailable."
                );
            // Replace the clone's list, leaving the gaff donor's options intact.
            changer.allowedTextures = new List<int> { PlainTextureIndex };
            changer.SetTexture(index: PlainTextureIndex);
            sail.ChangeSailColor(newColor: WhiteColorIndex);
            var rig = sail.GetComponent<BoomedSpritsailRig>();
            var material = sail.cloth.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
            rig.ReefedRenderer.sharedMaterial = material;
            rig.FurledColorReference.sharedMaterial = material;
        }
    }
}
