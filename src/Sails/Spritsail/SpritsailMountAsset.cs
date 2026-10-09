using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Reads the baked luff mount once; live sails copy its immutable topology into owned meshes.
    internal static class SpritsailMountAsset
    {
        internal const string ResourceName = "MoreSailwindSails.SailMount";
        internal const int HeaderSize = 44;
        internal static readonly Vector3[] Positions;
        internal static readonly Vector3[] Normals;
        internal static readonly Vector2[] UV;
        internal static readonly int[] Parts;
        internal static readonly int[][] Triangles;
        internal static readonly float[] EyeletX = new float[7];
        internal static readonly float[] RopeRadii = new float[7];

        private static bool Finite(Vector3 point) =>
            SpritsailDeployment.Finite(value: point.x)
            && SpritsailDeployment.Finite(value: point.y)
            && SpritsailDeployment.Finite(value: point.z);

        static SpritsailMountAsset()
        {
            using (
                var stream = typeof(SpritsailMountAsset).Assembly.GetManifestResourceStream(
                    name: ResourceName
                )
            )
            {
                if (stream == null)
                    throw new InvalidOperationException(message: "Embedded sail mount is missing.");
                using (var reader = new BinaryReader(input: stream))
                {
                    if (reader.ReadInt32() != 0x314C534D)
                        throw new InvalidDataException(message: "Invalid sail mount header.");
                    int count = reader.ReadInt32(),
                        indices = reader.ReadInt32();
                    if (
                        count <= 0
                        || count > 65535
                        || indices <= 0
                        || indices % 3 != 0
                        || stream.Length != HeaderSize + count * 36L + indices / 3L * 16L
                    )
                        throw new InvalidDataException(message: "Invalid sail mount size.");
                    stream.Position = HeaderSize;
                    Positions = new Vector3[count];
                    Normals = new Vector3[count];
                    UV = new Vector2[count];
                    Parts = new int[count];
                    var minimum = new float[15];
                    var maximum = new float[15];
                    for (int i = 0; i < 15; i++)
                    {
                        minimum[i] = float.PositiveInfinity;
                        maximum[i] = float.NegativeInfinity;
                    }
                    for (int i = 0; i < count; i++)
                    {
                        Positions[i] = new Vector3(
                            reader.ReadSingle(),
                            reader.ReadSingle(),
                            reader.ReadSingle()
                        );
                        UV[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                        Normals[i] = new Vector3(
                            reader.ReadSingle(),
                            reader.ReadSingle(),
                            reader.ReadSingle()
                        );
                        Parts[i] = reader.ReadInt32();
                        if (
                            Parts[i] < 0
                            || Parts[i] > 14
                            || !Finite(point: Positions[i])
                            || !Finite(point: Normals[i])
                            || !SpritsailDeployment.Finite(value: UV[i].x)
                            || !SpritsailDeployment.Finite(value: UV[i].y)
                            || Math.Abs(Normals[i].sqrMagnitude - 1) > 0.001f
                        )
                            throw new InvalidDataException(message: "Invalid sail mount vertex.");
                        int vertexPart = Parts[i];
                        minimum[vertexPart] = Math.Min(minimum[vertexPart], Positions[i].x);
                        maximum[vertexPart] = Math.Max(maximum[vertexPart], Positions[i].x);
                    }
                    var buckets = new[]
                    {
                        new List<int>(),
                        new List<int>(),
                        new List<int>(),
                        new List<int>(),
                    };
                    var regions = new int[15];
                    for (int i = 0; i < indices; i += 3)
                    {
                        int a = reader.ReadInt32(),
                            b = reader.ReadInt32(),
                            c = reader.ReadInt32();
                        int material = reader.ReadInt32();
                        if (
                            a < 0
                            || a >= count
                            || b < 0
                            || b >= count
                            || c < 0
                            || c >= count
                            || material < 0
                            || material > 3
                            || Parts[a] != Parts[b]
                            || Parts[a] != Parts[c]
                        )
                            throw new InvalidDataException(message: "Invalid sail mount triangle.");
                        int part = Parts[a];
                        if (
                            part == 0 ? material != 0
                            : part >= 8 ? material != 3
                            : material != 1 && material != 2
                        )
                            throw new InvalidDataException(
                                message: "Invalid sail mount material region."
                            );
                        regions[part] |= 1 << material;
                        buckets[material].Add(item: a);
                        buckets[material].Add(item: b);
                        buckets[material].Add(item: c);
                    }
                    for (int part = 0; part < 15; part++)
                        if (
                            regions[part]
                            != (
                                part == 0 ? 1
                                : part >= 8 ? 8
                                : 6
                            )
                        )
                            throw new InvalidDataException(
                                message: "Missing sail mount part/material."
                            );
                    Triangles = new int[4][];
                    for (int i = 0; i < 4; i++)
                        Triangles[i] = buckets[i].ToArray();
                    for (int i = 0; i < 7; i++)
                    {
                        EyeletX[i] = (minimum[i + 1] + maximum[i + 1]) * 0.5f;
                        // Authored loops lie in Y/Z: their X extent measures tube
                        // diameter independently of the loop's shape and position.
                        RopeRadii[i] = (maximum[i + 8] - minimum[i + 8]) * 0.5f;
                        if (!SpritsailDeployment.Finite(value: RopeRadii[i]) || RopeRadii[i] <= 0)
                            throw new InvalidDataException(
                                message: "Invalid sail mount rope thickness."
                            );
                    }
                }
            }
        }
    }
}
