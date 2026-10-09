using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Tracks each part's last fitted inputs; unrelated updates cannot consume small movements.
    internal sealed class SpritsailMountFitState
    {
        internal const int Initial = 1;
        internal const int Luff = 2;
        internal const int Eyelet = 4;
        internal const int Mast = 8;
        internal const int Radius = 16;
        internal const int Support = 32;
        private readonly Vector3[] stripLuff;
        private readonly Vector3[] eyelets = new Vector3[7];
        private readonly Vector3[] ropeEyes = new Vector3[7];
        private readonly Vector3[] origins = new Vector3[7];
        private readonly Vector3[] axes = new Vector3[7];
        private readonly float[] radii = new float[7];
        private int fittedParts;

        internal SpritsailMountFitState(int luffCount)
        {
            stripLuff = new Vector3[luffCount];
        }

        private static bool Moved(Vector3 a, Vector3 b) => (a - b).sqrMagnitude > 0.00000001f;

        internal int Changes(
            Vector3[] luff,
            Vector3 origin,
            Vector3 axis,
            float[] currentRadii,
            bool supportChanged,
            out int reasons
        )
        {
            reasons = 0;
            if (fittedParts != SpritsailMountGeometry.AllParts || supportChanged)
            {
                reasons =
                    (fittedParts != SpritsailMountGeometry.AllParts ? Initial : 0)
                    | (supportChanged ? Support : 0);
                return SpritsailMountGeometry.AllParts;
            }
            int parts = 0;
            for (int row = 0; row < luff.Length; row++)
                if (Moved(a: luff[row], b: stripLuff[row]))
                {
                    parts |= 1;
                    reasons |= Luff;
                    break;
                }
            for (int eye = 0; eye < 7; eye++)
            {
                var point = SpritsailMountGeometry.Eyelet(
                    points: luff,
                    sourceX: SpritsailMountAsset.EyeletX[eye]
                );
                if (Moved(a: point, b: eyelets[eye]))
                {
                    parts |= 1 << (eye + 1);
                    reasons |= Eyelet;
                }
                bool moved = Moved(a: point, b: ropeEyes[eye]);
                bool mast = Moved(a: origin, b: origins[eye]) || Moved(a: axis, b: axes[eye]);
                bool radius = Mathf.Abs(f: currentRadii[eye] - radii[eye]) > 0.0001f;
                if (moved || mast || radius)
                {
                    parts |= 1 << (eye + 8);
                    reasons |= (moved ? Eyelet : 0) | (mast ? Mast : 0) | (radius ? Radius : 0);
                }
            }
            return parts;
        }

        internal void Commit(
            int parts,
            Vector3[] luff,
            Vector3 origin,
            Vector3 axis,
            float[] currentRadii
        )
        {
            if ((parts & 1) != 0)
                Array.Copy(sourceArray: luff, destinationArray: stripLuff, length: luff.Length);
            for (int eye = 0; eye < 7; eye++)
            {
                var point = SpritsailMountGeometry.Eyelet(
                    points: luff,
                    sourceX: SpritsailMountAsset.EyeletX[eye]
                );
                if ((parts & (1 << (eye + 1))) != 0)
                    eyelets[eye] = point;
                if ((parts & (1 << (eye + 8))) == 0)
                    continue;
                ropeEyes[eye] = point;
                origins[eye] = origin;
                axes[eye] = axis;
                radii[eye] = currentRadii[eye];
            }
            fittedParts |= parts;
        }
    }
}
