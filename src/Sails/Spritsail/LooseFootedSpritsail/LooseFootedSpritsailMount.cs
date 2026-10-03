using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail
{
    // Records the carrying mast, its active upper guide and protected ancestry.
    internal sealed class LooseFootedSpritsailMount
    {
        internal BoatRefs Boat;
        internal Mast Mast;
        internal Transform Guide;
        internal Mast[] Sections;
        internal bool Active =>
            Mast
            && Guide
            && Guide.gameObject.activeInHierarchy
            && Sections.All(predicate: m => m && m.gameObject.activeInHierarchy);
    }
}
