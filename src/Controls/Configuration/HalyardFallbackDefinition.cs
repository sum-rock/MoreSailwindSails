// Associates an authored halyard fallback with its aft physical mast.
using System;

namespace MoreSailwindSails.Controls.Configuration
{
    internal sealed class HalyardFallbackDefinition
    {
        internal readonly int Mast;
        internal readonly HalyardFallbackSeat Fallback;

        internal HalyardFallbackDefinition(int mast, HalyardFallbackSeat fallback)
        {
            if (mast < 0 || mast >= 128 || fallback == null || !fallback.Valid)
                throw new ArgumentException("Invalid halyard fallback.");
            Mast = mast;
            Fallback = fallback;
        }
    }
}
