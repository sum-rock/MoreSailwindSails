using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Retains one mast-local surface query per caller-selected attachment, including scale in the ray.
    internal struct SpritsailSurfaceSample
    {
        private Vector3 previousOrigin;
        private Vector3 previousDirection;
        private float previousRadius;
        private bool cached;

        internal bool TryGet(Vector3 origin, Vector3 direction, out float radius)
        {
            radius = previousRadius;
            return cached
                && (origin - previousOrigin).sqrMagnitude < 1e-8f
                && (direction - previousDirection).sqrMagnitude < 1e-8f;
        }

        internal void Store(Vector3 origin, Vector3 direction, float radius)
        {
            previousOrigin = origin;
            previousDirection = direction;
            previousRadius = radius;
            cached = true;
        }
    }
}
