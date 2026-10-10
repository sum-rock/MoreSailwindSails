using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Caches immutable authored geometry calculations, shared by mounts with the same luff size.
    internal sealed class SpritsailMountFitData
    {
        private static readonly Dictionary<int, SpritsailMountFitData> cache =
            new Dictionary<int, SpritsailMountFitData>();
        internal readonly int[][] Indices = new int[15][];
        internal readonly StripSample[] Strip;
        internal readonly RopeVertex[][] Ropes = new RopeVertex[7][];
        internal readonly LuffSample[] Eyelets = new LuffSample[7];

        internal static SpritsailMountFitData ForLuffCount(int count)
        {
            if (!cache.TryGetValue(key: count, value: out var data))
            {
                data = new SpritsailMountFitData(luffCount: count);
                cache.Add(key: count, value: data);
            }
            return data;
        }

        private SpritsailMountFitData(int luffCount)
        {
            if (luffCount < 2)
                throw new ArgumentOutOfRangeException(paramName: nameof(luffCount));
            var buckets = new List<int>[15];
            for (int part = 0; part < buckets.Length; part++)
                buckets[part] = new List<int>();
            for (int i = 0; i < SpritsailMountAsset.Parts.Length; i++)
                buckets[SpritsailMountAsset.Parts[i]].Add(item: i);
            for (int part = 0; part < buckets.Length; part++)
                Indices[part] = buckets[part].ToArray();
            for (int eye = 0; eye < 7; eye++)
            {
                Eyelets[eye] = new LuffSample(
                    sourceX: SpritsailMountAsset.EyeletX[eye],
                    count: luffCount
                );
                Ropes[eye] = new RopeVertex[Indices[eye + 8].Length];
                for (int i = 0; i < Ropes[eye].Length; i++)
                    Ropes[eye][i] = new RopeVertex(index: Indices[eye + 8][i], eye: eye);
            }
            Strip = new StripSample[Indices[0].Length * 5];
            for (int i = 0; i < Indices[0].Length; i++)
            {
                var source = SpritsailMountAsset.Positions[Indices[0][i]];
                Strip[i * 5] = new StripSample(source: source, count: luffCount);
                Strip[i * 5 + 1] = new StripSample(
                    source: source + Vector3.right * 0.001f,
                    count: luffCount
                );
                Strip[i * 5 + 2] = new StripSample(
                    source: source - Vector3.right * 0.001f,
                    count: luffCount
                );
                Strip[i * 5 + 3] = new StripSample(
                    source: source + Vector3.forward * 0.001f,
                    count: luffCount
                );
                Strip[i * 5 + 4] = new StripSample(
                    source: source - Vector3.forward * 0.001f,
                    count: luffCount
                );
            }
        }

        internal readonly struct LuffSample
        {
            private readonly int first;
            private readonly float weight;

            internal LuffSample(float sourceX, int count)
            {
                float row = Mathf.Clamp01(value: (3 - sourceX) / 6) * (count - 1);
                first = Math.Min(count - 2, (int)row);
                weight = row - first;
            }

            internal Vector3 Evaluate(Vector3[] luff) =>
                Vector3.Lerp(a: luff[first], b: luff[first + 1], t: weight);
        }

        internal readonly struct StripSample
        {
            private readonly LuffSample luff;
            private readonly Vector3 offset;
            private readonly Vector3 rigidOffset;
            private readonly int eye;
            private readonly float blend;

            internal StripSample(Vector3 source, int count)
            {
                luff = new LuffSample(sourceX: source.x, count: count);
                offset =
                    -Vector3.up * source.y
                    + Vector3.forward * (SpritsailMountGeometry.SeamOverlap - source.z - 0.1f);
                eye = -1;
                blend = 0;
                rigidOffset = Vector3.zero;
                for (int i = 0; i < 7; i++)
                {
                    float x = SpritsailMountAsset.EyeletX[i];
                    float distance = Mathf.Sqrt(
                        f: (source.x - x) * (source.x - x) + (source.z + 0.05f) * (source.z + 0.05f)
                    );
                    if (distance >= 0.049f)
                        continue;
                    eye = i;
                    float weight = Mathf.Clamp01(value: (distance - 0.025f) / 0.024f);
                    blend = weight * weight * (3 - 2 * weight);
                    rigidOffset = new Vector3(source.x - x, -source.y, -(source.z + 0.05f));
                    break;
                }
            }

            internal Vector3 Evaluate(Vector3[] points, Vector3[] eyelets)
            {
                var point = luff.Evaluate(luff: points) + offset;
                return eye < 0
                    ? point
                    : Vector3.Lerp(a: eyelets[eye] + rigidOffset, b: point, t: blend);
            }
        }

        internal readonly struct RopeVertex
        {
            internal readonly float Axial;
            internal readonly Vector3 Normal;
            internal readonly RopeSample Point;
            internal readonly RopeSample Before;
            internal readonly RopeSample After;

            internal RopeVertex(int index, int eye)
            {
                var normal = SpritsailMountAsset.Normals[index];
                var path =
                    SpritsailMountAsset.Positions[index]
                    - new Vector3(SpritsailMountAsset.EyeletX[eye], 0, 0)
                    - normal * SpritsailMountAsset.RopeRadii[eye];
                float angle = (float)Math.Atan2(path.y / 0.2f, (0.23f - path.z) / 0.275f);
                Point = new RopeSample(angle: angle);
                Before = new RopeSample(angle: angle - 0.001f);
                After = new RopeSample(angle: angle + 0.001f);
                Axial = path.x;
                var radial = new Vector3(
                    0,
                    path.y / (0.2f * 0.2f),
                    (path.z - 0.23f) / (0.275f * 0.275f)
                ).normalized;
                Normal = new Vector3(
                    normal.x,
                    Vector3.Dot(lhs: normal, rhs: radial),
                    Vector3.Dot(lhs: normal, rhs: Vector3.Cross(lhs: Vector3.right, rhs: radial))
                );
            }
        }

        internal readonly struct RopeSample
        {
            internal readonly bool Collar;
            internal readonly float Cos;
            internal readonly float Sin;
            internal readonly float Sign;
            internal readonly float A;
            internal readonly float B;
            internal readonly float C;
            internal readonly float D;

            internal RopeSample(float angle)
            {
                Collar = Math.Abs(angle) >= Math.PI / 2;
                Cos = (float)Math.Cos(angle);
                Sin = (float)Math.Sin(angle);
                Sign = angle < 0 ? -1 : 1;
                float t = (float)(Math.Abs(angle) / (Math.PI / 2));
                float rest = 1 - t;
                A = rest * rest * rest;
                B = 3 * rest * rest * t;
                C = 3 * rest * t * t;
                D = t * t * t;
            }
        }
    }
}
