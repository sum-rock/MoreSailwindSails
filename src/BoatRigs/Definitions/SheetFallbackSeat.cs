using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    // Immutable authored SheetFallbackSeat data, copied at the definition boundary.
    internal sealed class SheetFallbackSeat
    {
        internal readonly Vector3 Contact,
            Normal,
            SourceNormal;
        internal readonly float Offset;
        internal readonly int TemplateMast,
            TemplateIndex;
        internal readonly WinchRole TemplateRole;
        internal readonly IReadOnlyList<int> Supports;

        internal SheetFallbackSeat(
            Vector3 contact,
            Vector3 normal,
            Vector3 sourceNormal,
            float offset,
            int templateMast,
            WinchRole templateRole,
            int templateIndex,
            IEnumerable<int> supports = null
        )
        {
            var supportsCopy = (supports ?? Array.Empty<int>()).ToArray();
            Contact = contact;
            Normal = normal;
            SourceNormal = sourceNormal;
            Offset = offset;
            TemplateMast = templateMast;
            TemplateRole = templateRole;
            TemplateIndex = templateIndex;
            Supports = Array.AsReadOnly(supportsCopy);
        }

        internal bool Valid =>
            Finite(Contact)
            && Finite(Normal)
            && Finite(SourceNormal)
            && Normal.sqrMagnitude > 0.000001f
            && SourceNormal.sqrMagnitude > 0.000001f
            && !float.IsNaN(Offset)
            && !float.IsInfinity(Offset)
            && TemplateMast >= 0
            && TemplateIndex >= 0;

        private static bool Finite(Vector3 v) =>
            !float.IsNaN(v.x)
            && !float.IsInfinity(v.x)
            && !float.IsNaN(v.y)
            && !float.IsInfinity(v.y)
            && !float.IsNaN(v.z)
            && !float.IsInfinity(v.z);
    }
}
