// Adds source templates and live donor references to native or authored placements.
using UnityEngine;

namespace MoreSailwindSails.Controls.Placement
{
    // Retains the source slot needed to validate a placement without searching alternatives.
    internal sealed class WinchPlacementCandidate : WinchCandidate
    {
        internal GPButtonRopeWinch[] Templates;
        internal Vector3? TemplateScale;
        internal Mast SourceMast;
        internal int SourceIndex;
        internal string Context;
    }
}
