// Resolves authored supports configuration independently of other features.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.Sails.FishermansFlyingSail.Configuration.Boats;

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Configuration
{
    internal static class FlyingSailCatalog
    {
        internal static readonly IReadOnlyList<FlyingSailDefinition> All = Array.AsReadOnly(
            new[]
            {
                BrigFlyingSail.Definition,
                JunkFlyingSail.Definition,
                JongFlyingSail.Definition,
                SanbuqFlyingSail.Definition,
                CogFlyingSail.Definition,
                ShroudFlyingSail.Definition,
                LargeDhowFlyingSail.Definition,
                GlorianaFlyingSail.Definition,
                ChronianFlyingSail.Definition,
                CaelanorFlyingSail.Definition,
                KakamFlyingSail.Definition,
                DhowFlyingSail.Definition,
            }
        );

        internal static FlyingSailDefinition Find(string boatName)
        {
            boatName = BoatConfiguration.BoatCatalog.NormalizeName(boatName: boatName);
            return All.FirstOrDefault(d => d.BoatName == boatName);
        }
    }
}
