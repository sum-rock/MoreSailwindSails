using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Carries the posed aerodynamic center and orthogonal sail axes.
    internal struct BoomedSpritsailWindFrame
    {
        internal Vector3 AlongSail;
        internal Vector3 MastAxis;
        internal Vector3 Normal;
        internal Vector3 Center;
    }
}
