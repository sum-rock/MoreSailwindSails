using System;
using System.Linq;
using MoreSailwindSails.BoatRigs;

namespace MoreSailwindSails.Controls
{
    // Keeps startup control assets independent of geometry donors and placement.
    internal sealed class WinchBootstrapTemplates
    {
        private readonly NativeWinchSeats native;

        internal WinchBootstrapTemplates(NativeWinchSeats native)
        {
            this.native = native;
        }

        internal bool TryGet(
            Mast forward,
            Mast halyard,
            out GPButtonRopeWinch[] templates,
            out string failure
        )
        {
            templates = null;
            var category = forward ? native.Profile?.SheetCategory(forward.orderIndex) : null;
            var pair =
                category == null
                    ? null
                    : WinchBootstrapPolicy.Sheets(
                        sources: category.Sources,
                        port: id =>
                            NativeWinchSeats.Sources(mast: native.Mast(id), role: WinchRole.Left),
                        starboard: id =>
                            NativeWinchSeats.Sources(mast: native.Mast(id), role: WinchRole.Right),
                        usable: native.TemplateUsable,
                        fallback: () => Fallback(category: category)
                    );
            var reef = NativeWinchSeats
                .Sources(mast: halyard, role: WinchRole.Reef)
                ?.FirstOrDefault(native.TemplateUsable);
            failure =
                pair == null
                    ? $"sheet template pair unavailable: category={category?.Name ?? "missing"}, forward={(forward ? forward.orderIndex : -1)}; ordered native sources and fallback exhausted"
                : !reef
                    ? $"halyard template unavailable: requestedMast={(halyard ? halyard.orderIndex : -1)}"
                : null;
            if (failure != null)
                return false;
            templates = new[] { reef, pair[0], pair[1] };
            return true;
        }

        private GPButtonRopeWinch[] Fallback(SheetWinchCategory category)
        {
            if (category.Fallback == null || category.Fallback.InvalidSide != null)
                return null;
            var result = new GPButtonRopeWinch[2];
            var definitions = new[] { category.Fallback.Port, category.Fallback.Starboard };
            for (int i = 0; i < 2; i++)
            {
                var d = definitions[i];
                var controls = NativeWinchSeats.Sources(
                    mast: native.Mast(d.TemplateMast),
                    role: d.TemplateRole
                );
                if (controls == null || d.TemplateIndex >= controls.Length)
                    return null;
                result[i] = controls[d.TemplateIndex];
            }
            return result;
        }
    }
}
