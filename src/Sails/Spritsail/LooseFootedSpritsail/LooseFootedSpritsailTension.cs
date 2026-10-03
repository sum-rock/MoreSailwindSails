using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail
{
    // Fits the clew to coupled foot and leech length budgets.
    internal static class LooseFootedSpritsailTension
    {
        // The clew lies on the intersection of two spheres: distance from the
        // tack preserves foot tension, distance from the head budgets leech
        // length. Select the point nearest the native sheet-requested pose.
        internal static bool Fit(
            Vector3 requested,
            Vector3 head,
            Vector3 tack,
            Vector3 bow,
            float leechLength,
            float footLength,
            Vector3[] points
        )
        {
            float distance = (tack - head).magnitude;
            // The rest mesh already contains camber reserve. Preserve projected
            // edge budgets exactly so entering Cloth does not jump the clew.
            float footRadius = footLength;
            float arcLength = leechLength;
            if (distance <= 1e-8f || arcLength <= 1e-8f || footRadius <= 1e-8f)
            {
                Fill(clew: head, head: head, bow: Vector3.zero, points: points);
                return false;
            }
            var axis = (tack - head) / distance;
            float low = Math.Abs(value: distance - footRadius);
            float high = Math.Min(val1: arcLength, val2: distance + footRadius);
            if (low > high)
            {
                // Impossible installation geometry: retain the fixed head and
                // a finite leech. Caller can report this without breaking load.
                Fill(
                    clew: head + axis * Math.Min(val1: arcLength, val2: distance),
                    head: head,
                    bow: Vector3.zero,
                    points: points
                );
                return false;
            }
            var radial = requested - head;
            radial -= axis * Vector3.Dot(radial, axis);
            if (radial.sqrMagnitude < distance * distance * 1e-12f)
            {
                radial = Vector3.Cross(axis, Vector3.up);
                if (radial.sqrMagnitude < 1e-8f)
                    radial = Vector3.Cross(axis, Vector3.forward);
            }
            radial = radial.normalized;
            var nearest = OnCircle(
                head: head,
                axis: axis,
                radial: radial,
                distance: distance,
                leechRadius: low,
                footRadius: footRadius
            );
            // Curve shape yields to available cloth, never foot tension. This
            // reduction is normally unnecessary but handles extreme bow inputs.
            for (int i = 0; i < 16 && Length(clew: nearest, head: head, bow: bow) > arcLength; i++)
                bow *= 0.5f;
            if (Length(clew: nearest, head: head, bow: bow) > arcLength)
                bow = Vector3.zero;
            for (int i = 0; i < 24; i++)
            {
                float radius = (low + high) * 0.5f;
                var candidate = OnCircle(
                    head: head,
                    axis: axis,
                    radial: radial,
                    distance: distance,
                    leechRadius: radius,
                    footRadius: footRadius
                );
                if (Length(clew: candidate, head: head, bow: bow) > arcLength)
                    high = radius;
                else
                    low = radius;
            }
            var clew = OnCircle(
                head: head,
                axis: axis,
                radial: radial,
                distance: distance,
                leechRadius: low,
                footRadius: footRadius
            );
            Fill(clew: clew, head: head, bow: bow, points: points);
            return true;
        }

        private static Vector3 OnCircle(
            Vector3 head,
            Vector3 axis,
            Vector3 radial,
            float distance,
            float leechRadius,
            float footRadius
        )
        {
            double along =
                (
                    (double)leechRadius * leechRadius
                    - (double)footRadius * footRadius
                    + (double)distance * distance
                ) / (2 * distance);
            float radius = (float)
                Math.Sqrt(
                    d: Math.Max(val1: 0, val2: (double)leechRadius * leechRadius - along * along)
                );
            return head + axis * (float)along + radial * radius;
        }

        private static float Length(Vector3 clew, Vector3 head, Vector3 bow)
        {
            float length = 0;
            var previous = clew;
            for (int i = 1; i <= 64; i++)
            {
                var point = LooseFootedSpritsailBillow.SupportPoint(
                    clew: clew,
                    head: head,
                    bow: bow,
                    t: i / 64f
                );
                length += (point - previous).magnitude;
                previous = point;
            }
            return length;
        }

        private static void Fill(Vector3 clew, Vector3 head, Vector3 bow, Vector3[] points)
        {
            float length = Length(clew: clew, head: head, bow: bow);
            points[0] = clew;
            var previous = clew;
            float travelled = 0;
            int next = 1;
            for (int step = 1; step <= 64 && next < points.Length - 1; step++)
            {
                var point = LooseFootedSpritsailBillow.SupportPoint(
                    clew: clew,
                    head: head,
                    bow: bow,
                    t: step / 64f
                );
                float segment = (point - previous).magnitude;
                while (
                    next < points.Length - 1
                    && length * next / (points.Length - 1) <= travelled + segment
                )
                {
                    float fraction =
                        segment > 1e-8f
                            ? (length * next / (points.Length - 1) - travelled) / segment
                            : 0;
                    points[next++] = LooseFootedSpritsailBillow.SupportPoint(
                        clew: clew,
                        head: head,
                        bow: bow,
                        t: (step - 1 + fraction) / 64
                    );
                }
                travelled += segment;
                previous = point;
            }
            while (next < points.Length)
                points[next++] = head;
        }
    }
}
