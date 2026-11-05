// Checks native integration contracts without constructing Unity objects or changing game state.
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.LooseFootedSpritsail;

internal static class IntegrationChecks
{
    private const BindingFlags All =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private const string Family = "MoreSailwindSails.Sails.LooseFootedSpritsail.";

    internal static void Run(Assembly assembly)
    {
        Type Type(string name) => assembly.GetType(name: Family + name, throwOnError: true);
        var prefab = Type("LooseFootedSpritsailPrefab");
        var rig = Type("LooseFootedSpritsailRig");
        var assets = Type("LooseFootedSpritsailAssets");
        var cut = Type("MkA.LooseFootedSpritsailMkA");
        if (
            (int)cut.GetField(name: "PrefabIndex", bindingAttr: All).GetRawConstantValue() != 404
            || (int)prefab.GetField(name: "SourceIndex", bindingAttr: All).GetRawConstantValue()
                != 110
            || (int)prefab.GetField(name: "FullGaffIndex", bindingAttr: All).GetRawConstantValue()
                != 15
        )
            throw new Exception("Spritsail save identity or native donor identity changed.");
        if (
            !CalledMethods(method: cut.GetMethod(name: "Register", bindingAttr: All))
                .Any(m => m.DeclaringType == prefab && m.Name == "Register")
        )
            throw new Exception("Cuts must use the shared prefab builder.");
        var register = prefab.GetMethod(name: "Register", bindingAttr: All);
        var instructions = Instructions(method: register).ToArray();
        int inactive = Array.FindIndex(
            array: instructions,
            match: i => i.Operand is MethodBase m && m.Name == "SetActive"
        );
        int clone = Array.FindIndex(
            array: instructions,
            match: i => i.Operand is MethodBase m && m.Name == "Instantiate"
        );
        if (
            inactive < 0
            || clone <= inactive
            || instructions[inactive - 1].Code != OpCodes.Ldc_I4_0
        )
            throw new Exception("Spritsail donor must be cloned beneath an inactive container.");
        if (
            !CalledMethods(method: register)
                .Any(m => m.DeclaringType == assets && m.Name == "Release")
            || register.GetMethodBody().ExceptionHandlingClauses.Count == 0
        )
            throw new Exception("Failed prefab construction must release its allocated assets.");
        foreach (
            string patch in new[]
            {
                "SpritsailMastCompatiblePatch",
                "SpritsailHeightPatch",
                "SpritsailScalePatch",
                "SpritsailAreaPatch",
                "SpritsailClothRefreshPatch",
                "SpritsailUpdatePatch",
                "SpritsailPhysicsPatch",
                "SpritsailForceDirectionPatch",
                "SpritsailWindFramePatch",
                "SpritsailHingeResetPatch",
                "SpritsailMovementPatch",
            }
        )
        {
            var prefix = Type("Patches." + patch).GetMethod(name: "Prefix", bindingAttr: All);
            if (
                !CalledMethods(method: prefix)
                    .OfType<MethodInfo>()
                    .Any(m =>
                        m.Name == "GetComponent"
                        && m.IsGenericMethod
                        && m.GetGenericArguments().Single() == rig
                    )
            )
                throw new Exception(patch + " affects unrelated sails.");
        }
        var compatible = Type("Patches.SpritsailMastCompatiblePatch")
            .GetMethod(name: "Prefix", bindingAttr: All);
        if (
            Instructions(method: compatible)
                .Any(i =>
                    i.Operand is FieldInfo f
                    && (f.Name == "onlySquareSails" || f.Name == "onlyStaysails")
                )
        )
            throw new Exception("Spritsails must not depend on category-only mast restrictions.");
        foreach (
            var type in assembly
                .GetTypes()
                .Where(t => t.Namespace?.StartsWith(value: Family.TrimEnd('.')) == true)
        )
        foreach (var method in type.GetMethods(bindingAttr: All | BindingFlags.DeclaredOnly))
        foreach (var call in CalledMethods(method: method))
            if (call.DeclaringType?.Namespace?.Contains(value: "BoatConfiguration") == true)
                throw new Exception("Spritsail fitting must not whitelist boats.");

        var strip = prefab.GetMethod(name: "StripDonor", bindingAttr: All);
        var cleared = Instructions(method: strip)
            .Where(i => i.Code == OpCodes.Stfld)
            .Select(i => i.Operand)
            .OfType<FieldInfo>()
            .Select(f => f.Name)
            .ToArray();
        foreach (
            string field in new[]
            {
                "angleControllerMid",
                "midRopeAttachment",
                "mastReefAttachment",
                "mastReefAttExtension",
            }
        )
            if (!cleared.Contains(value: field))
                throw new Exception("Donor binding retained: " + field);
        foreach (
            string field in new[]
            {
                "reefController",
                "angleControllerLeft",
                "angleControllerRight",
            }
        )
            if (cleared.Contains(value: field))
                throw new Exception("Native controller reference cleared: " + field);
        if (
            Instructions(method: strip)
                .Any(i =>
                    i.Code == OpCodes.Stfld
                    && i.Operand is FieldInfo f
                    && f.DeclaringType.Name == "Mast"
                )
        )
            throw new Exception("Stripping a sail must not change native mast fields.");
        var ownerRelease = CalledMethods(
                method: assets.GetMethod(name: "Release", bindingAttr: All)
            )
            .ToArray();
        if (
            !ownerRelease.Any(m => m.Name == "Destroy") || !ownerRelease.Any(m => m.Name == "Clear")
        )
            throw new Exception("Owned meshes/materials require idempotent cleanup.");
        var destroy = rig.GetMethod(name: "OnDestroy", bindingAttr: All);
        if (!CalledMethods(method: destroy).Any(m => m.Name == "IsChildOf"))
            throw new Exception("Externally parented collision checkers must be cleaned up.");
        var sync = rig.GetMethod(name: "SyncColor", bindingAttr: All);
        if (!CalledMethods(method: sync).Any(m => m.Name == "get_whiteTexture"))
            throw new Exception("Spritsail recoloring must retain plain cloth.");
        var se = assembly.GetType(
            name: "MoreSailwindSails.Compatibility.Patches.ShipyardExpansionTexturesPatch",
            throwOnError: true
        );
        if (
            !CalledMethods(method: se.GetMethod(name: "Prefix", bindingAttr: All))
                .OfType<MethodInfo>()
                .Any(m =>
                    m.Name == "GetComponent"
                    && m.IsGenericMethod
                    && m.GetGenericArguments().Single() == rig
                )
        )
            throw new Exception("SE plain texture handling must include spritsails.");
        var registration = Type("Patches.SpritsailRegistrationPatch")
            .GetMethod(name: "Postfix", bindingAttr: All);
        if (
            !registration
                .GetCustomAttributes<HarmonyAfter>()
                .Any(a => a.info.after.Contains(value: "com.nandbrew.shipyardexpansion"))
            || !registration
                .GetCustomAttributes<HarmonyBefore>()
                .Any(a => a.info.before.Contains(value: "NatoriusG.AllSailsAllShipyards"))
        )
            throw new Exception("Spritsail registration ordering changed.");
        // Native save/load still supplies both size axes to the family-scoped scale adapter.
        var nativeSail = register
            .GetParameters()[0]
            .ParameterType.Assembly.GetType(name: "Mast", throwOnError: true);
        if (
            !CalledMethods(method: nativeSail.GetMethod(name: "LoadSail", bindingAttr: All))
                .Any(m => m.Name == "LoadScale")
        )
            throw new Exception("Native saved sail sizing no longer uses LoadScale.");
        Console.WriteLine(
            "PASS (structural): spritsail registration, universal mast compatibility, scoped sailing patches, native controller retention, asset cleanup, plain textures and native save sizing."
        );
        SailingIntegrationChecks.Run(assembly: assembly);
    }
}
