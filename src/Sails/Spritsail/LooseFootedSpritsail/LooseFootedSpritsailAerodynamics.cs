using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail
{
    // Derives the aerodynamic sensor frame from posed sail corners.

    internal static class LooseFootedSpritsailAerodynamics
    {
        // Native staysail wind sensors use right=along the sail, forward=up
        // the mast, and up=normal to the sail. The procedural mesh uses a
        // different frame; never inherit the donor sensor's local rotation.
        internal static bool TryFrame(
            Vector3 foreHead,
            Vector3 foreTack,
            Vector3 aftHead,
            Vector3 clew,
            out LooseFootedSpritsailWindFrame frame
        )
        {
            frame = default;
            var mast = foreHead - foreTack;
            if (mast.sqrMagnitude < 1e-8f)
                return false;
            mast = mast.normalized;
            var chord = (aftHead + clew - foreHead - foreTack) * 0.5f;
            chord -= mast * Vector3.Dot(chord, mast);
            if (chord.sqrMagnitude < 1e-8f)
                return false;
            chord = chord.normalized;
            float first = Vector3.Cross(foreTack - foreHead, aftHead - foreHead).magnitude;
            float second = Vector3.Cross(foreTack - aftHead, clew - aftHead).magnitude;
            if (first + second < 1e-8f)
                return false;
            frame = new LooseFootedSpritsailWindFrame
            {
                AlongSail = chord,
                MastAxis = mast,
                Normal = Vector3.Cross(mast, chord).normalized,
                Center =
                    ((foreHead + foreTack + aftHead) * first + (aftHead + foreTack + clew) * second)
                    / (3 * (first + second)),
            };
            return true;
        }

        internal static Vector3 ForceDirection(Vector3 wind, Vector3 normal) =>
            Vector3.Dot(wind, normal) >= 0 ? normal : -normal;
    }
}
