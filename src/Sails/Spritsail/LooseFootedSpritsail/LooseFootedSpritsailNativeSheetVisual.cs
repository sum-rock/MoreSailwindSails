using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail
{
    // Added only to this sail's own sheet controllers. Native tension and input
    // continue running; the support-line renderer owns their visible routes.
    internal sealed class LooseFootedSpritsailNativeSheetVisual : MonoBehaviour
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
