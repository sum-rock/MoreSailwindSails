using System;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Sails.FishermansFlyingSail;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansFlyingSail;

internal static class RigChecks
{
    internal static void Run()
    {
        string[] names =
        {
            "BOAT medi medium (50)",
            "BOAT junk medium (80)",
            "BOAT junk large (70)",
            "BOAT dhow medium (20)",
            "BOAT medi small (40)",
            "BOAT LEOPARD (207)",
            "BOAT Shroud Large",
            "BOAT dhow large (30)",
        };
        // Live mast-section and native winch-source mapping, in selection order.
        string[] layouts =
        {
            "15:3>5;16:3>4;18:2>5;20:2>4;22:4>7;24:4>6;61:3>56,5;62:3>58,4;63:2>56,5;64:2>58,4;65:4>59,7;66:4>60,6",
            "16:9>10;61:58>11;5:10>12;6:11>12;65:10>53,12;66:11>53,12",
            "10:1>2;12:2>3;55:51>52;58:51>3;73:1>52;71:52>4",
            "60:10>59,55;67:11>59,55;71:10>70,69;81:11>80,12;82:10>80,12;58:51>14,11;68:62>14,11;79:51>14,11;54:10>55;61:10>69;66:11>55",
            "51:8>5;58:5>57;65:8>6",
            "18:7>12;17:8>12;19:7>11",
            "25:7>9",
            "20:0>3,2;23:1>3,2;26:0>5,4;29:1>5,4;32:3,2>6;34:5,4>6;36:3,2>7;38:5,4>7;40:3,2>8;42:5,4>8",
        };
        Assert(BoatRigCatalog.All.Length == names.Length, "Missing boat mapping coverage.");
        for (int i = 0; i < names.Length; i++)
        {
            var profile = BoatRigCatalog.Find(names[i]);
            Assert(
                profile != null && BoatRigCatalog.Find(names[i] + "(Clone)") == profile,
                "Boat lookup failed."
            );
            string actual = string.Join(
                ";",
                profile.Supports.Select(s =>
                    s.SheetControlSource
                    + ":"
                    + string.Join(",", s.ForeSections)
                    + ">"
                    + string.Join(",", s.AftSections)
                )
            );
            Assert(actual == layouts[i], "Mast or control mapping changed: " + names[i]);
            foreach (int fore in profile.Supports.SelectMany(s => s.ForeSections).Distinct())
            {
                var pairs = profile.MastPairs(fore).ToArray();
                Assert(pairs.Length > 0, "Supported foremast lost all aft supports.");
                foreach (var pair in pairs)
                    Assert(
                        pair.All(s =>
                            s.ForeSections.Contains(fore) && s.AftSections.Last() == pair.Key
                        ),
                        "Mast pair mixed unrelated support sections."
                    );
            }
        }
        Assert(
            BoatRigCatalog.Find("BOAT medi medium (50) custom") == null
                && BoatRigCatalog.Find(null) == null,
            "Unknown boats must not use guessed profiles."
        );
        var brig = BoatRigCatalog.Find(names[0]).Supports;
        Assert(
            brig.Single(s => s.SheetControlSource == 61).AftSections.SequenceEqual(new[] { 56, 5 }),
            "Brig topmast lost its lower mainmast support."
        );
        Assert(
            brig.Single(s => s.SheetControlSource == 62).AftSections.SequenceEqual(new[] { 58, 4 }),
            "Alternate Brig topmast uses wrong lower section."
        );
        Reject(() =>
            new BoatRigDefinition(
                "duplicate",
                new[] { brig[0], brig[0] },
                Brig.Definition.Stays,
                Brig.Definition.MastParents,
                Brig.Definition.SheetCategories
            )
        );
        Reject(() =>
            new BoatRigDefinition(
                "empty",
                Array.Empty<MastSupportDefinition>(),
                Brig.Definition.Stays,
                Brig.Definition.MastParents,
                Brig.Definition.SheetCategories
            )
        );
        Reject(() => new MastSupportDefinition(1, new[] { 2 }, new[] { 2 }));
        Reject(() => new MastSupportDefinition(-1, new[] { 2 }, new[] { 3 }));
        Reject(() => new MastSupportDefinition(1, Array.Empty<int>(), new[] { 3 }));
        Console.WriteLine(
            "PASS: eight boat profiles, physical mast sections, control-source selection and unknown-boat handling."
        );
    }

    private static void Assert(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
    }

    private static void Reject(Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }
        throw new Exception("Invalid rig definition was accepted.");
    }
}
