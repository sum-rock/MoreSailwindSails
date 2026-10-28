// Checks shipyard bindings against installed native obstruction and overlap contracts.
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.LooseFootedSpritsail;

internal static class CollisionIntegrationChecks
{
    private const BindingFlags All =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    internal static void Run(Assembly assembly)
    {
        Type Find(string name) =>
            assembly.GetType(
                name: "MoreSailwindSails.Sails.LooseFootedSpritsail." + name,
                throwOnError: true
            );
        var overlap = Find(name: "Patches.SpritsailOverlapPatch");
        var contact = Find(name: "Patches.SpritsailSquareCollisionPatch");
        var pair = overlap.GetMethod(name: "SharesSquareMast", bindingAttr: All);
        var overlapPatch = overlap.GetMethod(name: "Postfix", bindingAttr: All);
        var contactPatch = contact.GetMethod(name: "Prefix", bindingAttr: All);
        foreach (var patch in new[] { overlapPatch, contactPatch })
            Require(
                CalledMethods(method: patch).Contains(pair),
                "Height and trigger exceptions must use the same shared-mast rule."
            );

        var pairCalls = CalledMethods(method: pair).OfType<MethodInfo>().ToArray();
        Require(
            pairCalls.Count(m =>
                m.IsGenericMethod
                && m.GetGenericArguments().Single() == Find(name: "LooseFootedSpritsailRig")
            ) == 2,
            "Both installation orders need the spritsail identity check."
        );
        Require(
            pairCalls.Any(m => m.IsGenericMethod && m.GetGenericArguments().Single().Name == "Mast")
                && pairCalls.Count(m => m.Name == "get_parent") >= 3
                && pairCalls.Any(m => m.Name == "op_Inequality"),
            "The exemption must require the same physical Mast parent."
        );
        var contactInstructions = Instructions(method: contactPatch).ToArray();
        foreach (string tag in new[] { "SailColChecker", "SailColCheckerSub" })
            Require(
                contactInstructions.Any(i => i.Operand as string == tag),
                "Missing root/child sail-contact classification: " + tag
            );
        Require(
            CalledMethods(method: contactPatch).Any(m => m.Name == "GetComponentInParent"),
            "Child colliders must resolve their owning checker."
        );
        Require(
            !contactInstructions.Any(i => i.Code == OpCodes.Stfld),
            "The pair exemption must not reset obstruction counters or travel limits."
        );

        var target = overlap.GetCustomAttribute<HarmonyPatch>().info;
        var nativeOverlap = target.declaringType.GetMethod(
            name: target.methodName,
            bindingAttr: All
        );
        Require(
            CalledMethods(method: nativeOverlap).Any(m => m.Name == "IsOverlapping"),
            "Installed native overlap algorithm changed; review the spritsail exception."
        );
        var nativeInterval = target.declaringType.GetMethod(
            name: "IsOverlapping",
            bindingAttr: All
        );
        foreach (string method in new[] { "Max", "Min" })
            Require(
                CalledMethods(method: nativeInterval).Any(m => m.Name == method)
                    && CalledMethods(method: overlapPatch).Any(m => m.Name == method),
                "Remaining sail pairs must retain native inclusive height-overlap checking."
            );
        foreach (string method in new[] { "GetCurrentInstallHeight", "GetScaledHeight" })
            Require(
                CalledMethods(method: overlapPatch).Count(m => m.Name == method) == 2,
                "Remaining overlap checks must account for both sails' fitted extents."
            );

        var contactTarget = contact.GetCustomAttribute<HarmonyPatch>().info;
        var nativeContact = contactTarget.declaringType.GetMethod(
            name: contactTarget.methodName,
            bindingAttr: All
        );
        foreach (
            string field in new[]
            {
                "currentAngle",
                "colAngleMin",
                "colAngleMax",
                "collisionsWithOther",
            }
        )
            Require(
                Instructions(method: nativeContact)
                    .Any(i => i.Operand is FieldInfo f && f.Name == field),
                "Installed native rotation/obstruction handling changed: " + field
            );
        var sweep = Find(name: "Patches.SpritsailCollisionPatch")
            .GetMethod(name: "Prefix", bindingAttr: All);
        Require(
            CalledMethods(method: sweep).Any(m => m.Name == "PositionCollisionChecker")
                && Instructions(method: sweep)
                    .Any(i => i.Operand is FieldInfo f && f.Name == "startMaxAngle"),
            "The custom sweep must preserve its completion-to-neutral handling."
        );
        var refresh = Find(name: "LooseFootedSpritsailRig")
            .GetMethod(name: "RefreshCollision", bindingAttr: All);
        Require(
            Instructions(method: refresh)
                .Any(i =>
                    i.Operand is FieldInfo f
                    && f.DeclaringType.Name == "LooseFootedSpritsailClearance"
                    && f.Name == "Rotation"
                ),
            "The sprit collider must apply its fitted orientation."
        );
        Console.WriteLine(
            "PASS (structural): spritsail same-mast square exceptions, child contacts, fitted overlap and native obstruction/travel contracts."
        );
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
