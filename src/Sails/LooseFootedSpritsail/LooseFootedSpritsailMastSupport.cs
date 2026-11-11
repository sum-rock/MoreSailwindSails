// Resolves active descendant mast sections without changing the sail's saved mount.
using MoreSailwindSails.BoatConfiguration;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal static class LooseFootedSpritsailMastSupport
    {
        internal static float Height(Mast mast)
        {
            float height = mast.mastHeight;
            var boat = mast.GetComponentInParent<BoatRefs>();
            var definition = boat ? BoatCatalog.Find(boatName: boat.name) : null;
            if (definition == null || boat.masts == null)
                return height;
            foreach (var candidate in boat.masts)
            {
                if (
                    !candidate
                    || !candidate.gameObject.activeInHierarchy
                    || !Supported(
                        definition: definition,
                        masts: boat.masts,
                        selected: mast.orderIndex,
                        section: candidate.orderIndex
                    )
                )
                    continue;
                // Native mast origins are at their heads; local Z runs along the shaft.
                float top =
                    mast.transform.InverseTransformPoint(position: candidate.transform.position).z
                    + mast.mastHeight;
                if (LooseFootedSpritsailFittingGeometry.Finite(value: top))
                    height = Mathf.Max(a: height, b: top);
            }
            return height;
        }

        private static bool Supported(
            BoatDefinition definition,
            Mast[] masts,
            int selected,
            int section
        )
        {
            return LooseFootedSpritsailFittingGeometry.SupportedSection(
                parents: definition.MastParents,
                selected: selected,
                section: section,
                active: index =>
                {
                    foreach (var mast in masts)
                        if (mast && mast.orderIndex == index && mast.gameObject.activeInHierarchy)
                            return true;
                    return false;
                }
            );
        }
    }
}
