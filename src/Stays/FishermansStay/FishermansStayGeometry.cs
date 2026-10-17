using System;
using UnityEngine;

namespace MoreSailwindSails.Stays.FishermansStay
{
    internal static class FishermansStayGeometry
    {
        internal const int MountCapacity = 256;
        internal const float PreferredAngle = 70f;

        // Keep the native guide's radial position, moving only its mast-axis height.
        internal static Vector3 AlignGuideHeight(Vector3 guide, Vector3 head, Vector3 mastAxis) =>
            guide + mastAxis.normalized * Vector3.Dot(head - guide, mastAxis.normalized);

        internal static float Span(Vector3 aft, Vector3 fore)
        {
            float span = (aft - fore).magnitude;
            if (float.IsNaN(span) || float.IsInfinity(span) || span < 0.25f)
                throw new ArgumentException("Stay endpoints must be finite and on separate masts.");
            return span;
        }

        // Keep the authored cross-section; only the rope's length follows its mounts.
        internal static Vector3 RopeScale(float span) =>
            new Vector3(1, 1, span / FishermansStayBakedMesh.RopeLength);

        // Existing mast collision dimensions provide a simple, shared sizing rule.
        internal static Vector3 CollarScale(float radius) =>
            new Vector3(
                radius / FishermansStayBakedMesh.CollarInnerRadius,
                radius / FishermansStayBakedMesh.CollarInnerRadius,
                1
            );

        internal static Vector3 FrameUp(Vector3 up, Vector3 forward)
        {
            up -= forward * Vector3.Dot(up, forward);
            if (up.sqrMagnitude < 0.001f)
                throw new ArgumentException("The stay has no usable roll reference.");
            return up.normalized;
        }

        // Validation only: endpoints are authored in stay configuration, never generated
        // from a runtime mast search. Accept physical masthead fallback angles.
        internal static bool OnSpar(
            Vector3 point,
            Vector3 center,
            int direction,
            float height,
            float radius
        )
        {
            var delta = point - center;
            float along =
                direction == 0 ? delta.x
                : direction == 1 ? delta.y
                : delta.z;
            float radial = delta.sqrMagnitude - along * along;
            return Math.Abs(along) <= height * 0.5f + 0.02f
                && radial <= (radius + 0.02f) * (radius + 0.02f);
        }
    }
}
