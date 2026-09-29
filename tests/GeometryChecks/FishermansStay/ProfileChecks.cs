using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MoreSailwindSails.BoatRigs;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStay;

internal static class ProfileChecks
{
    internal static void Run()
    {
        foreach (var boat in BoatRigCatalog.All)
        {
            CheckCopies(boat);
            foreach (var support in boat.Supports)
                CheckCopies(support);
            foreach (var group in boat.HalyardGroups)
                CheckCopies(group);
            foreach (var group in boat.Stays)
            {
                CheckCopies(group);
                foreach (var variant in group.Variants)
                    CheckCopies(variant);
            }
            foreach (var category in boat.SheetCategories)
            {
                CheckCopies(category);
                if (category.Fallback != null)
                {
                    CheckCopies(category.Fallback.Port);
                    CheckCopies(category.Fallback.Starboard);
                }
            }
        }
        CheckReadOnly(BoatRigCatalog.All);
        if (!ReferenceEquals(BoatRigCatalog.All, BoatRigCatalog.All))
            throw new Exception("Catalog is rebuilt on access.");
        foreach (string suffix in new[] { "", "(Clone)", "(Clone)(Clone)" })
            if (BoatRigCatalog.Find(boatName: "BOAT LEOPARD (207)" + suffix) != null)
                throw new Exception("Leopard must remain unsupported pending compatibility work.");

        if (
            !ReferenceEquals(
                BoatRigCatalog.Find(boatName: "BOAT medi medium (50)(Clone)(Clone)"),
                Brig.Definition
            )
            || !Brig.Definition.Sections(56).SequenceEqual(new[] { 56, 5 })
            || Brig.Definition.Base(section: 56) != 5
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

        Reject<ArgumentException>(() => Brig.Definition.Sections(section: 127));
        Reject<ArgumentException>(() => Brig.Definition.Base(section: 127));
        BoatRigDefinition Profile(
            IReadOnlyDictionary<int, int> parents,
            params SheetWinchCategory[] categories
        ) =>
            new BoatRigDefinition(
                "test",
                Brig.Definition.Supports,
                Array.Empty<FishermansStayGroupDefinition>(),
                parents,
                categories
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
        var sample = Brig.Definition.SheetCategories[0];
        Reject<ArgumentException>(() => Profile(new Dictionary<int, int>(), sample, sample));
        BoatRigDefinition HalyardProfile(params HalyardWinchGroup[] groups) =>
            new BoatRigDefinition(
                boatName: "test",
                supports: Cog.Definition.Supports,
                stays: Cog.Definition.Stays,
                mastParents: Cog.Definition.MastParents,
                halyardGroups: groups
            );
        var halyard = new HalyardWinchGroup(mast: 57, sources: new[] { 57, 58 });
        Reject<ArgumentException>(() => HalyardProfile(halyard, halyard));
        Reject<ArgumentException>(() =>
            HalyardProfile(new HalyardWinchGroup(mast: 127, sources: new[] { 127 }))
        );
        Reject<ArgumentException>(() => new HalyardWinchGroup(mast: 57, sources: null));
        Reject<ArgumentException>(() => new HalyardWinchGroup(mast: 57, sources: new[] { 58, 58 }));
        Reject<ArgumentException>(() => new HalyardWinchGroup(mast: 57, sources: new[] { 128 }));
        Console.WriteLine(
            "PASS: immutable boat profiles and halyard groups, ordered mast ancestry, missing entries, duplicate groups/categories and invalid-source/cyclic-parent rejection."
        );
    }

    // Reconstruct every authored definition using mutable arrays, then mutate all
    // inputs and try the exposed IList setters. Covers nested collections too.
    private static void CheckCopies(object original)
    {
        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var type = original.GetType();
        var constructor = type.GetConstructors(flags).Single();
        var fields = constructor
            .GetParameters()
            .Select(p =>
                type.GetFields(flags)
                    .Single(f => string.Equals(f.Name, p.Name, StringComparison.OrdinalIgnoreCase))
            )
            .ToArray();
        var arguments = fields.Select(f => f.GetValue(original)).ToArray();
        for (int i = 0; i < arguments.Length; i++)
            if (arguments[i] is IList list)
            {
                var element = fields[i].FieldType.GetGenericArguments().Single();
                var copy = Array.CreateInstance(element, list.Count);
                list.CopyTo(copy, 0);
                arguments[i] = copy;
            }
        var rebuilt = constructor.Invoke(arguments);
        for (int i = 0; i < arguments.Length; i++)
            if (arguments[i] is Array input)
            {
                var exposed = (IList)fields[i].GetValue(rebuilt);
                var expected = exposed.Cast<object>().ToArray();
                for (int j = 0; j < input.Length; j++)
                    input.SetValue(null, j);
                if (!exposed.Cast<object>().SequenceEqual(expected))
                    throw new Exception(
                        "Mutable definition input: " + type.Name + "." + fields[i].Name
                    );
                CheckReadOnly(exposed);
            }
    }

    private static void CheckReadOnly(object collection)
    {
        if (collection is Array || collection is not IList list || !list.IsReadOnly)
            throw new Exception("Definition collection exposes mutable storage.");
        if (list.Count > 0)
            Reject<NotSupportedException>(() => list[0] = list[0]);
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
