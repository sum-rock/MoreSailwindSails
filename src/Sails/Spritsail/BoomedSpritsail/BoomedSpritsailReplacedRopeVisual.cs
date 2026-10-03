using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.BoomedSpritsail
{
    // Suppresses this clone's obsolete gaff topping lift and replaced reef visuals;
    // the native single sheet remains visible and all control input stays native.
    internal sealed class BoomedSpritsailReplacedRopeVisual : MonoBehaviour
    {
        private LineRenderer nativeLine;
        private ClothRope nativeCloth;

        internal void Suppress(LineRenderer line, ClothRope cloth)
        {
            nativeLine = line;
            nativeCloth = cloth;
            Hide();
        }

        private void Hide()
        {
            if (nativeLine)
                nativeLine.enabled = false;
            if (nativeCloth)
                nativeCloth.gameObject.SetActive(value: false);
        }

        private void OnDisable() => Hide();
    }
}
