using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail
{
    // Carries the posed aerodynamic center and orthogonal sail axes.
    internal struct LooseFootedSpritsailWindFrame
    {
        internal Vector3 AlongSail;
        internal Vector3 MastAxis;
        internal Vector3 Normal;
        internal Vector3 Center;
    }
}
