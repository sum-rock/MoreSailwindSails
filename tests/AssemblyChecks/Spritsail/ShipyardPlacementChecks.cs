using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Spritsail;

// Verifies category-wide rotation suppression and bottom placement against installed SE/native code.
internal static class ShipyardPlacementChecks
{
    private const BindingFlags All =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private const string Family = "MoreSailwindSails.Sails.Spritsail.";

    internal static void Run(Assembly assembly)
    {
        MethodInfo Method(string type, string method) =>
            assembly
                .GetType(name: Family + type, throwOnError: true)
                .GetMethod(name: method, bindingAttr: All);
        var configure = IlReader
            .Instructions(method: Method(type: "Patches.SpritsailScalerPatch", method: "Postfix"))
            .ToArray();
        int target = Array.FindIndex(
            configure,
            i => i.Code == OpCodes.Stfld && i.Operand is FieldInfo f && f.Name == "rotatablePart"
        );
        Require(
            value: target > 0 && configure[target - 1].Code == OpCodes.Ldnull,
            message: "All registered spritsails must have no SE rotation target."
        );

        var se = Assembly.Load(assemblyString: "ShipyardExpansion");
        var scaler = se.GetType(name: "ShipyardExpansion.SailScaler", throwOnError: true);
        var angle = IlReader
            .Instructions(method: scaler.GetMethod(name: "SetAngle", bindingAttr: All))
            .ToArray();
        Require(
            value: angle.Length > 5
                && angle[1].Operand is FieldInfo f
                && f.Name == "rotatablePart"
                && angle[2].Code == OpCodes.Ldnull
                && angle[4].Code == OpCodes.Brfalse_S
                && angle[5].Code == OpCodes.Ret,
            message: "SE must reject rotation before mutating transforms when the target is null."
        );
        var ui = IlReader
            .Instructions(
                method: se.GetType(
                        name: "ShipyardExpansion.Patches.ShipyardUIPatches",
                        throwOnError: true
                    )
                    .GetMethod(name: "UpdateMoveButtonsPatch", bindingAttr: All)
            )
            .ToArray();
        Require(
            value: ui.Any(i => i.Operand is FieldInfo field && field.Name == "rotatablePart"),
            message: "SE rotation UI must depend on the rotation target."
        );
        foreach (string name in new[] { "rotateForwardButton", "rotateBackwardButton" })
        {
            int index = Array.FindIndex(
                ui,
                i => i.Code == OpCodes.Ldsfld && i.Operand is FieldInfo field && field.Name == name
            );
            Require(
                value: index >= 0
                    && ui[index + 1].Code == OpCodes.Ldloc_2
                    && ui[index + 2].Operand is MethodInfo called
                    && called.Name == "SetActive",
                message: "Both SE rotation buttons must use the native rotatable-target visibility flag."
            );
        }

        var placement = Method(type: "Patches.SpritsailInitialPlacementPatch", method: "Postfix");
        Require(
            value: placement.GetCustomAttribute<HarmonyPriority>().info.priority == Priority.Last
                && placement
                    .GetCustomAttribute<HarmonyAfter>()
                    .info.after.Contains("com.nandbrew.shipyardexpansion"),
            message: "Bottom placement must run after SE and make-specific sizing."
        );
        var calls = IlReader.CalledMethods(method: placement).ToArray();
        foreach (
            string name in new[]
            {
                "IsSpritsail",
                "GetScaledHeight",
                "GetCurrentInstallHeight",
                "ChangeInstallHeight",
                "MoveHeldSail",
            }
        )
            Require(
                value: calls.Any(m => m.Name == name),
                message: "Category-scoped bottom placement/refresh is missing: " + name
            );
        Require(
            value: !IlReader
                .Instructions(method: placement)
                .Any(i => i.Operand is FieldInfo field && field.Name == "mastHeight"),
            message: "Initial placement must use the sail's scaled height, not the top of the mast."
        );
        Require(
            value: !IlReader
                .CalledMethods(
                    method: Method(
                        type: "LooseFootedSpritsail.Patches.LooseFootedSpritsailNewSailPatch",
                        method: "Postfix"
                    )
                )
                .Any(m => m.Name == "ChangeInstallHeight"),
            message: "Mk.A must not override the category placement policy."
        );
        Console.WriteLine(
            "PASS (structural): category-wide no-rotation target, installed SE button/SetAngle guards and final bottom-placement/native refresh path; live fitting requires Unity validation."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message: message);
    }
}
