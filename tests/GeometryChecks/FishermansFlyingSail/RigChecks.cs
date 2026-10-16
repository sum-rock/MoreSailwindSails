using System;
using System.Linq;
using MoreSailwindSails.BoatConfiguration;
using MoreSailwindSails.BoatConfiguration.Boats;
using MoreSailwindSails.Sails.FishermansFlyingSail;
using MoreSailwindSails.Sails.FishermansFlyingSail.Configuration;

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
            "BOAT Shroud Large",
            "BOAT dhow large (30)",
            "BOAT GLORIANA (182)",
            "BOAT CHRONIAN (187)",
            "BOAT CAELANOR (192)",
            "BOAT junk small singleroof(90)",
            "BOAT dhow small (10)",
        };
        // Live mast-section mapping, in selection order.
        string[] layouts =
        {
            "3>5;3>4;2>5;2>4;4>7;4>6;3>56,5;3>58,4;2>56,5;2>58,4;4>59,7;4>60,6",
            "9>10;58>11;10>12;11>12;10>53,12;11>53,12",
            "1>2;2>3;51>52;51>3;1>52;52>4",
            "10>59,55;11>59,55;10>70,69;11>80,12;10>80,12;51>14,11;62>14,11;51>14,11;10>55;10>69;11>55",
            "8>5;5>57;8>6",
            "7>9",
            "0>3,2;1>3,2;0>5,4;1>5,4;3,2>6;5,4>6;3,2>7;5,4>7;3,2>8;5,4>8",
            "1>2;2>4,3",
            "15,3,2>16,5,4;16,5,4>17,7,6",
            "17,2,1>18,6,5",
            "5>7;6>7",
            "6,7>51",
        };
        Assert(FlyingSailCatalog.All.Count == names.Length, "Missing boat mapping coverage.");
        for (int i = 0; i < names.Length; i++)
        {
            var profile = FlyingSailCatalog.Find(names[i]);
            Assert(
                profile != null && FlyingSailCatalog.Find(names[i] + "(Clone)") == profile,
                "Boat lookup failed."
            );
            string actual = string.Join(
                ";",
                profile.Supports.Select(s =>
                    string.Join(",", s.ForeSections) + ">" + string.Join(",", s.AftSections)
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
            FlyingSailCatalog.Find("BOAT medi medium (50) custom") == null
                && FlyingSailCatalog.Find(null) == null,
            "Unknown boats must not use guessed profiles."
        );
        var brig = FlyingSailCatalog.Find(names[0]).Supports;
        Assert(
            brig.Single(s =>
                    s.ForeSections.SequenceEqual(new[] { 3 }) && s.AftSections.Contains(56)
                )
                .AftSections.SequenceEqual(new[] { 56, 5 }),
            "Brig topmast lost its lower mainmast support."
        );
        Assert(
            brig.Single(s =>
                    s.ForeSections.SequenceEqual(new[] { 3 }) && s.AftSections.Contains(58)
                )
                .AftSections.SequenceEqual(new[] { 58, 4 }),
            "Alternate Brig topmast uses wrong lower section."
        );
        Reject(() =>
            new MastSupportDefinition(foreSections: new[] { 2 }, aftSections: new[] { 2 })
        );
        Reject(() =>
            new MastSupportDefinition(foreSections: new[] { -1 }, aftSections: new[] { 3 })
        );
        Reject(() =>
            new MastSupportDefinition(foreSections: Array.Empty<int>(), aftSections: new[] { 3 })
        );
        Console.WriteLine(
            "PASS: twelve Fisherman boat profiles, physical mast sections, support selection and unknown-boat handling."
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
