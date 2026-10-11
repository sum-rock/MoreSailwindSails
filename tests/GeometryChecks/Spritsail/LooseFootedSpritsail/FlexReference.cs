using System;
using MoreSailwindSails.Sails.Spritsail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail.LooseFootedSpritsail
{
    // Frozen pre-optimization solver for differential correctness checks and offline benchmarks.
    internal static class FlexReference
    {
        internal const float MaximumDisplacement = 0.15f;

        private static float Clamp(float value) => Math.Max(0, Math.Min(1, value));

        internal static float Load(float paidOut, float routed)
        {
            if (
                !SpritsailDeployment.Finite(value: paidOut)
                || !SpritsailDeployment.Finite(value: routed)
                || paidOut <= 1e-6f
                || routed <= 0
            )
                return 0;
            // Only the last 10% of slack develops a shaping load; never write native rope state.
            return Clamp(value: (routed / paidOut - 0.9f) / 0.1f);
        }

        internal static Vector3 Pull(
            Vector3 clew,
            Vector3 port,
            float portLoad,
            Vector3 starboard,
            float starboardLoad
        )
        {
            if (!Finite(value: clew))
                return Vector3.zero;
            portLoad =
                Finite(value: port) && SpritsailDeployment.Finite(value: portLoad)
                    ? Clamp(value: portLoad)
                    : 0;
            starboardLoad =
                Finite(value: starboard) && SpritsailDeployment.Finite(value: starboardLoad)
                    ? Clamp(value: starboardLoad)
                    : 0;
            var result =
                (portLoad > 0 ? (port - clew).normalized * portLoad : Vector3.zero)
                + (
                    starboardLoad > 0 ? (starboard - clew).normalized * starboardLoad : Vector3.zero
                );
            return result / Math.Max(1, portLoad + starboardLoad);
        }

        internal static Vector3 Smooth(Vector3 previous, Vector3 target, float seconds)
        {
            if (!Finite(value: previous))
                previous = Vector3.zero;
            if (!Finite(value: target))
                target = Vector3.zero;
            if (!SpritsailDeployment.Finite(value: seconds))
                seconds = 0;
            return previous
                + (target - previous)
                    * (1 - (float)Math.Exp(-5 * Math.Max(0, Math.Min(0.1f, seconds))));
        }

        internal static float Weight(Vector3 point, Vector3 peak, Vector3 tack, Vector3 clew)
        {
            // Signed XZ distance from the fixed peak-to-tack diagonal, normalized at the clew.
            var edge = tack - peak;
            float denominator = edge.x * (clew.z - peak.z) - edge.z * (clew.x - peak.x);
            if (Math.Abs(denominator) < 1e-8f)
                return 0;
            float t = Clamp(
                value: (edge.x * (point.z - peak.z) - edge.z * (point.x - peak.x)) / denominator
            );
            return t * t;
        }

        internal static float Arc(Vector3[] edge, Vector3 displacement)
        {
            float length = 0;
            var previous = edge[0];
            for (int i = 1; i < edge.Length; i++)
            {
                float t = i / (float)(edge.Length - 1);
                var point = edge[i] + displacement * (t * t);
                length += (point - previous).magnitude;
                previous = point;
            }
            return length;
        }

        internal static Vector3 Fit(Vector3 requested, Vector3[] foot, Vector3[] leech, float limit)
        {
            if (
                !Finite(value: requested)
                || !SpritsailDeployment.Finite(value: limit)
                || limit <= 0
                || foot == null
                || leech == null
                || foot.Length < 2
                || leech.Length < 2
            )
                return Vector3.zero;
            float footBudget = Arc(edge: foot, displacement: Vector3.zero);
            float leechBudget = Arc(edge: leech, displacement: Vector3.zero);
            if (
                !SpritsailDeployment.Finite(value: footBudget)
                || !SpritsailDeployment.Finite(value: leechBudget)
            )
                return Vector3.zero;
            requested = Limit(value: requested, limit: limit);
            if (requested.sqrMagnitude < 1e-12f)
                return Vector3.zero;
            var inward = ((foot[0] + leech[0]) * 0.5f - foot[foot.Length - 1]).normalized;
            var best = Vector3.zero;
            float error = requested.sqrMagnitude;
            // A little in-plane gathering buys arc length for the transverse curve.
            // For each direction, arc length is convex in displacement, so bisection is safe.
            for (int candidate = 0; candidate <= 16; candidate++)
            {
                var direction = Limit(
                    value: requested + inward * (requested.magnitude * candidate / 16f),
                    limit: limit
                );
                if (Vector3.Dot(direction, requested) <= 0)
                    continue;
                float low = 0,
                    high = 1;
                for (int step = 0; step < 18; step++)
                {
                    float t = (low + high) * 0.5f;
                    var delta = direction * t;
                    if (
                        Arc(edge: foot, displacement: delta) <= footBudget
                        && Arc(edge: leech, displacement: delta) <= leechBudget
                    )
                        low = t;
                    else
                        high = t;
                }
                var fitted = direction * low;
                float nextError = (fitted - requested).sqrMagnitude;
                if (nextError < error)
                {
                    best = fitted;
                    error = nextError;
                }
            }
            return best;
        }

        private static Vector3 Limit(Vector3 value, float limit) =>
            value.magnitude > limit ? value.normalized * limit : value;

        private static bool Finite(Vector3 value) =>
            SpritsailDeployment.Finite(value: value.x)
            && SpritsailDeployment.Finite(value: value.y)
            && SpritsailDeployment.Finite(value: value.z);
    }
}
