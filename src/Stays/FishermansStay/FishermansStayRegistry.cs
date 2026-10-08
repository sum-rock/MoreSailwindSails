using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Stays.FishermansStay
{
    internal sealed class FishermansStayRegistry : MonoBehaviour
    {
        internal readonly List<FishermansStay> Stays = new List<FishermansStay>();
        internal readonly List<BoatPart> Parts = new List<BoatPart>();
        internal readonly List<GameObject> AuxiliaryObjects = new List<GameObject>();
        internal bool Registered;
        internal bool PreviewingOrder;

        internal static bool TryFind(Mast mount, out FishermansStay stay)
        {
            var boat = mount ? mount.GetComponentInParent<BoatRefs>() : null;
            var registry = boat ? boat.GetComponent<FishermansStayRegistry>() : null;
            stay = registry ? registry.Stays.FirstOrDefault(s => s.Mount == mount) : null;
            return stay != null;
        }

        internal static void Register(SaveableBoatCustomization customization)
        {
            var boat = customization.GetComponent<BoatRefs>();
            var parts = customization.GetComponent<BoatCustomParts>();
            if (!boat || !parts || parts.availableParts == null)
                return;
            var profile = BoatRigCatalog.Find(boat.name);
            if (profile == null || profile.Stays.Count == 0)
                return;
            var registry =
                boat.GetComponent<FishermansStayRegistry>()
                ?? boat.gameObject.AddComponent<FishermansStayRegistry>();
            if (registry.Registered)
                return;
            registry.Registered = true;
            try
            {
                // Resolve the complete profile before appending any saved part slots.
                var groups = FishermansStayReferences.Resolve(profile, parts, boat);
                if (boat.masts.Length < FishermansStayGeometry.MountCapacity)
                    Array.Resize(ref boat.masts, FishermansStayGeometry.MountCapacity);
                for (int index = 0; index < groups.Length; index++)
                {
                    var variants = new List<BoatPartOption>();
                    var empty = FishermansStay.NewInactive("FishermansStay None", boat.transform);
                    var emptyWalk = FishermansStay.NewInactive(
                        "FishermansStay None Walk",
                        boat.walkCol
                    );
                    registry.AuxiliaryObjects.Add(empty);
                    registry.AuxiliaryObjects.Add(emptyWalk);
                    var none = empty.AddComponent<BoatPartOption>();
                    none.optionName = "(no Fisherman's Stay: " + profile.Stays[index].Label + ")";
                    none.requires = new List<BoatPartOption>();
                    none.requiresDisabled = new List<BoatPartOption>();
                    none.childOptions = new GameObject[0];
                    none.walkColObject = emptyWalk;
                    none.canInstall = true;
                    variants.Add(none);
                    foreach (
                        var configurations in groups[index].GroupBy(r => r.Definition.MountIndex)
                    )
                    {
                        var stay = new FishermansStay(
                            boat: boat,
                            configurations: configurations.ToArray()
                        );
                        registry.Stays.Add(stay);
                        stay.Create();
                        variants.Add(stay.Option);
                    }
                    registry.Parts.Add(
                        new BoatPart
                        {
                            category = 2,
                            activeOption = 0,
                            partOptions = variants,
                        }
                    );
                    empty.SetActive(true);
                    emptyWalk.SetActive(true);
                }
                // Awake must see complete references and register each fixed ID,
                // including variants that are initially disabled.
                foreach (var stay in registry.Stays)
                {
                    stay.Mount.gameObject.SetActive(true);
                    if (boat.masts[stay.Mount.orderIndex] != stay.Mount)
                        throw new InvalidOperationException(
                            "New stay mount did not register during Awake."
                        );
                    stay.Mount.gameObject.SetActive(false);
                }
                parts.availableParts.AddRange(registry.Parts);
                Plugin.Log.LogInfo(
                    $"Registered Fisherman's Stays: boat={boat.name}, groups={registry.Parts.Count}, variants={registry.Stays.Count}."
                );
            }
            catch (Exception exception)
            {
                foreach (var part in registry.Parts)
                    parts.availableParts.Remove(part);
                foreach (var stay in registry.Stays)
                {
                    if (stay.Mount)
                    {
                        int slot = stay.Mount.orderIndex;
                        if (slot < boat.masts.Length && boat.masts[slot] == stay.Mount)
                            boat.masts[slot] = null;
                    }
                    stay.Destroy();
                }
                foreach (var item in registry.AuxiliaryObjects)
                    if (item)
                        Object.Destroy(item);
                registry.Stays.Clear();
                registry.Parts.Clear();
                registry.AuxiliaryObjects.Clear();
                Plugin.Log.LogError(
                    $"Fisherman's Stay profile rejected for {boat.name}: {exception}"
                );
            }
        }

        internal void PrepareSnapshot(IReadOnlyList<int> savedOptions)
        {
            var parts = GetComponent<BoatCustomParts>();
            foreach (var part in Parts)
                part.activeOption = FishermansStaySaveState.RestoreOption(
                    savedOptions,
                    parts.availableParts.IndexOf(part)
                );
        }

        internal void Refresh()
        {
            foreach (var stay in Stays)
                stay.Refresh();
        }

        internal bool Protects(BoatPartOption option) =>
            Stays.Any(stay =>
                stay.Mount
                && stay.Mount.sails.Any(s => s)
                && (stay.Option == option || stay.Option.requires.Contains(option))
            );

        private void OnDestroy()
        {
            foreach (var stay in Stays)
                stay.Destroy();
            foreach (var item in AuxiliaryObjects)
                if (item)
                    Object.Destroy(item);
        }
    }
}
