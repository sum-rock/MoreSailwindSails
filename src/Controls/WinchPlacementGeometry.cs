using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using UnityEngine;

namespace MoreSailwindSails.Controls
{
    internal static class WinchPlacementGeometry
    {
        internal static WinchPlacement[] Candidates(
            WinchMountDefinition definition,
            Vector3 origin,
            float radius,
            Vector3 axisPoint
        )
        {
            if (definition.FixedSurfacePoint.HasValue)
            {
                var point = definition.FixedSurfacePoint.Value;
                return new[]
                {
                    new WinchPlacement(
                        point.Position + point.Normal * definition.BaseOffset,
                        Align(definition.SourceNormal, point.Normal)
                    ),
                };
            }
            float spacing = Math.Max(0.35f, radius * 2f + 0.02f);
            if (definition.SurfaceSegments != null)
                return SurfaceCandidates(definition, origin, radius, spacing);
            // An unmeasured deck/rail direction cannot establish physical support.
            if (!definition.OnMast)
                return Array.Empty<WinchPlacement>();
            var result = new List<WinchPlacement>();
            var offsets = new[] { 1, -1, 2, 3 };
            foreach (int offset in offsets)
                if (Math.Abs(offset * spacing) <= 1.401f && offset * spacing >= -0.701f)
                    result.Add(
                        new WinchPlacement(
                            origin + definition.Direction * (offset * spacing),
                            Quaternion.identity
                        )
                    );
            if (definition.OnMast)
            {
                // Overflow stays within the same height band, on another mast face.
                // Rotate the fitting and its radial position together, never just its position.
                var center =
                    axisPoint
                    + definition.Direction * Vector3.Dot(origin - axisPoint, definition.Direction);
                var radial = origin - center;
                foreach (float degrees in new[] { 90f, -90f, 180f })
                {
                    var rotation = WinchPlacement.Turn(definition.Direction, degrees);
                    foreach (int offset in new[] { 0, 1, -1, 2 })
                        if (Math.Abs(offset * spacing) <= 1.401f && offset * spacing >= -0.701f)
                            result.Add(
                                new WinchPlacement(
                                    center
                                        + rotation * radial
                                        + definition.Direction * (offset * spacing),
                                    rotation
                                )
                            );
                }
                // Large fittings can leave no free point on the regular grid,
                // especially beside a topmast's native control. Try the finite
                // upper band end last, preserving the existing preferred placements.
                // Do not add a lower endpoint: low native fittings can be near deck level.
                foreach (float degrees in new[] { 0f, 90f, -90f, 180f })
                {
                    var rotation = WinchPlacement.Turn(definition.Direction, degrees);
                    result.Add(
                        new WinchPlacement(
                            center + rotation * radial + definition.Direction * 1.4f,
                            rotation
                        )
                    );
                }
            }
            return result.ToArray();
        }

        private static WinchPlacement[] SurfaceCandidates(
            WinchMountDefinition definition,
            Vector3 origin,
            float radius,
            float spacing
        )
        {
            var result = new List<WinchPlacement>();
            foreach (var segment in definition.SurfaceSegments)
            {
                var travel = segment.End - segment.Start;
                float length = travel.magnitude;
                if (length < radius * 2f)
                    continue;
                var direction = travel / length;
                float nearest = Math.Max(
                    radius,
                    Math.Min(length - radius, Vector3.Dot(origin - segment.Start, direction))
                );
                var rotation = Align(definition.SourceNormal, segment.Normal);
                // A neighboring native fitting can block the regular spacing grid.
                // Include both safe ends so a short strip does not lose usable space.
                var distances = new List<float> { radius, length - radius };
                foreach (int offset in new[] { 0, 1, -1, 2, -2, 3, -3, 4, -4 })
                    distances.Add(nearest + offset * spacing);
                foreach (float along in distances)
                {
                    if (along < radius || along > length - radius)
                        continue;
                    var position =
                        segment.Start + direction * along + segment.Normal * definition.BaseOffset;
                    if ((position - origin).sqrMagnitude <= 1.401f * 1.401f)
                        result.Add(new WinchPlacement(position, rotation));
                }
            }
            return result.OrderBy(p => (p.Position - origin).sqrMagnitude).ToArray();
        }

        private static Quaternion Align(Vector3 from, Vector3 to)
        {
            // Managed equivalent of FromToRotation; geometry checks run without Unity.
            float dot = Vector3.Dot(from, to);
            if (dot < -0.999999f)
                return WinchPlacement.Turn(
                    Vector3
                        .Cross(from, Math.Abs(from.x) < 0.9f ? Vector3.right : Vector3.up)
                        .normalized,
                    180f
                );
            var cross = Vector3.Cross(from, to);
            float w = 1f + dot;
            float magnitude = (float)Math.Sqrt(cross.sqrMagnitude + w * w);
            return new Quaternion(
                cross.x / magnitude,
                cross.y / magnitude,
                cross.z / magnitude,
                w / magnitude
            );
        }
    }

    internal readonly struct WinchPlacement
    {
        internal readonly Vector3 Position;
        internal readonly Quaternion Rotation;

        internal WinchPlacement(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        internal static Quaternion Turn(Vector3 axis, float degrees)
        {
            double half = degrees * Math.PI / 360;
            var imaginary = axis * (float)Math.Sin(half);
            return new Quaternion(imaginary.x, imaginary.y, imaginary.z, (float)Math.Cos(half));
        }
    }
}
