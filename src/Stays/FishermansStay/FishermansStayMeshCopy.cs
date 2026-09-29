using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Stays.FishermansStay
{
    // Creates owned mesh regions while retaining every donor channel, submesh and original seam index.
    internal static class FishermansStayMeshCopy
    {
        internal static Mesh Create(
            Mesh source,
            int[] regions,
            int region,
            Matrix4x4 toGeometry,
            bool collar,
            List<Mesh> owned,
            out FishermansStayCollarFrame frame
        )
        {
            if (!source.isReadable)
                throw new ArgumentException(
                    "Native collar/combined stay mesh is not readable: " + source.name
                );
            var vertices = source.vertices;
            FishermansStayMeshRegions.Validate(vertices: vertices, triangles: source.triangles);
            var selected = FishermansStayMeshRegions
                .Select(triangles: source.triangles, regions: regions, region: region)
                .Distinct()
                .ToArray();
            if (selected.Length == 0)
                throw new ArgumentException("Native stay region is empty: " + source.name);
            var transformed = vertices.Select(toGeometry.MultiplyPoint3x4).ToArray();
            frame = collar
                ? new FishermansStayCollarFrame(
                    points: selected.Select(i => transformed[i]).ToArray()
                )
                : null;
            var copy = Object.Instantiate(source);
            owned.Add(copy); // Registration rollback owns this even if channel validation fails.
            copy.name = "FishermansStay " + source.name + " region " + region;
            var fitted = new Vector3[vertices.Length];
            foreach (int i in selected)
                fitted[i] = frame == null ? transformed[i] : frame.Point(point: transformed[i]);
            copy.vertices = fitted;
            var normals = source.normals;
            var tangents = source.tangents;
            if (
                (normals.Length != 0 && normals.Length != vertices.Length)
                || (tangents.Length != 0 && tangents.Length != vertices.Length)
            )
                throw new ArgumentException(
                    "Native stay has incomplete normal or tangent channels."
                );
            var normalMatrix = toGeometry.inverse.transpose;
            foreach (int i in selected)
            {
                if (normals.Length != 0)
                {
                    var normal = normalMatrix.MultiplyVector(normals[i]);
                    normals[i] = (
                        frame == null ? normal : frame.Direction(direction: normal)
                    ).normalized;
                }
                if (tangents.Length != 0)
                {
                    var tangent = toGeometry.MultiplyVector(
                        new Vector3(tangents[i].x, tangents[i].y, tangents[i].z)
                    );
                    tangent = (
                        frame == null ? tangent : frame.Direction(direction: tangent)
                    ).normalized;
                    tangents[i] = new Vector4(tangent.x, tangent.y, tangent.z, tangents[i].w);
                }
            }
            if (normals.Length != 0)
                copy.normals = normals;
            if (tangents.Length != 0)
                copy.tangents = tangents;
            for (int submesh = 0; submesh < source.subMeshCount; submesh++)
                copy.SetTriangles(
                    triangles: FishermansStayMeshRegions.Select(
                        triangles: source.GetTriangles(submesh),
                        regions: regions,
                        region: region
                    ),
                    submesh: submesh,
                    calculateBounds: false
                );
            // Unreferenced vertices retain their channel slots but must not influence culling.
            var bounds = new Bounds(center: fitted[selected[0]], size: Vector3.zero);
            foreach (int i in selected)
                bounds.Encapsulate(fitted[i]);
            copy.bounds = bounds;
            return copy;
        }
    }
}
