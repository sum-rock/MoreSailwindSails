// Resolves authored stays configuration independently of other features.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Stays.FishermansStay.Configuration.Boats;

namespace MoreSailwindSails.Stays.FishermansStay.Configuration
{
    internal static class FishermansStayCatalog
    {
        internal static readonly IReadOnlyList<FishermansStayDefinition> All = Array.AsReadOnly(
            new[]
            {
                BrigStays.Definition,
                JunkStays.Definition,
                JongStays.Definition,
                SanbuqStays.Definition,
                CogStays.Definition,
                ShroudStays.Definition,
                LargeDhowStays.Definition,
                GlorianaStays.Definition,
                ChronianStays.Definition,
                CaelanorStays.Definition,
                KakamStays.Definition,
                DhowStays.Definition,
            }
        );

        internal static FishermansStayDefinition Find(string boatName)
        {
            boatName = BoatConfiguration.BoatCatalog.NormalizeName(boatName: boatName);
            return All.FirstOrDefault(d => d.BoatName == boatName);
        }
    }
}
