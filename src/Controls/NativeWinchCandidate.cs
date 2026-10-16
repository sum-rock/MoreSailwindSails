using UnityEngine;

namespace MoreSailwindSails.Controls
{
    // Retains the source slot needed to validate a placement without searching alternatives.
    internal sealed class NativeWinchCandidate : WinchCandidate
    {
        internal GPButtonRopeWinch[] Templates;
        internal Vector3? TemplateScale;
        internal Mast SourceMast;
        internal int SourceIndex;
        internal string Context;
    }
}
