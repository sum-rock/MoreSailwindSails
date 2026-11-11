// Applies luff-only fitting compatibility to staysails paired with gaffs or spritsails.
using MoreSailwindSails.BoatConfiguration;
using MoreSailwindSails.Sails.LooseFootedSpritsail;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal static class FishermansStaysailLuffCompatibility
    {
        internal static bool TryOverlap(Sail first, Sail second, out bool overlap)
        {
            overlap = false;
            if (!first || !second || first == second)
                return false;
            var staysail = first.GetComponent<FishermansStaysailRig>() ? first : second;
            var other = staysail == first ? second : first;
            var rig = staysail.GetComponent<FishermansStaysailRig>();
            if (!rig || !IsMastSail(sail: other))
                return false;
            var mount = staysail.transform.parent
                ? staysail.transform.parent.GetComponent<Mast>()
                : null;
            var otherMast = other.transform.parent
                ? other.transform.parent.GetComponent<Mast>()
                : null;
            if (
                !otherMast
                || !FishermansStaysailRigging.TryResolve(mount: mount, pair: out var pair)
                || otherMast.GetComponentInParent<BoatRefs>() != pair.Boat
                || !FishermansStaysailInstallationGeometry.SharesLuffMast(
                    boat: BoatCatalog.Find(boatName: pair.Boat.name),
                    forwardSection: pair.Fore.orderIndex,
                    squareSection: otherMast.orderIndex
                )
                || !rig.RefreshFrame()
            )
                return false;

            var binding = FishermansStaysailRigging.For(sail: staysail);
            binding.ForeSailFrame(point: out var head, axis: out var axis);
            float otherTop = other.GetCurrentInstallHeight() - otherMast.mastHeight;
            overlap = FishermansStaysailInstallationGeometry.LuffsOverlap(
                firstHead: head,
                firstFoot: head - axis * staysail.GetScaledHeight(),
                secondHead: otherMast.transform.TransformPoint(
                    position: new Vector3(x: 0, y: 0, z: otherTop)
                ),
                secondFoot: otherMast.transform.TransformPoint(
                    position: new Vector3(x: 0, y: 0, z: otherTop - other.GetScaledHeight())
                ),
                axis: axis
            );
            return true;
        }

        internal static bool HasOverlap(Sail sail, Mast mast)
        {
            if (!sail || (!sail.GetComponent<FishermansStaysailRig>() && !IsMastSail(sail: sail)))
                return false;
            var boat = mast ? mast.GetComponentInParent<BoatRefs>() : null;
            if (!boat || boat.masts == null)
                return false;
            // Stay mounts and physical mast sections have separate native sail lists.
            // Include pending fittings as well as installed sails, in either order.
            foreach (var candidate in boat.masts)
            {
                if (
                    !candidate
                    || !candidate.gameObject.activeInHierarchy
                    || candidate.sails == null
                )
                    continue;
                foreach (var item in candidate.sails)
                    if (
                        item
                        && item.activeInHierarchy
                        && TryOverlap(
                            first: sail,
                            second: item.GetComponent<Sail>(),
                            overlap: out bool overlap
                        )
                        && overlap
                    )
                        return true;
            }
            return false;
        }

        private static bool IsMastSail(Sail sail) =>
            sail.category == SailCategory.gaff || sail.GetComponent<LooseFootedSpritsailRig>();
    }
}
