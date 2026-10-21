// Defines a shared native donor and two authored sheet fallback seats.
using System;
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration
{
    // Keeps donor selection and mounting measurements together for both sheet controls.
    internal sealed class SheetFallbackPair
    {
        internal readonly SheetFallbackSeat Port,
            Starboard;
        internal readonly int TemplateMast,
            TemplateIndex;
        internal readonly WinchRole TemplateRole;
        internal readonly Vector3 SourceNormal;
        internal readonly float Offset;

        internal SheetFallbackPair(
            int templateMast,
            WinchRole templateRole,
            int templateIndex,
            Vector3 sourceNormal,
            float offset,
            SheetFallbackSeat port,
            SheetFallbackSeat starboard
        )
        {
            if (
                templateMast < 0
                || templateMast >= 128
                || templateIndex < 0
                || (
                    templateRole != WinchRole.Reef
                    && templateRole != WinchRole.Left
                    && templateRole != WinchRole.Right
                )
                || !Finite(value: sourceNormal)
                || sourceNormal.sqrMagnitude <= 0.000001f
                || float.IsNaN(offset)
                || float.IsInfinity(offset)
                || offset < 0f
            )
                throw new ArgumentException(
                    "Invalid sheet fallback donor or mounting measurements."
                );
            TemplateMast = templateMast;
            TemplateRole = templateRole;
            TemplateIndex = templateIndex;
            SourceNormal = sourceNormal;
            Offset = offset;
            Port = port;
            Starboard = starboard;
        }

        internal string InvalidSide =>
            Port == null || !Port.Valid ? "port"
            : Starboard == null || !Starboard.Valid ? "starboard"
            : null;

        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x)
            && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y)
            && !float.IsInfinity(value.y)
            && !float.IsNaN(value.z)
            && !float.IsInfinity(value.z);
    }
}
