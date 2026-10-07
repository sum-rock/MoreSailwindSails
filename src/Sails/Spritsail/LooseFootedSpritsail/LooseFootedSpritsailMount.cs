using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail
{
    // Records the carrying mast, its native guides and their actual supporting parts.
    internal sealed class LooseFootedSpritsailMount
    {
        internal BoatRefs Boat;
        internal Mast Mast;
        internal Transform Guide,
            LowerGuide;
        internal BoatPartOption[] Parts;
        internal bool Active =>
            Mast
            && Mast.gameObject.activeInHierarchy
            && LowerGuide
            && LowerGuide.gameObject.activeInHierarchy
            && Guide
            && Guide.gameObject.activeInHierarchy
            && Parts.All(predicate: p => p && p.gameObject.activeInHierarchy);
    }
}
