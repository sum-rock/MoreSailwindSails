using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Builds curved wooden cheek plates, a seated heel cradle, mast band and bolt heads.
    internal static class SpritsailSnotterGeometry
    {
        internal static void Create(
            float backDepth,
            float mastRadius,
            out Vector3[] vertices,
            out Vector2[] uv,
            out int[] wood,
            out int[] iron
        )
        {
            var points = new List<Vector3>();
            var texture = new List<Vector2>();
            var woodIndices = new List<int>();
            var ironIndices = new List<int>();
            const int steps = 24;
            for (int i = 0; i < steps; i++)
            {
                float a = -(float)Math.PI / 2 + (float)Math.PI * i / steps;
                float b = -(float)Math.PI / 2 + (float)Math.PI * (i + 1) / steps;
                Vector3 Inner(float angle) =>
                    new Vector3(
                        0,
                        0.65f + 1.55f * (float)Math.Sin(angle),
                        1 - (backDepth + 0.5f) * (float)Math.Cos(angle)
                    );
                Vector3 Outer(float angle) =>
                    new Vector3(
                        0,
                        0.65f + 1.95f * (float)Math.Sin(angle),
                        1.1f - (backDepth + 1.1f) * (float)Math.Cos(angle)
                    );
                Strip(
                    low: -1.65f,
                    high: -1.1f,
                    a: Inner(angle: a),
                    b: Inner(angle: b),
                    c: Outer(angle: a),
                    d: Outer(angle: b)
                );
                Strip(
                    low: 1.1f,
                    high: 1.65f,
                    a: Inner(angle: a),
                    b: Inner(angle: b),
                    c: Outer(angle: a),
                    d: Outer(angle: b)
                );
                // Join the cheeks below the heel and against the mast, leaving
                // the forward and upper mouth open for pitching and stowing.
                if (i < 3 || (i >= 10 && i < 14))
                    Strip(
                        low: -1.1f,
                        high: 1.1f,
                        a: Inner(angle: a),
                        b: Inner(angle: b),
                        c: Outer(angle: a),
                        d: Outer(angle: b)
                    );
            }
            var mastCenter = new Vector3(0, 0, -backDepth - mastRadius);
            for (int i = 0; i < 48; i++)
            {
                float a = (float)Math.PI * 2 * i / 48;
                float b = (float)Math.PI * 2 * (i + 1) / 48;
                Vector3 Ring(float angle, float radius, float height) =>
                    mastCenter
                    + new Vector3(
                        (float)Math.Sin(angle) * radius,
                        height,
                        (float)Math.Cos(angle) * radius
                    );
                var outward = new Vector3(
                    (float)Math.Sin((a + b) / 2),
                    0,
                    (float)Math.Cos((a + b) / 2)
                );
                float inner = mastRadius + 0.015f,
                    outer = mastRadius + 0.13f;
                Quad(
                    a: Ring(a, outer, 0.25f),
                    b: Ring(b, outer, 0.25f),
                    c: Ring(b, outer, 0.95f),
                    d: Ring(a, outer, 0.95f),
                    normal: outward,
                    metal: true
                );
                Quad(
                    a: Ring(a, inner, 0.25f),
                    b: Ring(b, inner, 0.25f),
                    c: Ring(b, inner, 0.95f),
                    d: Ring(a, inner, 0.95f),
                    normal: -outward,
                    metal: true
                );
                Quad(
                    a: Ring(a, inner, 0.95f),
                    b: Ring(b, inner, 0.95f),
                    c: Ring(b, outer, 0.95f),
                    d: Ring(a, outer, 0.95f),
                    normal: Vector3.up,
                    metal: true
                );
                Quad(
                    a: Ring(a, inner, 0.25f),
                    b: Ring(b, inner, 0.25f),
                    c: Ring(b, outer, 0.25f),
                    d: Ring(a, outer, 0.25f),
                    normal: Vector3.down,
                    metal: true
                );
            }
            foreach (float side in new[] { -1f, 1f })
            {
                var center = new Vector3(side * 1.67f, 0.6f, -backDepth + 0.25f);
                // A narrow iron mounting plate and raised six-sided bolt head.
                Quad(
                    a: center + new Vector3(0, -0.55f, -0.2f),
                    b: center + new Vector3(0, 0.55f, -0.2f),
                    c: center + new Vector3(0, 0.55f, 0.2f),
                    d: center + new Vector3(0, -0.55f, 0.2f),
                    normal: Vector3.right * side,
                    metal: true
                );
                for (int i = 0; i < 6; i++)
                {
                    float a = (float)Math.PI * i / 3,
                        b = (float)Math.PI * (i + 1) / 3;
                    var p = new Vector3(0, (float)Math.Sin(a), (float)Math.Cos(a)) * 0.23f;
                    var q = new Vector3(0, (float)Math.Sin(b), (float)Math.Cos(b)) * 0.23f;
                    var lift = Vector3.right * side * 0.16f;
                    Quad(
                        a: center + p,
                        b: center + q,
                        c: center + q + lift,
                        d: center + p + lift,
                        normal: p + q,
                        metal: true
                    );
                    Quad(
                        a: center + lift,
                        b: center + p + lift,
                        c: center + q + lift,
                        d: center + lift,
                        normal: Vector3.right * side,
                        metal: true
                    );
                }
            }
            vertices = points.ToArray();
            uv = texture.ToArray();
            wood = woodIndices.ToArray();
            iron = ironIndices.ToArray();

            void Strip(float low, float high, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                var left = Vector3.right * low;
                var right = Vector3.right * high;
                var radial = (a + b + c + d) / 4 - new Vector3(0, 0.65f, 1);
                Quad(
                    a: a + left,
                    b: b + left,
                    c: d + left,
                    d: c + left,
                    normal: Vector3.left,
                    metal: false
                );
                Quad(
                    a: a + right,
                    b: b + right,
                    c: d + right,
                    d: c + right,
                    normal: Vector3.right,
                    metal: false
                );
                Quad(
                    a: a + left,
                    b: a + right,
                    c: b + right,
                    d: b + left,
                    normal: -radial,
                    metal: false
                );
                Quad(
                    a: c + left,
                    b: c + right,
                    c: d + right,
                    d: d + left,
                    normal: radial,
                    metal: false
                );
                Quad(
                    a: a + left,
                    b: c + left,
                    c: c + right,
                    d: a + right,
                    normal: a - b,
                    metal: false
                );
                Quad(
                    a: b + left,
                    b: d + left,
                    c: d + right,
                    d: b + right,
                    normal: b - a,
                    metal: false
                );
            }
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, bool metal)
            {
                int start = points.Count;
                points.AddRange(new[] { a, b, c, d });
                texture.AddRange(
                    new[]
                    {
                        new Vector2(a.z, a.y),
                        new Vector2(b.z, b.y),
                        new Vector2(c.z, c.y),
                        new Vector2(d.z, d.y),
                    }
                );
                var indices = metal ? ironIndices : woodIndices;
                bool forward = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0;
                indices.AddRange(
                    forward
                        ? new[] { start, start + 1, start + 2 }
                        : new[] { start, start + 2, start + 1 }
                );
                if ((d - a).sqrMagnitude > 1e-10f)
                    indices.AddRange(
                        forward
                            ? new[] { start, start + 2, start + 3 }
                            : new[] { start, start + 3, start + 2 }
                    );
            }
        }
    }
}
