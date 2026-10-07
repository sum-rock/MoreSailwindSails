using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Spritsail.LooseFootedSpritsail.MkA;

// Checks integration boundaries and safety invariants against installed native signatures.
internal static class PrototypeChecks
{
    private const string Family = "MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail.";
    private const BindingFlags All =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    internal static void Run(Assembly assembly)
    {
        Type Type(string name) => assembly.GetType(name: Family + name, throwOnError: true);
        MethodInfo Method(string type, string method) =>
            Type(name: type).GetMethod(name: method, bindingAttr: All);
        var registration = Type(name: "MkA.LooseFootedSpritsailMkA");
        Require(
            value: (int)
                registration.GetField(name: "PrefabIndex", bindingAttr: All).GetRawConstantValue()
                == 404,
            message: "Spritsail must retain its own stable save ID."
        );
        var register = Method(
            type: "Patches.LooseFootedSpritsailRegistrationPatch",
            method: "Postfix"
        );
        Require(
            value: register
                .GetCustomAttribute<HarmonyAfter>()
                .info.after.Contains("com.nandbrew.shipyardexpansion")
                && register
                    .GetCustomAttribute<HarmonyBefore>()
                    .info.before.Contains("NatoriusG.AllSailsAllShipyards"),
            message: "Register after SE construction and before All Sails caches the catalog."
        );
        Require(
            value: assembly.GetType(name: Family + "Patches.LooseFootedSpritsailControlsPatch")
                == null,
            message: "Loose-footed spritsails must remain in the native control list."
        );
        var resolver = IlReader
            .CalledMethods(
                method: Method(type: "LooseFootedSpritsailRigging", method: "TryResolve")
            )
            .ToArray();
        Require(
            value: !resolver.Any(m => m.DeclaringType.Namespace == "MoreSailwindSails.BoatRigs")
                && resolver.Any(m => m.Name == "Attachment"),
            message: "Spritsail supports use the carrying mast's native attachments without a profile."
        );
        foreach (
            var type in assembly
                .GetTypes()
                .Where(t =>
                    t.Namespace?.StartsWith(Family.TrimEnd('.'), StringComparison.Ordinal) == true
                )
        )
        foreach (var method in type.GetMethods(bindingAttr: All | BindingFlags.DeclaredOnly))
        foreach (var called in IlReader.CalledMethods(method: method))
            Require(
                value: called.DeclaringType?.Namespace?.StartsWith(
                    "MoreSailwindSails.Sails.Fishermans",
                    StringComparison.Ordinal
                ) != true,
                message: "Spritsail mechanics must remain independent of the existing sail families."
            );
        var shapeCalls = IlReader
            .CalledMethods(
                method: Method(type: "LooseFootedSpritsailRig", method: "UpdateShapeBones")
            )
            .ToArray();
        Require(
            value: !shapeCalls.Any(m =>
                m.Name
                    is "set_sharedMesh"
                        or "set_bindposes"
                        or "ClearTransformMotion"
                        or "set_enabled"
            ),
            message: "Tacking may move bones, but must not reset or replace live Cloth."
        );
        var late = IlReader
            .CalledMethods(method: Method(type: "LooseFootedSpritsailRig", method: "LateUpdate"))
            .ToArray();
        Require(
            value: !late.Any(m => m.Name is "set_sharedMesh" or "set_bindposes" or "AddComponent"),
            message: "Live deployment must retain initialized topology."
        );
        var wind = Type(name: "Patches.LooseFootedSpritsailWindFramePatch");
        var finalizer = wind.GetMethod(name: "Finalizer", bindingAttr: All);
        Require(
            value: finalizer.IsDefined(attributeType: typeof(HarmonyFinalizer))
                && IlReader
                    .Instructions(method: finalizer)
                    .Any(i =>
                        i.Code == OpCodes.Stfld
                        && i.Operand is FieldInfo f
                        && f.Name == "currentUnroll"
                    ),
            message: "Restore the original winch/save value after scoped force calculation, including exceptions."
        );
        var force = Method(type: "Patches.LooseFootedSpritsailWindFramePatch", method: "Prefix");
        Require(
            value: IlReader
                .CalledMethods(method: force)
                .Any(m =>
                    m.Name == "GetComponent"
                    && m.IsGenericMethod
                    && m.GetGenericArguments()[0] == Type(name: "LooseFootedSpritsailRig")
                ),
            message: "Exposed-area force changes must be scoped to the new rig."
        );
        var order = Method(type: "Patches.LooseFootedSpritsailOrderTextPatch", method: "Prefix");
        Require(
            value: order
                .GetCustomAttribute<HarmonyBefore>()
                .info.before.Contains("com.nandbrew.nandfixes")
                && IlReader.Instructions(method: order).Any(i => i.Code == OpCodes.Stind_Ref),
            message: "Order guard must precede NANDFixes and consume recursive input."
        );
        var spar = assembly
            .GetType(name: "MoreSailwindSails.Sails.Spritsail.SpritsailSpar", throwOnError: true)
            .GetMethod(name: "Create", bindingAttr: BindingFlags.NonPublic | BindingFlags.Static);
        Require(
            value: new MethodBase[] { spar }
                .Concat(
                    IlReader
                        .CalledMethods(method: spar)
                        .Where(m =>
                            m.DeclaringType?.Namespace == "MoreSailwindSails.Sails.Spritsail"
                        )
                )
                .SelectMany(IlReader.Instructions)
                .Any(i => i.Operand is string s && s == "boom_gaff_top")
                && !IlReader
                    .CalledMethods(method: spar)
                    .Any(m => m.Name is "set_color" or "SetTexture"),
            message: "Reuse the inspected gaff timber without modifying its shared material."
        );
        Console.WriteLine(
            "PASS (structural): spritsail registration, single-mast supports, native control binding, independent mechanics, fixed live topology, force-state restoration, order guards and gaff timber ownership."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message: message);
    }
}
