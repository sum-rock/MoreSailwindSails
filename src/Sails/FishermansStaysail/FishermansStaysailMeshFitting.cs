// Fits fresh normalized baked data to a stay, then prepares planar UVs and the shared rig.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal static class FishermansStaysailMeshFitting
    {
        internal const float UVMargin = 0.05f;

        // Sail frame: +X up, -Z toward the fore mast; corners: head fore/aft, foot fore/aft.
        // The caller supplies fresh arrays owned exclusively by this fitted instance.
        internal static FishermansStaysailMeshData Fit(
            FishermansStaysailMeshData data,
            float width,
            float headSlope
        )
        {
            FishermansStaysailGeometry.ValidateWidth(width: width);
            if (
                float.IsNaN(headSlope)
                || float.IsInfinity(headSlope)
                || headSlope < 0
                || headSlope > 80
            )
                throw new ArgumentException(
                    "Expected a finite stay slope between 0 and 80 degrees."
                );
            float rise = width * (float)Math.Tan(headSlope * Math.PI / 180);
            int columns = FishermansStaysailGeometry.Columns;
            int rows = FishermansStaysailGeometry.Rows;
            float headDelta = rise - data.Vertices[columns].x * width;
            float bottom =
                Math.Min(
                    data.Vertices[rows * (columns + 1)].x,
                    data.Vertices[data.Vertices.Length - 1].x
                ) * width;
            float height = rise - bottom;
            if (
                data.Vertices[rows * (columns + 1)].x >= 0
                || data.Vertices[data.Vertices.Length - 1].x * width >= rise
            )
                throw new ArgumentException("Stay slope leaves no positive luff or leech.");
            float uvScale = (1 - 2 * UVMargin) / Math.Max(width, height);
            float uvLeft = (1 - width * uvScale) / 2;
            float uvBottom = (1 - height * uvScale) / 2;
            for (int row = 0; row <= rows; row++)
            for (int col = 0; col <= columns; col++)
            {
                int i = row * (columns + 1) + col;
                float u = (float)col / columns,
                    v = (float)row / rows;
                var point = data.Vertices[i] * width;
                point.x += headDelta * u * (1 - v);
                // Reproject the flat fitted cut, then add the established rest belly.
                data.UV[i] = new Vector2(
                    uvLeft + (point.z + width) * uvScale,
                    uvBottom + (point.x - bottom) * uvScale
                );
                point.y = FishermansStaysailGeometry.RestCamber(width: width, u: u, v: v);
                data.Vertices[i] = point;
            }
            data.Corners = new[]
            {
                data.Vertices[0],
                data.Vertices[columns],
                data.Vertices[rows * (columns + 1)],
                data.Vertices[data.Vertices.Length - 1],
            };
            return FishermansStaysailGeometry.Finish(width: width, data: data);
        }
    }
}
