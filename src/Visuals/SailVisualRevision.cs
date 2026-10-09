using System;
using UnityEngine;

namespace MoreSailwindSails.Visuals
{
    // Produces a per-sail revision from deliberate control/fitting changes for independent consumers.
    internal sealed class SailVisualRevision
    {
        internal const int Initial = 1,
            Sheet = 2,
            Reef = 4,
            Tack = 8,
            Fitting = 16,
            Support = 32,
            Reactivate = 64;
        internal long Revision { get; private set; }
        internal int Reasons { get; private set; }
        private SailVisualState previous;
        private Vector3[] corners;
        private bool initialized;

        internal static int TackSide(float angle, int previous) =>
            angle > 5 ? 1
            : angle < -5 ? -1
            : previous;

        internal long Update(SailVisualState state, Vector3[] shape)
        {
            int reasons = initialized ? 0 : Initial;
            if (initialized)
            {
                if (
                    !state.Sheets.Equals(previous.Sheets)
                    || !ReferenceEquals(state.Left, previous.Left)
                    || !ReferenceEquals(state.Mid, previous.Mid)
                    || !ReferenceEquals(state.Right, previous.Right)
                )
                    reasons |= Sheet;
                if (state.Unroll != previous.Unroll)
                    reasons |= Reef;
                if (state.Tack != previous.Tack)
                    reasons |= Tack;
                if (
                    !state.Scale.Equals(previous.Scale)
                    || !state.MastScale.Equals(previous.MastScale)
                    || state.Height != previous.Height
                )
                    reasons |= Fitting;
                if (
                    !ReferenceEquals(state.Mast, previous.Mast)
                    || !ReferenceEquals(state.Mesh, previous.Mesh)
                )
                    reasons |= Support;
                if (state.Active && !previous.Active)
                    reasons |= Reactivate;
            }
            if (corners == null || corners.Length != shape.Length)
            {
                corners = new Vector3[shape.Length];
                reasons |= Fitting;
            }
            for (int i = 0; i < shape.Length; i++)
                if (!shape[i].Equals(corners[i]))
                    reasons |= Fitting;
            Array.Copy(sourceArray: shape, destinationArray: corners, length: shape.Length);
            previous = state;
            initialized = true;
            Reasons = reasons;
            if (reasons != 0)
                Revision++;
            return Revision;
        }

        internal void Reset() => initialized = false;
    }
}
