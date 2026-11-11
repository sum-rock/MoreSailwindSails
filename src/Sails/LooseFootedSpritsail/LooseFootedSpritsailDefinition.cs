// Supplies each spritsail cut's stable save identity and baked asset factory to the shared builder.
using System;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal sealed class LooseFootedSpritsailDefinition
    {
        internal readonly int PrefabIndex;
        internal readonly string DisplayName;
        internal readonly Func<LooseFootedSpritsailAssetData> CreateAsset;

        internal LooseFootedSpritsailDefinition(
            int prefabIndex,
            string displayName,
            Func<LooseFootedSpritsailAssetData> createAsset
        )
        {
            PrefabIndex = prefabIndex;
            DisplayName = displayName;
            CreateAsset = createAsset;
        }
    }
}
