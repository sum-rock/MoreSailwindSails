// Computes cut-independent neutral fitting, collar sizing and spar poses in a Y-up assembly frame.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal static class LooseFootedSpritsailFittingGeometry
    {
        internal const float Clearance = 0.01f;
        internal const float CollarClearance = 0.001f;
        internal const int CollisionStrips = 16;

        // Measure the visible shaft where the collar sits, including tapered mast sections.
        internal static float MastRadius(
            Vector3[] vertices,
            int[] triangles,
            Matrix4x4 matrix,
            Vector3 center,
            Vector3 axis
        )
        {
            float radius = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            for (int edge = 0; edge < 3; edge++)
            {
                var a = matrix.MultiplyPoint3x4(point: vertices[triangles[i + edge]]) - center;
                var b =
                    matrix.MultiplyPoint3x4(point: vertices[triangles[i + (edge + 1) % 3]])
                    - center;
                float da = Vector3.Dot(lhs: a, rhs: axis);
                float db = Vector3.Dot(lhs: b, rhs: axis);
                if ((da > 0 && db > 0) || (da < 0 && db < 0))
                    continue;
                var point = da == db ? a : Vector3.Lerp(a: a, b: b, t: da / (da - db));
                radius = Math.Max(radius, point.magnitude);
            }
            return radius;
        }

        internal static float BaseScale(LooseFootedSpritsailAssetData data, float fullGaffHeight)
        {
            float luff =
                Corner(data: data, index: data.Throat).y - Corner(data: data, index: data.Tack).y;
            if (!Finite(luff) || luff <= 0 || !Finite(fullGaffHeight) || fullGaffHeight <= 0)
                throw new ArgumentException(
                    "Expected positive authored and reference luff heights."
                );
            return fullGaffHeight / luff;
        }

        internal static Vector3 Corner(LooseFootedSpritsailAssetData data, int index) =>
            data.Sail.RestTransform.MultiplyPoint3x4(point: data.Sail.Vertices[index]);

        internal static float CollarRadius(LooseFootedSpritsailAssetData data)
        {
            float radius = data.CollarInnerRadius;
            if (!Finite(radius) || radius <= 0)
                throw new ArgumentException("The snotter requires a measurable inner radius.");
            return radius;
        }

        internal static LooseFootedSpritsailPose Pose(
            LooseFootedSpritsailAssetData data,
            float baseScale,
            float height,
            float width,
            float mastRadius
        )
        {
            if (
                !Finite(baseScale)
                || baseScale <= 0
                || !Finite(height)
                || height <= 0
                || !Finite(width)
                || width <= 0
                || !Finite(mastRadius)
                || mastRadius <= 0
            )
                throw new ArgumentException(
                    "Expected positive finite spritsail fitting dimensions."
                );
            var throat = Corner(data: data, index: data.Throat);
            var clothScale = new Vector3(x: baseScale, y: baseScale * height, z: baseScale * width);
            var cloth = LooseFootedSpritsailTransform.TRS(
                pos: new Vector3(
                    x: 0,
                    y: -throat.y * clothScale.y,
                    z: -mastRadius - Clearance - throat.z * clothScale.z
                ),
                q: Quaternion.identity,
                s: clothScale
            );
            var snotterOrigin = data.SnotterAssembly.RestTransform.MultiplyPoint3x4(
                point: Vector3.zero
            );
            float hardwareScale = (mastRadius + CollarClearance) / CollarRadius(data: data);
            var hardware =
                LooseFootedSpritsailTransform.TRS(
                    pos: new Vector3(x: 0, y: (snotterOrigin.y - throat.y) * clothScale.y, z: 0),
                    q: Quaternion.identity,
                    s: Vector3.one * hardwareScale
                ) * Matrix4x4.Translate(vector: -snotterOrigin);
            var hinge = hardware.MultiplyPoint3x4(
                point: data.Sprit.RestTransform.MultiplyPoint3x4(point: Vector3.zero)
            );
            var target =
                cloth.MultiplyPoint3x4(point: Corner(data: data, index: data.Peak)) - hinge;
            // The baked peak in spar-local space is a virtual tie point. Retain the authored
            // gap to the physical tip while mapping that virtual point exactly to the cloth peak.
            var reference = LooseFootedSpritsailTransform.InversePoint(
                matrix: data.Sprit.RestTransform,
                point: Corner(data: data, index: data.Peak)
            );
            float thickness = baseScale * (float)Math.Sqrt(height * width);
            float radialSquared =
                thickness * thickness * (reference.x * reference.x + reference.y * reference.y);
            if (Math.Abs(reference.z) < 0.01f || target.sqrMagnitude <= radialSquared)
                throw new ArgumentException("The fitted peak is too close to the sprit hinge.");
            var sparScale = new Vector3(
                x: thickness,
                y: thickness,
                z: (float)Math.Sqrt(target.sqrMagnitude - radialSquared) / Math.Abs(reference.z)
            );
            var restRotation = LooseFootedSpritsailTransform.Rotation(
                matrix: data.Sprit.RestTransform
            );
            var from = restRotation * Vector3.Scale(a: reference, b: sparScale);
            var sparRotation =
                LooseFootedSpritsailTransform.FromTo(from: from, to: target) * restRotation;
            return new LooseFootedSpritsailPose
            {
                Sail = cloth * data.Sail.RestTransform,
                Sprit = LooseFootedSpritsailTransform.TRS(
                    pos: hinge,
                    q: sparRotation,
                    s: sparScale
                ),
                SnotterAssembly = hardware * data.SnotterAssembly.RestTransform,
                RotationPlate = hardware * data.RotationPlate.RestTransform,
            };
        }

        internal static bool CollisionStrip(
            LooseFootedSpritsailAssetData data,
            LooseFootedSpritsailPose pose,
            int column,
            out Vector3 center,
            out Vector3 size
        )
        {
            var tack = pose.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Tack]);
            var throat = pose.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Throat]);
            var clew = pose.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Clew]);
            var peak = pose.Sail.MultiplyPoint3x4(point: data.Sail.Vertices[data.Peak]);
            float a = (column + 0.05f) / CollisionStrips;
            float b = (column + 0.95f) / CollisionStrips;
            float top = Math.Min(
                Vector3.Lerp(a: throat, b: peak, t: a).y,
                Vector3.Lerp(a: throat, b: peak, t: b).y
            );
            float bottom = Math.Max(
                Vector3.Lerp(a: tack, b: clew, t: a).y,
                Vector3.Lerp(a: tack, b: clew, t: b).y
            );
            float start = Mathf.Lerp(a: throat.z, b: peak.z, t: a);
            float end = Mathf.Lerp(a: throat.z, b: peak.z, t: b);
            center = new Vector3(x: 0, y: (top + bottom) / 2, z: (start + end) / 2);
            size = new Vector3(
                x: 0.02f,
                y: Math.Max(0.001f, top - bottom - 0.02f),
                z: Math.Abs(end - start)
            );
            return top - bottom > 0.02f;
        }

        internal static float ClampScale(float value) =>
            Mathf.Clamp(value: value, min: 0.2f, max: 3f);

        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    internal struct LooseFootedSpritsailPose
    {
        internal Matrix4x4 Sail;
        internal Matrix4x4 Sprit;
        internal Matrix4x4 SnotterAssembly;
        internal Matrix4x4 RotationPlate;
    }
}
