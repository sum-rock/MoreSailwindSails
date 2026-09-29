using System;

namespace MoreSailwindSails.Sails.FishermansFlyingSail
{
    // Validates optional native knot channels and preserves their selected vertex mapping.
    internal static class FishermansFlyingSailKnotChannels
    {
        internal static bool Validate(
            int vertexCount,
            int normalCount,
            int uvCount,
            string shaderName,
            bool hasAssignedTextures
        )
        {
            if (vertexCount <= 0)
                throw new ArgumentException("Native knot vertices are missing.");
            if (normalCount != vertexCount)
                throw new ArgumentException(
                    $"Native knot normal count {normalCount} does not match vertex count {vertexCount}."
                );
            if (uvCount == vertexCount)
                return true;
            if (uvCount != 0)
                throw new ArgumentException(
                    $"Native knot UV count {uvCount} does not match vertex count {vertexCount}."
                );
            if (shaderName != "Standard" || hasAssignedTextures)
                throw new ArgumentException(
                    "Native knot UVs are missing; only an untextured Standard material supports UV-less knots."
                );
            return false;
        }

        internal static T[] Remap<T>(T[] channel, int[] selected)
        {
            var result = new T[selected.Length];
            for (int i = 0; i < selected.Length; i++)
                result[i] = channel[selected[i]];
            return result;
        }
    }
}
