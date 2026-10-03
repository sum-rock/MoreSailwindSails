using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Registers family members and protects the category from conflicting owners.
    internal static class SpritsailCatalog
    {
        private static readonly Dictionary<int, GameObject> prefabs =
            new Dictionary<int, GameObject>();

        internal static bool HasMembers
        {
            get
            {
                foreach (var prefab in prefabs.Values)
                    if (prefab)
                        return true;
                return false;
            }
        }

        internal static bool IsRegistered(int prefabIndex) =>
            prefabs.TryGetValue(key: prefabIndex, value: out var prefab) && prefab;

        internal static void ValidateCategory(GameObject[] sails)
        {
            if (Enum.IsDefined(enumType: typeof(SailCategory), value: SpritsailCategory.Value))
                throw new InvalidOperationException(
                    message: "Spritsails category 6 conflicts with a native category; registration stopped."
                );
            foreach (var entry in sails)
            {
                var sail = entry ? entry.GetComponent<Sail>() : null;
                if (
                    sail
                    && sail.category == SpritsailCategory.Value
                    && (
                        !prefabs.TryGetValue(key: sail.prefabIndex, value: out var owned)
                        || owned != entry
                    )
                )
                    throw new InvalidOperationException(
                        message: $"Spritsails category 6 is already used by {entry.name}; registration stopped."
                    );
            }
        }

        internal static void Register(Sail sail)
        {
            if (!sail || sail.category != SpritsailCategory.Value)
                throw new InvalidOperationException(
                    message: "Only Spritsails can join the family catalog."
                );
            if (
                prefabs.TryGetValue(key: sail.prefabIndex, value: out var owned)
                && owned
                && owned != sail.gameObject
            )
                throw new InvalidOperationException(
                    message: $"Spritsail prefab {sail.prefabIndex} already has an owner."
                );
            prefabs[sail.prefabIndex] = sail.gameObject;
        }

        internal static void Unregister(GameObject candidate)
        {
            if (!candidate)
                return;
            var sail = candidate.GetComponent<Sail>();
            if (
                sail
                && prefabs.TryGetValue(key: sail.prefabIndex, value: out var owned)
                && owned == candidate
            )
                prefabs.Remove(key: sail.prefabIndex);
        }

        internal static GameObject[] Available(IEnumerable<GameObject> source)
        {
            var result = new List<GameObject>();
            var seen = new HashSet<int>();
            if (source != null)
                foreach (var entry in source)
                {
                    var sail = entry ? entry.GetComponent<Sail>() : null;
                    if (
                        SpritsailCategory.IsSpritsail(sail: sail)
                        && !sail.obsolete
                        && prefabs.TryGetValue(key: sail.prefabIndex, value: out var owned)
                        && owned == entry
                        && seen.Add(item: sail.prefabIndex)
                    )
                        result.Add(item: entry);
                }
            return result.ToArray();
        }

        internal static void AddToShipyard(Shipyard shipyard)
        {
            if (!shipyard || shipyard.sailPrefabs == null)
                return;
            var result = new List<GameObject>(collection: shipyard.sailPrefabs);
            foreach (var prefab in prefabs.Values)
                if (prefab && !result.Contains(item: prefab))
                    result.Add(item: prefab);
            shipyard.sailPrefabs = result.ToArray();
        }
    }
}
