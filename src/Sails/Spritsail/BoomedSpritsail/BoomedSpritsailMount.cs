using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Records the carrying mast, its native guides and their actual supporting parts.
    internal sealed class BoomedSpritsailMount
    {
        internal BoatRefs Boat;
        internal Mast Mast;
        internal Transform Guide,
            LowerGuide;
        internal BoatPartOption[] Parts;
        internal bool Active
        {
            get
            {
                if (
                    !Mast
                    || !Mast.gameObject.activeInHierarchy
                    || !LowerGuide
                    || !LowerGuide.gameObject.activeInHierarchy
                    || !Guide
                    || !Guide.gameObject.activeInHierarchy
                )
                    return false;
                if (Parts == null)
                    throw new ArgumentNullException(paramName: nameof(Parts));
                for (int i = 0; i < Parts.Length; i++)
                    if (!Parts[i] || !Parts[i].gameObject.activeInHierarchy)
                        return false;
                return true;
            }
        }
    }
}
