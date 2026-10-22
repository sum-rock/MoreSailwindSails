// Describes the live supports, source policy and existing rope controllers of a consumer.
using UnityEngine;

namespace MoreSailwindSails.Controls
{
    internal struct SailControlRequest
    {
        internal GameObject Owner;
        internal BoatRefs Boat;

        // NativeMount is a dedicated mount used only for native rebind notifications.
        internal Mast Forward,
            Halyard,
            NativeMount,
            SourceMast;
        internal Mast[] Supports;
        internal NativeControlSourcePolicy SourcePolicy;
        internal float? FittedSheetSpan;
        internal RopeController Left,
            Right,
            Reef;
    }
}
