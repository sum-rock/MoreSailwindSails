using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Fits and poses the rotating sleeve/bolt and mast-fixed lower mounting.
    internal static class SpritsailSnotterGeometry
    {
        internal const string ResourceName = "MoreSailwindSails.Snotter";

        internal const int DarkWoodMaterial = 0;
        internal const int MetalMaterial = 1;
        internal const int MaterialCount = 2;

        internal const int SleevePart = 0;
        internal const int BoltPart = 1;
        internal const int MountingPart = 2;

        // Supplied OBJ: Y up, bolt centered at Y=Z=0 and pointing along -X.
        // Inner radii include the flat facets, not just the circular vertex rings.
        private const float InnerRadius = 0.8718f;
        private const float MountingOuterRadius = 1.2314329f;
        private const float MountingDiameterExtra = 0.2032f;
        private const float OuterRadius = 1.1408f;
        private const float BoltBase = 0.875f;
        private const float HeadInner = 1.593703f;
        private const float BoltEnd = 1.68f;
        private const float SpritClearance = 0.003f;
        private const float HeadClearance = 0.001f;
        private const float BoltProjection = 0.005f;
        private const float Clearance = 0.005f;
        private static readonly int[] parts;
        private static readonly Vector3[] source;
        private static readonly Vector3[] sourceNormals;
        private static readonly Vector2[] texture;
        private static readonly int[] indices;
        private static readonly int[][] materialIndices;

        static SpritsailSnotterGeometry()
        {
            using (
                var stream = typeof(SpritsailSnotterGeometry).Assembly.GetManifestResourceStream(
                    name: ResourceName
                )
            )
            {
                if (stream == null)
                    throw new InvalidOperationException(
                        message: "Embedded snotter mesh is missing."
                    );
                using (var reader = new BinaryReader(input: stream))
                {
                    if (reader.ReadInt32() != 0x354E534D)
                        throw new InvalidDataException(message: "Invalid snotter mesh header.");
                    int vertexCount = reader.ReadInt32();
                    int indexCount = reader.ReadInt32();
                    if (
                        vertexCount <= 0
                        || vertexCount > 65535
                        || indexCount <= 0
                        || indexCount % 3 != 0
                        || stream.Length != 12L + vertexCount * 36L + indexCount / 3L * 16L
                    )
                        throw new InvalidDataException(message: "Invalid snotter mesh size.");
                    source = new Vector3[vertexCount];
                    sourceNormals = new Vector3[vertexCount];
                    texture = new Vector2[vertexCount];
                    parts = new int[vertexCount];
                    indices = new int[indexCount];
                    for (int i = 0; i < vertexCount; i++)
                    {
                        float x = reader.ReadSingle(),
                            y = reader.ReadSingle(),
                            z = reader.ReadSingle();
                        source[i] = new Vector3(z, y, -x);
                        texture[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                        float nx = reader.ReadSingle(),
                            ny = reader.ReadSingle(),
                            nz = reader.ReadSingle();
                        sourceNormals[i] = new Vector3(nz, ny, -nx);
                        int part = reader.ReadInt32();
                        if (part < SleevePart || part > MountingPart)
                            throw new InvalidDataException(message: "Invalid snotter mesh part.");
                        parts[i] = part;
                    }
                    var buckets = new[] { new List<int>(), new List<int>() };
                    var woodVertices = new bool[vertexCount];
                    for (int i = 0; i < indexCount; i += 3)
                    {
                        for (int corner = 0; corner < 3; corner++)
                        {
                            int index = reader.ReadInt32();
                            if (index < 0 || index >= vertexCount)
                                throw new InvalidDataException(
                                    message: "Invalid snotter mesh index."
                                );
                            indices[i + corner] = index;
                        }
                        int material = reader.ReadInt32();
                        if (material < 0 || material >= MaterialCount)
                            throw new InvalidDataException(
                                message: "Invalid snotter mesh material."
                            );
                        for (int corner = 0; corner < 3; corner++)
                        {
                            buckets[material].Add(item: indices[i + corner]);
                            if (material == DarkWoodMaterial)
                                woodVertices[indices[i + corner]] = true;
                        }
                    }
                    materialIndices = new[] { buckets[0].ToArray(), buckets[1].ToArray() };
                    // Native dhow_medium_paint (Sailwind 0.39): interior of the dark
                    // longitudinal mast wood island, away from its edge and bright grain.
                    // The bake has separate face corners, so wood UVs never alter metal.
                    // Map each vertex once even when two triangles share a face corner.
                    for (int i = 0; i < vertexCount; i++)
                        if (woodVertices[i])
                            texture[i] = new Vector2(
                                x: 0.350f + texture[i].x * 0.002f,
                                y: 0.870f + texture[i].y * 0.030f
                            );
                }
            }
        }

        internal static int[] MaterialTriangles(int material) =>
            (int[])materialIndices[material].Clone();

        internal static int Part(int vertex) => parts[vertex];

        // Both frames move with the mast; only the sleeve/bolt frame turns with the sail.
        // Directions and axis are orthonormal world vectors supplied by the caller.
        internal static Vector3 PosePoint(
            int vertex,
            Vector3 point,
            Vector3 center,
            Vector3 axis,
            Vector3 rotatingDirection,
            Vector3 fixedDirection
        )
        {
            var outward = parts[vertex] == MountingPart ? fixedDirection : rotatingDirection;
            var right = Vector3.Cross(axis, outward);
            return center + right * point.x + axis * point.y + outward * point.z;
        }

        internal static float LuffDistance(float mastRadius) =>
            (mastRadius + Clearance) / InnerRadius * OuterRadius + SpritClearance;

        internal static void Create(
            float sparRadius,
            float mastRadius,
            float pivotDistance,
            out Vector3[] vertices,
            out Vector3[] normals,
            out Vector2[] uv,
            out int[] triangles
        )
        {
            vertices = new Vector3[source.Length];
            normals = new Vector3[source.Length];
            uv = (Vector2[])texture.Clone();
            triangles = (int[])indices.Clone();
            float scale = (mastRadius + Clearance) / InnerRadius;
            float mountingScale = (mastRadius + MountingDiameterExtra / 2) / MountingOuterRadius;
            float pinBase = mastRadius * 0.9f;
            float headInner = pivotDistance + sparRadius + HeadClearance;
            for (int i = 0; i < vertices.Length; i++)
            {
                var point = source[i];
                var normal = sourceNormals[i];
                if (parts[i] == BoltPart)
                {
                    float lengthScale =
                        point.z <= HeadInner
                            ? (headInner - pinBase) / (HeadInner - BoltBase)
                            : (BoltProjection - HeadClearance) / (BoltEnd - HeadInner);
                    normal = new Vector3(
                        x: normal.x / (sparRadius * 3),
                        y: normal.y / (sparRadius * 3),
                        z: normal.z / lengthScale
                    );
                    // Retain the pin/head profile, with its head outside the timber
                    // and its root buried in the mast beneath the curved sheet.
                    point.x *= sparRadius * 3;
                    point.y *= sparRadius * 3;
                    // Stretch the shaft to reach through the spar, then fit the head
                    // separately so a larger mast cannot lengthen the visible tip.
                    point.z =
                        point.z <= HeadInner
                            ? pinBase
                                + (point.z - BoltBase)
                                    / (HeadInner - BoltBase)
                                    * (headInner - pinBase)
                            : headInner
                                + (point.z - HeadInner)
                                    / (BoltEnd - HeadInner)
                                    * (BoltProjection - HeadClearance);
                }
                else if (parts[i] == MountingPart)
                {
                    // Fit the lower clip's opening independently while keeping its
                    // authored top at the sleeve's bottom (source Y=-1).
                    point.x *= mountingScale;
                    point.z *= mountingScale;
                    point.y *= scale;
                    normal = new Vector3(
                        x: normal.x / mountingScale,
                        y: normal.y / scale,
                        z: normal.z / mountingScale
                    );
                }
                else
                    point *= scale;
                vertices[i] = point;
                // Inverse-transpose of each part's fit preserves authored shading
                // under unequal radial/vertical scales and shortened bolt sections.
                normals[i] = normal.normalized;
            }
        }
    }
}
