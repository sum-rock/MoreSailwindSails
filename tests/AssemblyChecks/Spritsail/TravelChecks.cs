using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Spritsail;

// Verifies family-wide travel enforcement around native sheet and sway processing.
internal static class TravelChecks
{
    internal static void Run(Assembly assembly)
    {
        const string family = "MoreSailwindSails.Sails.Spritsail.";
        var patch = assembly.GetType(
            name: family + "Patches.SpritsailTravelPatch",
            throwOnError: true
        );
        var target = patch.GetCustomAttribute<HarmonyPatch>().info;
        var prefix = patch.GetMethod(
            name: "Prefix",
            bindingAttr: BindingFlags.NonPublic | BindingFlags.Static
        );
        var postfix = patch.GetMethod(
            name: "Postfix",
            bindingAttr: BindingFlags.NonPublic | BindingFlags.Static
        );
        var before = IlReader.CalledMethods(method: prefix).ToArray();
        var after = IlReader.CalledMethods(method: postfix).ToArray();
        if (
            target.declaringType.Name != "JibAngleMaster"
            || target.methodName != "Update"
            || !prefix.IsDefined(typeof(HarmonyPrefix))
            || !postfix.IsDefined(typeof(HarmonyPostfix))
            || !before.Any(m =>
                m.DeclaringType.FullName == family + "SpritsailCategory" && m.Name == "IsSpritsail"
            )
            || !before.Any(m =>
                m.DeclaringType.FullName == family + "SpritsailTravel" && m.Name == "Clamp"
            )
            || !after.Any(m =>
                m.DeclaringType.FullName == family + "SpritsailTravel" && m.Name == "ConstrainHinge"
            )
            || !after.Any(m => m.Name == "set_limits")
        )
            throw new Exception(
                "Spritsail travel must be category-scoped and bound the final native hinge limits."
            );
        if (
            before
                .Concat(after)
                .Any(m =>
                    m.DeclaringType.Name.Contains("Cloth") || m.DeclaringType.Name == "Transform"
                )
        )
            throw new Exception("Travel enforcement must not reset Cloth or snap transforms.");
        Console.WriteLine(
            "PASS: registered spritsail family travel wraps native sheet/sway processing without Cloth or transform mutation."
        );
    }
}
