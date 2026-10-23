// Owns cloned winch scene resources and controller detachment before replacement.
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Controls.Runtime
{
    internal sealed class WinchClone
    {
        internal GPButtonRopeWinch Source,
            Winch;
        internal Transform Mount;

        internal void Detach(BoatRefs boat)
        {
            if (Winch && Winch.rope && Winch.rope.transform.parent == Mount && boat)
                Winch.rope.transform.SetParent(parent: boat.transform, worldPositionStays: true);
        }

        internal void Destroy()
        {
            if (Winch)
            {
                Winch.rope = null;
                if (Winch.rotHandle)
                    Object.Destroy(Winch.rotHandle.gameObject);
            }
            if (Mount)
            {
                Mount.gameObject.SetActive(false);
                Object.Destroy(Mount.gameObject);
            }
        }
    }
}
