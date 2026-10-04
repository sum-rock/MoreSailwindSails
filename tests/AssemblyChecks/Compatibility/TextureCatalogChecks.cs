using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.Compatibility;

internal static class TextureCatalogChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags all = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var patch = assembly.GetType(
            "MoreSailwindSails.Compatibility.Patches.ShipyardExpansionTextureCatalogPatch"
        );
        var target = patch.GetCustomAttribute<HarmonyPatch>().info;
        var prefix = patch.GetMethod("Prefix", all);
        if (target.methodName != "Setup" || !prefix.IsDefined(typeof(HarmonyPrefix)))
            throw new Exception(
                "Register the plain texture before SE initializes prefab selections."
            );
        var changer = target.declaringType;
        var textureMap = changer.GetField("textures");
        if (
            textureMap?.FieldType.GetGenericTypeDefinition() != typeof(Dictionary<,>)
            || textureMap.FieldType.GetGenericArguments()[0] != typeof(string)
            || textureMap.FieldType.GetGenericArguments()[1].FullName != "UnityEngine.Texture"
            || changer.GetField("textureIndex")?.FieldType != typeof(string)
            || changer.GetField("allowedTextures")?.FieldType != typeof(List<string>)
            || changer.GetMethod("SetTexture", new[] { typeof(string) }) == null
        )
            throw new Exception("Installed SE name-based texture API changed.");
        var setup = Instructions(changer.GetMethod("Setup")).ToArray();
        var update = Instructions(changer.GetMethod("UpdateMaterial")).ToArray();
        if (
            !setup.Any(i => Equals(i.Operand, textureMap))
            || !update.Any(i => Equals(i.Operand, textureMap))
            || !CalledMethods(changer.GetMethod("UpdateMaterial"))
                .Any(m => m.Name == "TryGetValue" && m.DeclaringType == textureMap.FieldType)
        )
            throw new Exception("SE discovery and material updates must use the named catalog.");
        var names = (string[])changer.GetField("names").GetValue(null);
        if (names[0] != "ParticleCloudWhite")
            throw new Exception("Installed SE's default plain texture name changed.");

        var compatibility = assembly.GetType(
            "MoreSailwindSails.Compatibility.ShipyardExpansionTextureCatalog"
        );
        if (
            !CalledMethods(prefix)
                .Any(m => m.DeclaringType == compatibility && m.Name == "SeedPlain")
        )
            throw new Exception("Missing early texture catalog seeding.");
        var calls = CalledMethods(compatibility.GetMethod("SeedPlain", all)).ToArray();
        if (!calls.Any(m => m.Name == "get_sharedMaterial"))
            throw new Exception(
                "Resolve the native plain texture through a non-instantiating material read."
            );
        if (
            calls.Any(m =>
                m.Name.StartsWith("set_") && m.Name != "set_Item"
                || m.Name is "get_material" or "SetTexture" or "Clear" or "Insert"
            )
        )
            throw new Exception(
                "Catalog registration must not rewrite materials or remove named entries."
            );
        foreach (
            string family in new[]
            {
                "FishermansFlyingSail.FishermansFlyingSail",
                "FishermansStaysail.FishermansStaysail",
                "Spritsail.LooseFootedSpritsail.LooseFootedSpritsail",
                "Spritsail.BoomedSpritsail.BoomedSpritsail",
            }
        )
        {
            var appearance = assembly.GetType($"MoreSailwindSails.Sails.{family}Appearance");
            if (
                !CalledMethods(appearance.GetMethod("Configure", all))
                    .Any(m => m.DeclaringType == compatibility && m.Name == "get_HasPlainTexture")
            )
                throw new Exception(
                    "Custom appearance must verify the named plain texture is available."
                );
        }
        Console.WriteLine(
            "PASS: installed SE name-based texture contract, early catalog patch and guarded custom appearance; Unity initialization requires runtime validation."
        );
    }
}
