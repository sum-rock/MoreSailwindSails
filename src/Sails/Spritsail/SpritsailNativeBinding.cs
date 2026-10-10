using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Retains the native binding slot and resolves its assigned guides without borrowing fittings.
    internal sealed class SpritsailNativeBinding : MonoBehaviour
    {
        internal Mast Mast { get; private set; }
        internal int Slot { get; private set; }
        internal string Error { get; private set; }
        private Transform lower,
            upper;

        internal static SpritsailNativeBinding For(Sail sail) =>
            sail.GetComponent<SpritsailNativeBinding>()
            ?? sail.gameObject.AddComponent<SpritsailNativeBinding>();

        internal static bool IsSpritsail(Sail sail) =>
            sail
            && (
                sail.GetComponent<LooseFootedSpritsail.LooseFootedSpritsailRig>()
                || sail.GetComponent<BoomedSpritsail.BoomedSpritsailRig>()
            );

        internal static Transform Attachment(Transform[] items, int index) =>
            SpritsailControlSlots.At(items: items, index: index);

        internal static bool TryGuides(
            BoatRefs boat,
            CapsuleCollider mast,
            Transform[] primary,
            Transform[] extensions,
            int slot,
            out Transform lower,
            out Transform upper
        )
        {
            lower = Attachment(items: primary, index: slot);
            upper = Attachment(items: extensions, index: slot);
            if (!upper)
                upper = lower;
            if (
                !lower
                || !upper
                || !lower.gameObject.activeInHierarchy
                || !upper.gameObject.activeInHierarchy
            )
                return false;
            if (
                SpritsailMastAlignment.ExtensionIsLower(
                    primary: boat.transform.InverseTransformPoint(position: lower.position),
                    extension: boat.transform.InverseTransformPoint(position: upper.position),
                    boatLocalAxis: boat.transform.InverseTransformVector(
                        vector: mast.transform.TransformVector(
                            vector: SpritsailMastAlignment.LocalAxis(direction: mast.direction)
                        )
                    )
                )
            )
            {
                var swap = lower;
                lower = upper;
                upper = swap;
            }
            return true;
        }

        internal void Capture(Mast mast, int slot, string error)
        {
            var nextLower = Attachment(items: mast.mastReefAtt, index: slot);
            var nextUpper = Attachment(items: mast.mastReefAttExtension, index: slot);
            bool changed =
                Mast != mast
                || Slot != slot
                || Error != error
                || lower != nextLower
                || upper != nextUpper;
            if (error != null && (Mast != mast || Error != error))
                Plugin.Log.LogWarning(
                    data: $"Spritsail native binding unavailable: boat={mast.GetComponentInParent<BoatRefs>()?.name}, mast={mast.orderIndex}, sail={name}, slot={slot}: {error}"
                );
            Mast = mast;
            Slot = slot;
            Error = error;
            lower = nextLower;
            upper = nextUpper;
            if (changed)
            {
                GetComponent<LooseFootedSpritsail.LooseFootedSpritsailRigging>()?.Invalidate();
                GetComponent<BoomedSpritsail.BoomedSpritsailRigging>()?.Invalidate();
            }
        }

        internal static BoatPartOption[] SupportParts(Mast mast, Transform lower, Transform upper)
        {
            var targets = new[] { mast.transform, lower, upper };
            return mast.GetComponentInParent<BoatRefs>()
                .GetComponentsInChildren<BoatPartOption>(includeInactive: true)
                .Where(predicate: option =>
                    targets.Any(predicate: target =>
                        target.IsChildOf(parent: option.transform)
                        || (
                            option.childMast && target.IsChildOf(parent: option.childMast.transform)
                        )
                    )
                )
                .ToArray();
        }
    }
}
