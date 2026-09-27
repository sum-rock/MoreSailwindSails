using System;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    // Physical variants and sheet-source rigs are deliberately separate. Labels
    // describe audited shipyard groups; runtime lookup never parses display names.
    internal sealed class SheetWinchCategory
    {
        internal readonly string Name;
        internal readonly int[] PhysicalMasts;
        internal readonly NativeSheetSource[] Sources;
        internal readonly string FallbackName;
        internal readonly SheetFallbackPair Fallback;

        internal SheetWinchCategory(
            string name,
            int[] physicalMasts,
            int[] sources,
            SheetFallbackPair fallback = null
        )
        {
            Name = name;
            PhysicalMasts = physicalMasts;
            Sources = sources.Select(id => new NativeSheetSource(id)).ToArray();
            FallbackName = name + "Fallback";
            Fallback = fallback;
        }
    }

    internal sealed class NativeSheetSource
    {
        internal readonly int Mast;

        // Null means corresponding indices, verified by the installed inventory.
        // Exceptions can explicitly author left/right indices without synthesizing seats.
        internal readonly int[][] Pairs;

        internal NativeSheetSource(int mast, int[][] pairs = null)
        {
            Mast = mast;
            Pairs = pairs;
        }
    }

    internal sealed class SheetFallbackPair
    {
        internal readonly SheetFallbackSeat Port,
            Starboard;

        internal SheetFallbackPair(SheetFallbackSeat port, SheetFallbackSeat starboard)
        {
            Port = port;
            Starboard = starboard;
        }

        internal string InvalidSide =>
            Port == null || !Port.Valid ? "port"
            : Starboard == null || !Starboard.Valid ? "starboard"
            : null;
    }

    internal sealed class SheetFallbackSeat
    {
        internal readonly Vector3 Contact,
            Normal,
            SourceNormal;
        internal readonly float Offset;
        internal readonly int TemplateMast,
            TemplateIndex;
        internal readonly WinchRole TemplateRole;
        internal readonly int[] Supports;

        internal SheetFallbackSeat(
            Vector3 contact,
            Vector3 normal,
            Vector3 sourceNormal,
            float offset,
            int templateMast,
            WinchRole templateRole,
            int templateIndex,
            params int[] supports
        )
        {
            Contact = contact;
            Normal = normal;
            SourceNormal = sourceNormal;
            Offset = offset;
            TemplateMast = templateMast;
            TemplateRole = templateRole;
            TemplateIndex = templateIndex;
            Supports = supports;
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
