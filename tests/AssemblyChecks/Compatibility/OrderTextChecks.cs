// Exercises the single shared order-text prefix and verifies its Harmony integration.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace MoreSailwindSails.Tests.AssemblyChecks.Compatibility;

internal static class OrderTextChecks
{
    internal static void Run(Assembly assembly)
    {
        var patches = assembly
            .GetTypes()
            .Where(t =>
                t.GetCustomAttributes<HarmonyPatch>()
                    .Any(a =>
                        a.info.declaringType?.FullName == "ShipyardUIOrderText"
                        && a.info.methodName == "AddLine"
                    )
            )
            .ToArray();
        if (patches.Length != 1)
            throw new Exception("Fisherman order text must have exactly one shared patch.");
        var prefix = patches[0].GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
        if (
            !prefix.IsDefined(typeof(HarmonyPrefix))
            || !prefix
                .GetCustomAttribute<HarmonyBefore>()
                .info.before.Contains("com.nandbrew.nandfixes")
        )
            throw new Exception("The shared prefix must run before NANDFixes.");
        foreach (
            string name in new[]
            {
                "Fisherman's Flying Sail",
                "Fisherman's Staysail Mk.A",
                "Fisherman's Staysail Mk.B",
                "Fisherman's Staysail Mk.C",
                "Fisherman's Stay (foremast 1 / main topmast 2)",
            }
        )
        {
            string line = $"192: (ERROR): {name} (150%) -> (no sail)";
            var lines = new List<string> { "existing" };
            object[] args = { line, lines };
            if (
                (bool)prefix.Invoke(obj: null, parameters: args)
                || (string)args[0] != ""
                || lines.Count < 3
                || lines[0] != "existing"
                || lines.Any(s => s.Length > 45)
                || string.Concat(lines.Skip(1)).Replace(" ", "") != line.Replace(" ", "")
            )
                throw new Exception(
                    "The prefix did not consume and append the order exactly once."
                );
            int count = lines.Count;
            if (!(bool)prefix.Invoke(obj: null, parameters: args) || lines.Count != count)
                throw new Exception("Consumed input produced duplicate order lines.");
        }
        foreach (string line in new[] { null, "", "192: shipyard fee", new string('x', 100) })
        {
            var lines = new List<string> { "existing" };
            object[] args = { line, lines };
            if (
                !(bool)prefix.Invoke(obj: null, parameters: args)
                || (string)args[0] != line
                || !lines.SequenceEqual(new[] { "existing" })
            )
                throw new Exception("The prefix changed unrelated or short input.");
        }
        Console.WriteLine(
            "PASS: one shared order prefix, NANDFixes ordering, all Fisherman families, consumed input and native-list preservation."
        );
    }
}
