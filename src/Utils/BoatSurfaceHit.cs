using UnityEngine;

namespace MoreSailwindSails.Utils
{
    // Retains the winning surface hit and its mapping into the visible world.
    internal sealed class BoatSurfaceHit
    {
        internal BoatRefs Boat;
        internal RaycastHit Hit;
        internal Matrix4x4 HitToWorld;
        internal bool WalkingModel;
        internal float Distance;
    }
}
