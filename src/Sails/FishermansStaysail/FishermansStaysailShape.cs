using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail
{
    // Each mark supplies a cut and labels; mounting, reefing and sheeting stay
    // in the family rig. This component is copied with the native sail prefab.
    internal abstract class FishermansStaysailShape : MonoBehaviour
    {
        internal abstract string ObjectPrefix { get; }
        internal abstract FishermansStaysailMeshData Create(float width, float headSlope);
    }
}
