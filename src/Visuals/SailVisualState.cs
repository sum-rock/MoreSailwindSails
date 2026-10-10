using UnityEngine;

namespace MoreSailwindSails.Visuals
{
    // Logical fitting inputs only: world motion, load and native sway are deliberately absent.
    internal readonly struct SailVisualState
    {
        internal readonly Vector3 Sheets;
        internal readonly float Unroll;
        internal readonly int Tack;
        internal readonly Vector3 Scale;
        internal readonly Vector3 MastScale;
        internal readonly float Height;
        internal readonly object Mast;
        internal readonly object Mesh;
        internal readonly object Left;
        internal readonly object Mid;
        internal readonly object Right;
        internal readonly bool Active;

        internal SailVisualState(
            Vector3 sheets,
            float unroll,
            int tack,
            Vector3 scale,
            Vector3 mastScale,
            float height,
            object mast,
            object mesh,
            object left,
            object mid,
            object right,
            bool active
        )
        {
            Sheets = sheets;
            Unroll = unroll;
            Tack = tack;
            Scale = scale;
            MastScale = mastScale;
            Height = height;
            Mast = mast;
            Mesh = mesh;
            Left = left;
            Mid = mid;
            Right = right;
            Active = active;
        }
    }
}
