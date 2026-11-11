// Registers Mk.C through the shared spritsail builder; future cuts supply their own definitions.
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail.MkC
{
    internal static class LooseFootedSpritsailMkC
    {
        internal const int PrefabIndex = 406;
        internal static readonly LooseFootedSpritsailDefinition Definition =
            new LooseFootedSpritsailDefinition(
                prefabIndex: PrefabIndex,
                displayName: "Loose Footed Spritsail Mk.C",
                createAsset: LooseFootedSpritsailMkCBakedAsset.Create
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
