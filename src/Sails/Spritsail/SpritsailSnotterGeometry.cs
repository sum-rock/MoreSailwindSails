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
        internal const int HeaderSize = 100;

        internal const int DarkWoodMaterial = 0;
        internal const int MetalMaterial = 1;
        internal const int MaterialCount = 2;

        internal const int SleevePart = 0;
        internal const int BoltPart = 1;
        internal const int MountingPart = 2;

        // Supplied OBJ: Y up, bolt centered at Y=Z=0 and pointing along -X.
        // Inner radii include the flat facets, not just the circular vertex rings.
        private static readonly float InnerRadius;
        private static readonly float MountingOuterRadius;
        private const float MountingDiameterExtra = 0.2032f;
        private static readonly float OuterRadius;
        private static readonly float BoltBase;
        private static readonly float HeadInner;
        private static readonly float BoltEnd;
        private const float SpritClearance = 0.003f;
        private const float HeadClearance = 0.001f;
        private const float BoltProjection = 0.005f;
        private const float Clearance = 0.005f;
        private static readonly int[] parts;
        private static readonly Vector3[] source;
        private static readonly Vector3[] sourceNormals;
        private static readonly Vector2[] texture;

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
                    if (reader.ReadInt32() != 0x364E534D)
                        throw new InvalidDataException(message: "Invalid snotter mesh header.");
                    int vertexCount = reader.ReadInt32();
                    int indexCount = reader.ReadInt32();
                    if (
                        vertexCount <= 0
                        || vertexCount > 65535
                        || indexCount <= 0
                        || indexCount % 3 != 0
                        || stream.Length != HeaderSize + vertexCount * 36L + indexCount / 3L * 16L
                    )
                        throw new InvalidDataException(message: "Invalid snotter mesh size.");
                    InnerRadius = ReadDimension(reader: reader);
                    OuterRadius = ReadDimension(reader: reader);
                    MountingOuterRadius = ReadDimension(reader: reader);
                    BoltBase = ReadDimension(reader: reader);
                    HeadInner = ReadDimension(reader: reader);
                    BoltEnd = ReadDimension(reader: reader);
                    if (InnerRadius >= OuterRadius || BoltBase >= HeadInner || HeadInner >= BoltEnd)
                        throw new InvalidDataException(message: "Invalid snotter fit metadata.");
                    // Source and marker SHA-256 hashes are verified by the offline checks.
                    stream.Position = HeaderSize;
                    source = new Vector3[vertexCount];
                    sourceNormals = new Vector3[vertexCount];
                    texture = new Vector2[vertexCount];
                    parts = new int[vertexCount];
                    var indices = new int[indexCount];
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
                    // Separate face corners prevent wood UVs from altering metal.
                    for (int i = 0; i < vertexCount; i++)
                        if (woodVertices[i])
                            texture[i] = SpritsailSnotterMaterials.WoodUv(uv: texture[i]);
                }
            }
        }

        private static float ReadDimension(BinaryReader reader)
        {
            float value = reader.ReadSingle();
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
                throw new InvalidDataException(message: "Invalid snotter fit dimension.");
            return value;
        }

        internal static int[] MaterialTriangles(int material) =>
            (int[])materialIndices[material].Clone();

        internal static int Part(int vertex) => parts[vertex];

        // Build each rigid group's compact vertex map and material topology once.
        internal static void Group(
            bool mounting,
            out int[] sourceIndices,
            out Vector2[] uv,
            out int[][] triangles
        )
        {
            var selected = new List<int>();
            var mapped = new int[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                mapped[i] = -1;
                if ((parts[i] == MountingPart) == mounting)
                {
                    mapped[i] = selected.Count;
                    selected.Add(item: i);
                }
            }
            sourceIndices = selected.ToArray();
            uv = new Vector2[selected.Count];
            for (int i = 0; i < selected.Count; i++)
                uv[i] = texture[selected[i]];
            triangles = new int[MaterialCount][];
            for (int material = 0; material < MaterialCount; material++)
            {
                var group = new List<int>();
                var all = materialIndices[material];
                for (int i = 0; i < all.Length; i += 3)
                {
                    int a = mapped[all[i]],
                        b = mapped[all[i + 1]],
                        c = mapped[all[i + 2]];
                    if ((a >= 0) != (b >= 0) || (a >= 0) != (c >= 0))
                        throw new InvalidDataException(
                            message: "Snotter triangle spans rigid groups."
                        );
                    if (a < 0)
                        continue;
                    group.Add(item: a);
                    group.Add(item: b);
                    group.Add(item: c);
                }
                triangles[material] = group.ToArray();
            }
        }

        // Both frames follow the mast; only the sleeve/bolt frame follows the tack.
        // The caller supplies orthonormal world directions, independent of parent scale.
        internal static Matrix4x4 Frame(Vector3 center, Vector3 axis, Vector3 outward)
        {
            var right = Vector3.Cross(lhs: axis, rhs: outward);
            return new Matrix4x4(
                column0: new Vector4(right.x, right.y, right.z, 0),
                column1: new Vector4(axis.x, axis.y, axis.z, 0),
                column2: new Vector4(outward.x, outward.y, outward.z, 0),
                column3: new Vector4(center.x, center.y, center.z, 1)
            );
        }

        internal static float LuffDistance(float mastRadius) =>
            (mastRadius + Clearance) / InnerRadius * OuterRadius + SpritClearance;

        internal static Vector2[] TextureCoordinates() => (Vector2[])texture.Clone();

        internal static void Fit(
            float sparRadius,
            float mastRadius,
            float pivotDistance,
            out Vector3[] vertices,
            out Vector3[] normals
        )
        {
            vertices = new Vector3[source.Length];
            normals = new Vector3[source.Length];
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
