using System;
using System.Linq;
using System.Reflection;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Spritsail.LooseFootedSpritsail;

// Verifies distinct mark identities feed a single registration and runtime implementation.
internal static class MarkChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags all =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.Instance;
        const string family = "MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.";
        var builder = assembly.GetType(family + "LooseFootedSpritsail", true);
        var register = builder.GetMethod("Register", all);
        var calls = IlReader.CalledMethods(method: register).ToArray();
        Require(
            calls.Count(m => m.Name == "RegisterMark" && m.DeclaringType == builder) == 2,
            "Both marks must use one per-mark builder."
        );
        foreach (
            var mark in new[]
            {
                (Name: "MkA", Id: 404, Label: "Loose-footed Spritsail Mk.A"),
                (Name: "MkB", Id: 405, Label: "Loose-footed Spritsail Mk.B"),
            }
        )
        {
            var type = assembly.GetType(
                family + mark.Name + ".LooseFootedSpritsail" + mark.Name,
                true
            );
            Require(
                (int)type.GetField("PrefabIndex", all).GetRawConstantValue() == mark.Id,
                "Mark prefab IDs must remain distinct and stable."
            );
            Require(
                (string)builder.GetMethod("DisplayName", all).Invoke(null, new object[] { mark.Id })
                    == mark.Label,
                "HUD must resolve the correct mark name."
            );
            Require(
                IlReader
                    .Instructions(method: register)
                    .Any(i =>
                        i.Operand is FieldInfo f
                        && f.DeclaringType == type
                        && f.Name == "Definition"
                    ),
                "Each definition must reach registration."
            );
            Require(
                type.GetMethods(all | BindingFlags.DeclaredOnly).Length == 0,
                "Marks must carry definitions rather than duplicate rig behavior."
            );
        }
        var rig = assembly.GetType(family + "LooseFootedSpritsailRig", true);
        var refresh = IlReader
            .Instructions(method: rig.GetMethod("RefreshCollisionStrips", all))
            .ToArray();
        Require(
            refresh.Any(i => i.Operand is FieldInfo f && f.Name == "PanelCollisionStrips"),
            "Collision refresh must use explicit panel references."
        );
        Require(
            !IlReader
                .CalledMethods(method: rig.GetMethod("RefreshCollisionStrips", all))
                .Any(m => m.Name == "GetComponentsInChildren"),
            "Collider hierarchy order must not select panel strips."
        );
        Console.WriteLine(
            "PASS: Mk.A/Mk.B IDs, distinct HUD names, shared per-mark builder and explicit collision references."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }
}
