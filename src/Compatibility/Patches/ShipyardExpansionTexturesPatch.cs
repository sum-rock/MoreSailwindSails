// Restricts SE material updates on custom cloth sails to its blank white texture.
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MoreSailwindSails.Sails.FishermansFlyingSail;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Sails.LooseFootedSpritsail;
using UnityEngine;

namespace MoreSailwindSails.Compatibility.Patches
{
    [HarmonyPatch]
    internal static class ShipyardExpansionTexturesPatch
    {
        private static MethodBase target;
        private static bool warned;

        [HarmonyPrepare]
        private static bool Prepare()
        {
            target = null;
            try
            {
                // Inspect only loaded assemblies; SE remains an optional dependency.
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType(
                        name: "ShipyardExpansion.Scripts.SailTextureChanger"
                    );
                    if (type == null)
                        continue;
                    target = type.GetMethod(
                        name: "UpdateMaterial",
                        bindingAttr: BindingFlags.Instance
                            | BindingFlags.Public
                            | BindingFlags.NonPublic,
                        binder: null,
                        types: Type.EmptyTypes,
                        modifiers: null
                    );
                    if (target == null)
                        throw new MissingMethodException(
                            className: type.FullName,
                            methodName: "UpdateMaterial"
                        );
                    return true;
                }
            }
            catch (Exception exception)
            {
                Cleanup(exception: exception);
            }
            return false;
        }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() => target;

        [HarmonyCleanup]
        private static Exception Cleanup(Exception exception)
        {
            if (exception != null && !warned)
            {
                warned = true;
                Plugin.Log?.LogWarning(
                    $"SE plain sail texture restriction skipped: {exception.GetBaseException().Message}"
                );
            }
            return null;
        }

        [HarmonyPrefix]
        private static void Prefix(Component __instance, ref string ___textureIndex)
        {
            try
            {
                if (
                    !__instance.GetComponent<FishermansFlyingSailRig>()
                    && !__instance.GetComponent<FishermansStaysailRig>()
                    && !__instance.GetComponent<LooseFootedSpritsailRig>()
                )
                    return;
                var type = __instance.GetType();
                var names = (string[])type.GetField(name: "names").GetValue(obj: null);
                string white = names[0];
                if (string.IsNullOrWhiteSpace(value: white))
                    throw new InvalidOperationException("SE's blank texture name is empty.");
                // Replace the instance list so the donor's choices remain untouched.
                // A changed field type throws here before changing either field.
                type.GetField(name: "allowedTextures")
                    .SetValue(obj: __instance, value: new List<string> { white });
                ___textureIndex = white;
            }
            catch (Exception exception)
            {
                // Keep SE's own choices and selection if its API no longer matches.
                Cleanup(exception: exception);
            }
        }
    }
}
