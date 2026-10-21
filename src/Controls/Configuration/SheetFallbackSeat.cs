// Defines immutable sheet fallback seat data.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.Controls.Configuration
{
    // Aligns an authored surface contact with its pair's donor mounting frame.
    internal sealed class SheetFallbackSeat
    {
        internal readonly Vector3 Contact,
            Normal;
        internal readonly IReadOnlyList<int> Supports;

        internal SheetFallbackSeat(
            Vector3 contact,
            Vector3 normal,
            IEnumerable<int> supports = null
        )
        {
            var supportsCopy = (supports ?? Array.Empty<int>()).ToArray();
            Contact = contact;
            Normal = normal;
            Supports = Array.AsReadOnly(supportsCopy);
        }

        internal bool Valid => Finite(Contact) && Finite(Normal) && Normal.sqrMagnitude > 0.000001f;

        internal Vector3 Position(float offset) => Contact + Normal.normalized * offset;

        internal Quaternion Rotation(Vector3 sourceNormal, Quaternion templateRotation) =>
            Quaternion.FromToRotation(sourceNormal, Normal) * templateRotation;

        private static bool Finite(Vector3 v) =>
            !float.IsNaN(v.x)
            && !float.IsInfinity(v.x)
            && !float.IsNaN(v.y)
            && !float.IsInfinity(v.y)
            && !float.IsNaN(v.z)
            && !float.IsInfinity(v.z);
    }
}
