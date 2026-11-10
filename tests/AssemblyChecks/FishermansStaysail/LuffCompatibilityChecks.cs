// Checks the luff-only rule is shared by collision filtering and independent fitting validation.
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.FishermansStaysail;

internal static class LuffCompatibilityChecks
{
    private const BindingFlags All =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    internal static void Run(Assembly assembly)
    {
        Type Find(string name) =>
            assembly.GetType(
                "MoreSailwindSails.Sails.FishermansStaysail." + name,
                throwOnError: true
            );
        var compatibility = Find("FishermansStaysailLuffCompatibility");
        var pair = compatibility.GetMethod("TryOverlap", All);
        var scan = compatibility.GetMethod("HasOverlap", All);
        var contact = Find("Patches.FishermansStaysailLuffCollisionPatch").GetMethod("Prefix", All);
        var validation = Find("Patches.FishermansStaysailLuffOverlapPatch")
            .GetMethod("Postfix", All);
        Require(
            CalledMethods(contact).Contains(pair) && CalledMethods(scan).Contains(pair),
            "Collision filtering and validation must share the same pair and luff rule."
        );
        Require(
            CalledMethods(validation).Contains(scan)
                && CalledMethods(validation).Any(m => m.Name == "IsNullOrEmpty"),
            "Independent luff validation must preserve existing installation errors."
        );
        foreach (string tag in new[] { "SailColChecker", "SailColCheckerSub" })
            Require(
                Instructions(contact).Any(i => i.Operand as string == tag),
                "Missing sail-only collider filter: " + tag
            );
        Require(
            !Instructions(contact).Any(i => i.Code == OpCodes.Stfld),
            "Contact exceptions must not clear other obstructions or travel limits."
        );
        foreach (
            string call in new[]
            {
                "SharesLuffMast",
                "ForeSailFrame",
                "LuffsOverlap",
                "TransformPoint",
                "GetCurrentInstallHeight",
            }
        )
            Require(
                CalledMethods(pair).Any(m => m.Name == call),
                "Missing physical luff frame calculation: " + call
            );
        Require(
            CalledMethods(pair).Count(m => m.Name == "GetScaledHeight") == 2,
            "Use deployed fitted luff lengths for both sails."
        );
        foreach (string field in new[] { "masts", "sails" })
            Require(
                Instructions(scan).Any(i => i.Operand is FieldInfo f && f.Name == field),
                "Check registered fittings across stay and physical mast lists: " + field
            );
        Require(
            CalledMethods(scan).Any(m => m.Name == "get_activeInHierarchy"),
            "Ignore inactive mast alternatives."
        );
        var patch = validation.DeclaringType.GetCustomAttribute<HarmonyPatch>().info;
        Require(
            patch.declaringType.Name == "ShipyardSailInstaller"
                && patch.methodName == "GetInstallError",
            "Luff validation must run independently of contact triggers and same-mast overlap exemptions."
        );
        Console.WriteLine(
            "PASS: staysail/gaff/spritsail shared luff rule, sail-only contact filtering, cross-mount validation and preserved native errors."
        );
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
