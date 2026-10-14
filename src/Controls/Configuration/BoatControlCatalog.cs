// Resolves authored controls configuration independently of other features.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Controls.Configuration.Boats;

namespace MoreSailwindSails.Controls.Configuration
{
    internal static class BoatControlCatalog
    {
        internal static readonly IReadOnlyList<BoatControlDefinition> All = Array.AsReadOnly(
            new[]
            {
                BrigControls.Definition,
                JunkControls.Definition,
                JongControls.Definition,
                SanbuqControls.Definition,
                CogControls.Definition,
                ShroudControls.Definition,
                LargeDhowControls.Definition,
                GlorianaControls.Definition,
                ChronianControls.Definition,
                CaelanorControls.Definition,
                KakamControls.Definition,
                DhowControls.Definition,
            }
        );

        internal static BoatControlDefinition Find(string boatName)
        {
            boatName = BoatConfiguration.BoatCatalog.NormalizeName(boatName: boatName);
            return All.FirstOrDefault(d => d.BoatName == boatName);
        }
    }
}
