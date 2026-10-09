using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Retains the maximum of a mast-local sampling ring until its physical inputs change.
    internal struct SpritsailMountEnvelopeSample
    {
        private Vector3 origin;
        private Vector3 right;
        private Vector3 forward;
        private float fallback;
        private float radius;
        private bool cached;

        internal bool TryGet(
            Vector3 currentOrigin,
            Vector3 currentRight,
            Vector3 currentForward,
            float currentFallback,
            out float result
        )
        {
            result = radius;
            return cached
                && fallback == currentFallback
                && (currentOrigin - origin).sqrMagnitude < 1e-8f
                && (currentRight - right).sqrMagnitude + (currentForward - forward).sqrMagnitude
                    < 1e-8f;
        }

        internal void Store(
            Vector3 currentOrigin,
            Vector3 currentRight,
            Vector3 currentForward,
            float currentFallback,
            float result
        )
        {
            origin = currentOrigin;
            right = currentRight;
            forward = currentForward;
            fallback = currentFallback;
            radius = result;
            cached = true;
        }
    }
}
