using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay
{
    internal sealed class FishermansStayReferences
    {
        internal FishermansStayVariantDefinition Definition;
        internal Mast Donor,
            Fore,
            Aft;
        internal Transform Guide;
        internal BoatPartOption[] Required,
            Forbidden;

        // Resolve fitted parts even while the boat root is inactive during loading.
        // Actual use still requires activeInHierarchy in FishermansStay.Available.
        internal bool Fitted(Transform boatRoot)
        {
            bool Enabled(Transform item)
            {
                while (item && item != boatRoot)
                {
                    if (!item.gameObject.activeSelf)
                        return false;
                    item = item.parent;
                }
                return item == boatRoot;
            }
            return Required.All(o => o && Enabled(o.transform))
                && Forbidden.All(o => o && !Enabled(o.transform))
                && Guide
                && Enabled(Guide);
        }

        internal static FishermansStayReferences[][] Resolve(
            BoatRigDefinition profile,
            BoatCustomParts parts,
            BoatRefs boat
        )
        {
            var masts = parts
                .availableParts.SelectMany(p => p.partOptions)
                .Where(o => o)
                .Select(o => o.GetComponent<Mast>())
                .Where(m => m)
                .Distinct()
                .ToDictionary(m => m.orderIndex);
            Mast Physical(int index)
            {
                if (
                    !masts.TryGetValue(index, out var mast)
                    || mast.onlyStaysails
                    || !mast.GetComponent<CapsuleCollider>()
                )
                    throw new InvalidOperationException($"Missing physical mast {index}.");
                return mast;
            }
            // Exclusions may name native stays as well as physical mast sections.
            // Supporting endpoints and required sections still use Physical above.
            BoatPartOption ExcludedOption(int index)
            {
                if (!masts.TryGetValue(index, out var mast) || !mast.GetComponent<BoatPartOption>())
                    throw new InvalidOperationException($"Missing excluded rig option {index}.");
                return mast.GetComponent<BoatPartOption>();
            }
            return profile
                .Stays.Select(group =>
                    group
                        .Variants.Select(definition =>
                        {
                            if (
                                !masts.TryGetValue(definition.Donor, out var donor)
                                || !donor.onlyStaysails
                                || !donor.walkColMast
                                || !boat.walkCol
                                || (
                                    boat.masts.Length > definition.MountIndex
                                    && boat.masts[definition.MountIndex]
                                )
                            )
                                throw new InvalidOperationException(
                                    $"Missing donor {definition.Donor} or occupied mount {definition.MountIndex}."
                                );
                            var fore = Physical(definition.Fore);
                            var aft = Physical(definition.Aft);
                            var guides = definition.ExtendedGuide
                                ? aft.mastReefAttExtension
                                : aft.mastReefAtt;
                            if (
                                guides == null
                                || definition.GuideIndex >= guides.Length
                                || !guides[definition.GuideIndex]
                            )
                                throw new InvalidOperationException(
                                    $"Missing authored halyard guide on mast {definition.Aft}."
                                );
                            CheckPoint(fore, definition.ForePoint);
                            CheckPoint(aft, definition.AftPoint);
                            FishermansStayGeometry.Span(
                                aft.transform.TransformPoint(definition.AftPoint),
                                fore.transform.TransformPoint(definition.ForePoint)
                            );
                            return new FishermansStayReferences
                            {
                                Definition = definition,
                                Donor = donor,
                                Fore = fore,
                                Aft = aft,
                                Guide = guides[definition.GuideIndex],
                                Required = definition
                                    .Required.Select(i =>
                                        Physical(i).GetComponent<BoatPartOption>()
                                    )
                                    .ToArray(),
                                Forbidden = definition.Forbidden.Select(ExcludedOption).ToArray(),
                            };
                        })
                        .ToArray()
                )
                .ToArray();
        }

        private static void CheckPoint(Mast mast, Vector3 point)
        {
            var spar = mast.GetComponent<CapsuleCollider>();
            if (
                !FishermansStayGeometry.OnSpar(
                    point,
                    spar.center,
                    spar.direction,
                    spar.height,
                    spar.radius
                )
            )
                throw new InvalidOperationException(
                    $"Authored attachment no longer meets mast {mast.orderIndex}."
                );
        }
    }
}
