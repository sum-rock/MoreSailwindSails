// Registers Mk.D through the shared spritsail builder; future cuts supply their own definitions.
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail.MkD
{
    internal static class LooseFootedSpritsailMkD
    {
        internal const int PrefabIndex = 407;
        internal static readonly LooseFootedSpritsailDefinition Definition =
            new LooseFootedSpritsailDefinition(
                prefabIndex: PrefabIndex,
                displayName: "Loose Footed Spritsail Mk.D",
                createAsset: LooseFootedSpritsailMkDBakedAsset.Create
            );
        private static GameObject prefab;

        internal static void Register(PrefabsDirectory directory) =>
            prefab = LooseFootedSpritsailPrefab.Register(
                directory: directory,
                definition: Definition,
                existing: prefab
            );

        internal static void AddToShipyard(Shipyard shipyard) =>
            LooseFootedSpritsailPrefab.AddToShipyard(shipyard: shipyard, prefab: prefab);
    }
}
