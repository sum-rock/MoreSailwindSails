using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatRigs;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

internal static class ProfileChecks
{
    internal static void Run()
    {
        var leopard = Leopard.Definition;
        if (
            !ReferenceEquals(BoatRigCatalog.Find("BOAT LEOPARD (207)(Clone)(Clone)"), leopard)
            || !leopard.Sections(12).SequenceEqual(new[] { 12, 11, 10 })
            || leopard.Base(12) != 10
            || !Brig.Definition.Sections(56).SequenceEqual(new[] { 56, 5 })
            || Brig.Definition.Sections(-1).Length != 0
        )
            throw new Exception("Profile lookup or ordered mast ancestry changed.");

        var largeDhow = LargeDhow.Definition;
        if (
            !ReferenceEquals(BoatRigCatalog.Find("BOAT dhow large (30)(Clone)"), largeDhow)
            || !largeDhow.Sections(3).SequenceEqual(new[] { 3, 2 })
            || !largeDhow.Sections(5).SequenceEqual(new[] { 5, 4 })
        )
            throw new Exception("Large dhow topmasts lost their matching lower sections.");
        // Every native combination gets exactly one stay per adjacent mast pair.
        // Adding a topmast replaces the forward stay without disabling the aft stay.
        foreach (int fore in new[] { 0, 1 })
        foreach (int main in new[] { 2, 4 })
        foreach (bool topmast in new[] { false, true })
        foreach (int mizzen in new[] { 6, 7, 8 })
        {
            var active = new HashSet<int> { fore, main, mizzen };
            if (topmast)
                active.Add(main + 1);
            foreach (var group in largeDhow.Stays)
                if (
                    group.Variants.Count(s =>
                        s.Required.All(active.Contains) && !s.Forbidden.Any(active.Contains)
                    ) != 1
                )
                    throw new Exception(
                        "Large dhow mast combination has missing or ambiguous stays."
                    );
        }

        Reject<ArgumentException>(() => leopard.Sections(127));
        Reject<ArgumentException>(() => leopard.Base(127));
        Reject<InvalidOperationException>(() => leopard.WinchMount(127, WinchRole.Reef));
        foreach (var boat in BoatRigCatalog.All)
        foreach (var winch in boat.WinchMounts)
        {
            if (!ReferenceEquals(boat.WinchMount(winch.Mast, winch.Role), winch))
                throw new Exception("Winch lookup escaped its containing boat profile.");
            bool upperDhowRow =
                ReferenceEquals(boat, largeDhow)
                && winch.Role == WinchRole.Reef
                && new[] { 0, 1, 2, 4 }.Contains(winch.Mast);
            if (winch.SourceIndex != (upperDhowRow ? 2 : -1))
                throw new Exception(
                    "Authored winch source selection changed outside large-dhow stacked rows."
                );
            bool jongForeSheet =
                ReferenceEquals(boat, Jong.Definition)
                && winch.Mast == 10
                && winch.Role is WinchRole.Left or WinchRole.Right;
            if (winch.SourceMast != (jongForeSheet ? 7 : -1))
                throw new Exception("Unexpected native mast override outside Jong fore sheets.");
        }
        Reject<ArgumentOutOfRangeException>(() =>
            new WinchMountDefinition(0, WinchRole.Reef, UnityEngine.Vector3.up, true, 0, -2)
        );
        Reject<ArgumentOutOfRangeException>(() =>
            new WinchMountDefinition(
                0,
                WinchRole.Left,
                UnityEngine.Vector3.up,
                0.05f,
                -2,
                new WinchSurfaceSegment(UnityEngine.Vector3.zero, UnityEngine.Vector3.forward)
            )
        );

        BoatRigDefinition Profile(
            IReadOnlyDictionary<int, int> parents,
            params WinchMountDefinition[] winches
        ) =>
            new BoatRigDefinition(
                "test",
                Brig.Definition.Supports,
                Array.Empty<FishermansStayGroupDefinition>(),
                parents,
                winches
            );

        Reject<ArgumentException>(() => Profile(new Dictionary<int, int> { { 0, 1 } }));
        Reject<ArgumentException>(() => Profile(new Dictionary<int, int> { { 0, 0 } }));
        Reject<ArgumentException>(() => Profile(new Dictionary<int, int> { { 0, 1 }, { 1, 0 } }));
        Reject<ArgumentException>(() => Profile(new Dictionary<int, int> { { 0, -2 } }));
        var parents = new Dictionary<int, int> { { 0, -1 } };
        var profile = Profile(parents);
        parents[0] = 0;
        if (!profile.Sections(0).SequenceEqual(new[] { 0 }))
            throw new Exception("Caller mutation corrupted validated mast ancestry.");
        var sample = Brig.Definition.WinchMounts[0];
        Reject<ArgumentException>(() => Profile(new Dictionary<int, int>(), sample, sample));
        Console.WriteLine(
            "PASS: complete boat-profile lookup, ordered mast ancestry, missing entries, duplicate winches and cyclic/missing-parent rejection."
        );
    }

    private static void Reject<T>(Action action)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new Exception("Expected " + typeof(T).Name + " for invalid profile lookup/data.");
    }
}
