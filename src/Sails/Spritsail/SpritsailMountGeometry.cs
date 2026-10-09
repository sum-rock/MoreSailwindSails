using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Fits the authored edging to an existing luff and its rope loops to the physical mast.
    internal static class SpritsailMountGeometry
    {
        internal const float SeamOverlap = 0.002f;

        // The authored loop is an ellipse in Y/Z, centered at Z=0.23.
        // Its nose at Z=-0.045 passes through the eyelet centered at -0.05.
        private const float SourceCenter = 0.23f;
        private const float SourceSideRadius = 0.2f;
        private const float SourceLongRadius = 0.275f;

        internal static Vector3 Luff(Vector3[] points, float sourceX)
        {
            float row = Mathf.Clamp01(value: (3 - sourceX) / 6) * (points.Length - 1);
            int first = Math.Min(points.Length - 2, (int)row);
            return Vector3.Lerp(a: points[first], b: points[first + 1], t: row - first);
        }

        internal static Vector3 Eyelet(Vector3[] points, float sourceX) =>
            Luff(points: points, sourceX: sourceX) + Vector3.forward * (SeamOverlap - 0.05f);

        internal static Vector3 Strip(Vector3 source, Vector3[] luff)
        {
            var point =
                Luff(points: luff, sourceX: source.x)
                - Vector3.up * source.y
                + Vector3.forward * (SeamOverlap - source.z - 0.1f);
            // Keep each hole at the rigid eyelet's size while stretching the cloth
            // between fittings. The influence ends before the seam meets live Cloth.
            foreach (float x in SpritsailMountAsset.EyeletX)
            {
                float distance = Mathf.Sqrt(
                    f: (source.x - x) * (source.x - x) + (source.z + 0.05f) * (source.z + 0.05f)
                );
                if (distance >= 0.049f)
                    continue;
                float blend = Mathf.Clamp01(value: (distance - 0.025f) / 0.024f);
                blend = blend * blend * (3 - 2 * blend);
                var rigid =
                    Eyelet(points: luff, sourceX: x)
                    + new Vector3(source.x - x, -source.y, -(source.z + 0.05f));
                return Vector3.Lerp(a: rigid, b: point, t: blend);
            }
            return point;
        }

        internal static void Fit(
            Vector3[] luff,
            Vector3 mastOrigin,
            Vector3 mastAxis,
            float[] radii,
            Vector3[] vertices,
            Vector3[] normals
        )
        {
            for (int i = 0; i < SpritsailMountAsset.Positions.Length; i++)
            {
                int part = SpritsailMountAsset.Parts[i];
                var source = SpritsailMountAsset.Positions[i];
                var normal = SpritsailMountAsset.Normals[i];
                if (part == 0)
                {
                    vertices[i] = Strip(source: source, luff: luff);
                    var stripTangent =
                        Strip(source: source + Vector3.right * 0.001f, luff: luff)
                        - Strip(source: source - Vector3.right * 0.001f, luff: luff);
                    var transverse =
                        Strip(source: source + Vector3.forward * 0.001f, luff: luff)
                        - Strip(source: source - Vector3.forward * 0.001f, luff: luff);
                    // Source Z runs toward the mast; runtime cloth Z runs aft.
                    normals[i] = (
                        Vector3.Cross(lhs: transverse, rhs: stripTangent) / 0.000004f
                    ).normalized;
                    if (normal.y < 0)
                        normals[i] = -normals[i];
                    continue;
                }
                int eye = part <= 7 ? part - 1 : part - 8;
                float centerX = SpritsailMountAsset.EyeletX[eye];
                var eyelet = Eyelet(points: luff, sourceX: centerX);
                if (part <= 7)
                {
                    // Reflect both Y and Z to preserve winding while the edging extends forward.
                    vertices[i] =
                        eyelet + new Vector3(source.x - centerX, -source.y, -(source.z + 0.05f));
                    normals[i] = new Vector3(normal.x, -normal.y, -normal.z);
                    continue;
                }
                var center =
                    mastOrigin + mastAxis * Vector3.Dot(lhs: eyelet - mastOrigin, rhs: mastAxis);
                var outward = (eyelet - center).normalized;
                var side = Vector3.Cross(lhs: mastAxis, rhs: outward).normalized;
                var local = source - new Vector3(centerX, 0, 0);
                // Separate the tube thickness from the fitted path so mast size does not
                // stretch its cross-section. The supplied smooth normals describe that tube.
                float ropeRadius = SpritsailMountAsset.RopeRadii[eye];
                var path = local - normal * ropeRadius;
                float angle = (float)
                    Math.Atan2(
                        path.y / SourceSideRadius,
                        (SourceCenter - path.z) / SourceLongRadius
                    );
                var point = RopePoint(
                    angle: angle,
                    eyelet: eyelet,
                    center: center,
                    axis: mastAxis,
                    outward: outward,
                    side: side,
                    radius: radii[eye],
                    ropeRadius: ropeRadius
                );
                var tangent = (
                    RopePoint(
                        angle: angle + 0.001f,
                        eyelet: eyelet,
                        center: center,
                        axis: mastAxis,
                        outward: outward,
                        side: side,
                        radius: radii[eye],
                        ropeRadius: ropeRadius
                    )
                    - RopePoint(
                        angle: angle - 0.001f,
                        eyelet: eyelet,
                        center: center,
                        axis: mastAxis,
                        outward: outward,
                        side: side,
                        radius: radii[eye],
                        ropeRadius: ropeRadius
                    )
                ).normalized;
                var radial = Vector3.Cross(lhs: tangent, rhs: mastAxis).normalized;
                var sourceRadial = new Vector3(
                    0,
                    path.y / (SourceSideRadius * SourceSideRadius),
                    (path.z - SourceCenter) / (SourceLongRadius * SourceLongRadius)
                ).normalized;
                var sourceTangent = Vector3.Cross(lhs: Vector3.right, rhs: sourceRadial);
                normals[i] = (
                    mastAxis * normal.x
                    + radial * Vector3.Dot(lhs: normal, rhs: sourceRadial)
                    + tangent * Vector3.Dot(lhs: normal, rhs: sourceTangent)
                ).normalized;
                vertices[i] = point + mastAxis * path.x + normals[i] * ropeRadius;
            }
        }

        private static Vector3 RopePoint(
            float angle,
            Vector3 eyelet,
            Vector3 center,
            Vector3 axis,
            Vector3 outward,
            Vector3 side,
            float radius,
            float ropeRadius
        )
        {
            float fitted = radius + ropeRadius * 2;
            if (Math.Abs(angle) >= Math.PI / 2)
                return center
                    + (outward * (float)Math.Cos(angle) + side * (float)Math.Sin(angle)) * fitted;
            float sign = angle < 0 ? -1 : 1;
            float t = (float)(Math.Abs(angle) / (Math.PI / 2));
            float rest = 1 - t;
            // The two approach legs pass through the hole normal, then meet the
            // mast collar tangentially. Preserve a short passage before curving.
            var a = eyelet;
            var b = eyelet - Vector3.up * (sign * 0.02f);
            var d = center + side * (sign * fitted);
            var c = d + outward * (fitted * 0.55f);
            var point =
                a * (rest * rest * rest)
                + b * (3 * rest * rest * t)
                + c * (3 * rest * t * t)
                + d * (t * t * t);
            var axialCenter = center + axis * Vector3.Dot(lhs: point - center, rhs: axis);
            var radial = point - axialCenter;
            if (radial.sqrMagnitude < fitted * fitted)
                point =
                    axialCenter
                    + (radial.sqrMagnitude > 0.000001f ? radial.normalized : outward) * fitted;
            return point;
        }
    }
}
