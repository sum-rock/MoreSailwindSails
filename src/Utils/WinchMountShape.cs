using UnityEngine;

namespace MoreSailwindSails.Utils
{
    // A source transform and cached local line endpoints; owns no game mesh or renderer.
    internal sealed class WinchMountShape
    {
        internal readonly Transform Transform;
        internal readonly Vector3[] Edges;

        internal WinchMountShape(Transform transform, Vector3[] edges)
        {
            Transform = transform;
            Edges = edges;
        }
    }
}
