// Interpolates authored sheet/reef surfaces and retargets them to the fitted stay.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    internal sealed class FishermansStaysailAnimation
    {
        private readonly FishermansStaysailAnimationData data;
        private readonly float width,
            headDelta;
        internal readonly Vector3[] Vertices;
        internal int[] CornerIndices => data.CornerIndices;

        internal FishermansStaysailAnimation(
            FishermansStaysailAnimationData data,
            float width,
            float headSlope
        )
        {
            FishermansStaysailGeometry.ValidateWidth(width: width);
            this.data = data;
            this.width = width;
            headDelta =
                (float)Math.Tan(headSlope * Math.PI / 180) - (float)Math.Tan(20 * Math.PI / 180);
            Vertices = new Vector3[data.Sheet[0].Length];
        }

        internal void Pose(float deployment, float ease, float side)
        {
            float reef = 1 - Mathf.Clamp01(deployment);
            ease = Mathf.Clamp01(ease);
            int left = 0;
            while (left < data.ReefFractions.Length - 2 && reef > data.ReefFractions[left + 1])
                left++;
            int right = left + 1;
            float blend = Mathf.Clamp01(
                (reef - data.ReefFractions[left])
                    / (data.ReefFractions[right] - data.ReefFractions[left])
            );
            float middle = (1 - Math.Abs(2 * ease - 1)) * Mathf.Clamp01(1 - reef / 0.25f);
            for (int i = 0; i < Vertices.Length; i++)
            {
                var inside = Vector3.Lerp(
                    a: data.ReefIn[left][i],
                    b: data.ReefIn[right][i],
                    t: blend
                );
                var outside = Vector3.Lerp(
                    a: data.ReefOut[left][i],
                    b: data.ReefOut[right][i],
                    t: blend
                );
                var point = Vector3.Lerp(a: inside, b: outside, t: ease);
                point += (data.Sheet[1][i] - (data.Sheet[0][i] + data.Sheet[2][i]) * 0.5f) * middle;
                // Follow gathered cloth up the fitted head slope; the source's
                // lower edge follows this cut and its head rises toward the aft mast.
                float along = Mathf.Clamp01(point.z + 1);
                float foot = Mathf.Lerp(data.FootFore, data.FootAft, along);
                float height = Mathf.Clamp01(
                    (point.x - foot) / (along * (float)Math.Tan(20 * Math.PI / 180) - foot)
                );
                point.x += headDelta * along * height;
                point.y *= Math.Max(-1, Math.Min(1, side));
                Vertices[i] = point * width;
            }
        }

        internal static float SheetEase(float angle, float minimum, float maximum)
        {
            float limit = angle < 0 ? -minimum : maximum;
            return limit > 0.001f ? Mathf.Clamp01(Math.Abs(angle) / limit) : 0;
        }
    }

    internal struct FishermansStaysailAnimationData
    {
        internal float FootFore,
            FootAft;
        internal int[] CornerIndices;
        internal float[] ReefFractions;
        internal Vector3[][] Sheet,
            ReefIn,
            ReefOut;
    }
}
