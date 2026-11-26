using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    // Keeps the authored sail normal facing the apparent wind for native force magnitude.
    internal static class FishermansStaysailAerodynamics
    {
        internal static Vector3 ForceDirection(Vector3 wind, Vector3 normal) =>
            Vector3.Dot(wind, normal) >= 0 ? normal : -normal;
    }
}
