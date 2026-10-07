using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Defines the common blunt-ended sprit profile used by every spritsail make.
    internal static class SpritsailSpritGeometry
    {
        internal const float ForwardExtension = 0.3048f;

        // Inputs are world-space or scale-applied coordinates, never unscaled mesh units.
        internal static Vector3 ForwardEnd(Vector3 pivot, Vector3 tip) =>
            pivot - (tip - pivot).normalized * ForwardExtension;

        // The bolt intersects the taper 12 inches from the forward cap. Use that
        // section's radius when trimming the pin, rather than the thicker midpoint.
        internal static float RadiusAtPivot(float radius, float pivotToTip)
        {
            float fraction = ForwardExtension / (ForwardExtension + pivotToTip);
            return radius * (1 - (1 - EndRadiusRatio) * Math.Abs(2 * fraction - 1));
        }

        internal const float RadiusFraction = 0.018f;
        internal const float ThicknessMultiplier = 1.03f;
        internal const float EndRadiusRatio = 0.85f;
        internal const float RopeThicknessMultiplier = 1.5f;

        internal static void Spar(out Vector3[] vertices, out Vector2[] uv, out int[] triangles)
        {
            const int sides = 12;
            vertices = new Vector3[(sides + 1) * 5 + 2];
            uv = new Vector2[vertices.Length];
            triangles = new int[sides * 18];
            for (int ring = 0; ring < 3; ring++)
            for (int side = 0; side <= sides; side++)
            {
                float angle = side * 2 * (float)Math.PI / sides;
                float radius = ring == 1 ? 1f : SpritsailSpritGeometry.EndRadiusRatio;
                int index = ring * (sides + 1) + side;
                vertices[index] = new Vector3(
                    (float)Math.Cos(d: angle) * radius,
                    (float)Math.Sin(a: angle) * radius,
                    ring * 0.5f
                );
                uv[index] = new Vector2((float)side / sides, ring * 0.5f);
            }
            for (int side = 0; side <= sides; side++)
            {
                vertices[3 * (sides + 1) + side] = vertices[side];
                vertices[4 * (sides + 1) + side] = vertices[2 * (sides + 1) + side];
                uv[3 * (sides + 1) + side] =
                    new Vector2(vertices[side].x, vertices[side].y) * 0.5f + Vector2.one * 0.5f;
                uv[4 * (sides + 1) + side] = uv[3 * (sides + 1) + side];
            }
            int bottom = vertices.Length - 2,
                top = vertices.Length - 1;
            vertices[top] = Vector3.forward;
            uv[bottom] = uv[top] = Vector2.one * 0.5f;
            int cursor = 0;
            for (int side = 0; side < sides; side++)
            {
                for (int ring = 0; ring < 2; ring++)
                {
                    int a = ring * (sides + 1) + side,
                        b = a + sides + 1;
                    triangles[cursor++] = a;
                    triangles[cursor++] = a + 1;
                    triangles[cursor++] = b;
                    triangles[cursor++] = a + 1;
                    triangles[cursor++] = b + 1;
                    triangles[cursor++] = b;
                }
                triangles[cursor++] = bottom;
                triangles[cursor++] = 3 * (sides + 1) + side + 1;
                triangles[cursor++] = 3 * (sides + 1) + side;
                triangles[cursor++] = top;
                triangles[cursor++] = 4 * (sides + 1) + side;
                triangles[cursor++] = 4 * (sides + 1) + side + 1;
            }
        }
    }
}
