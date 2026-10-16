// Discovers one shared sheet fallback donor without activating or claiming its native control.
using MoreSailwindSails.BoatConfiguration;
using MoreSailwindSails.Controls.Configuration;
using UnityEngine;

namespace MoreSailwindSails.Controls
{
    internal static class SharedSheetWinchTemplate
    {
        private static GPButtonRopeWinch template;
        private static readonly WinchDiscoverySchedule discovery = new WinchDiscoverySchedule();

        internal static GPButtonRopeWinch Get()
        {
            if (Usable(control: template))
                return template;
            template = null;
            if (!discovery.Due(now: Time.unscaledTime))
                return null;
            discovery.Discovered(now: Time.unscaledTime);

            // Include inactive boats and unfitted masts; a template does not need
            // a free native seat. Destroyed scene objects naturally invalidate the cache.
            foreach (var boat in Resources.FindObjectsOfTypeAll<BoatRefs>())
            {
                if (
                    !boat.gameObject.scene.IsValid()
                    || BoatCatalog.NormalizeName(boatName: boat.name)
                        != SheetFallbackTemplate.BoatName
                )
                    continue;
                foreach (var mast in boat.GetComponentsInChildren<Mast>(includeInactive: true))
                {
                    if (mast.orderIndex != SheetFallbackTemplate.Mast)
                        continue;
                    var sources = NativeWinchSeats.Sources(
                        mast: mast,
                        role: SheetFallbackTemplate.Role
                    );
                    if (sources == null || sources.Length <= SheetFallbackTemplate.Index)
                        continue;
                    var source = sources[SheetFallbackTemplate.Index];
                    if (!Usable(control: source))
                        continue;
                    template = source;
                    return template;
                }
            }
            return null;
        }

        private static bool Usable(GPButtonRopeWinch control) =>
            NativeWinchSeats.Usable(c: control)
            && !control.GetComponent<FishermanOwnedWinchMarker>();
    }
}
