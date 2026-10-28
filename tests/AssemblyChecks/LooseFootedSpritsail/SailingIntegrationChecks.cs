// Verifies native physics contracts, control lifecycle and save-compatible sailing integration.
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.LooseFootedSpritsail;

internal static class SailingIntegrationChecks
{
    private const BindingFlags All =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    internal static void Run(Assembly assembly)
    {
        Type Find(string name) =>
            assembly.GetType(name: "MoreSailwindSails." + name, throwOnError: true);
        Type Family(string name) => Find(name: "Sails.LooseFootedSpritsail." + name);
        var rig = Family(name: "LooseFootedSpritsailRig");
        var rigging = Family(name: "LooseFootedSpritsailRigging");
        var physics = Family(name: "LooseFootedSpritsailPhysics");
        var prefab = Family(name: "LooseFootedSpritsailPrefab");
        var contract = Find(name: "Controls.ISailControlRig");
        Check(
            contract.IsAssignableFrom(c: rigging),
            "Spritsail sheets must use shared reservations."
        );
        var request = rigging.GetMethod(name: "TryGetControlRequest", bindingAttr: All);
        var requestFields = Instructions(method: request)
            .Where(i => i.Code == OpCodes.Stfld)
            .Select(i => i.Operand)
            .OfType<FieldInfo>()
            .Select(f => f.Name)
            .ToArray();
        Check(
            requestFields.Contains("Left")
                && requestFields.Contains("Right")
                && requestFields.Contains("SourceMast")
                && requestFields.Contains("FittedSheetSpan")
                && requestFields.Contains("Reef"),
            "Spritsails must request paired sheets and an independent halyard."
        );
        Check(
            Calls(type: rig, method: "PrepareSailing", called: "get_SheetsReady")
                && Calls(type: rig, method: "PrepareSailing", called: "IsInstalled")
                && Calls(type: rig, method: "PrepareSailing", called: "Clear")
                && Calls(type: rig, method: "PrepareSailing", called: "UpdateBodyMotion")
                && !Calls(type: rig, method: "PrepareSailing", called: "ResetToNeutral"),
            "Unavailable sheets and previews must clear force without parking a body joined to the boat."
        );
        Check(
            Instructions(method: rig.GetMethod(name: "PrepareSailing", bindingAttr: All))
                .Any(i => i.Operand is FieldInfo f && f.Name == "currentShipyard"),
            "Shipyard previews must not apply sail force."
        );
        var gate = Family(name: "Patches.SpritsailPhysicsPatch")
            .GetMethod(name: "Prefix", bindingAttr: All);
        var gateCalls = CalledMethods(method: gate).ToArray();
        Check(
            Array.FindIndex(array: gateCalls, match: m => m.Name == "PrepareSailing")
                < Array.FindIndex(
                    array: gateCalls,
                    match: m => m.DeclaringType == physics && m.Name == "Refresh"
                ),
            "Every admitted physics step must refresh force after checking readiness."
        );
        Check(
            Calls(type: rig, method: "Refresh", called: "set_anchor")
                && Calls(type: rig, method: "Refresh", called: "set_axis")
                && Calls(type: rig, method: "Refresh", called: "set_connectedAnchor")
                && Calls(type: rig, method: "Refresh", called: "InverseTransformPoint"),
            "Hinge pivots must use the measured mast and the boat's connected-body coordinates."
        );
        Check(
            Instructions(method: rig.GetMethod(name: "UpdatePlate", bindingAttr: All))
                .Any(i => i.Operand is FieldInfo f && f.Name == "neutralWorld")
                && !Calls(type: rig, method: "UpdatePlate", called: "get_localToWorldMatrix"),
            "The plate must use the neutral mast frame, not the rotating sail frame."
        );
        Check(
            Calls(type: rig, method: "RefreshAerodynamics", called: "TryWindFrame")
                && Calls(type: rig, method: "PositionCollisionChecker", called: "PivotRotation"),
            "Aerodynamics and collision travel must use the custom fitted geometry."
        );

        foreach (string lifecycle in new[] { "OnDisable", "OnDestroy" })
            Check(
                Calls(type: rigging, method: lifecycle, called: "ReleaseControls"),
                "Sheets must release claims on " + lifecycle
            );
        Check(
            Calls(type: rigging, method: "ReleaseControls", called: "Unregister")
                && Calls(type: rigging, method: "OnDestroy", called: "Destroy")
                && Calls(type: rigging, method: "ReleaseControls", called: "Retire"),
            "Claims must be released and externally parented controllers destroyed with their owner."
        );
        Check(
            rigging
                .GetMethod(name: "Refresh", bindingAttr: All)
                .GetMethodBody()
                .ExceptionHandlingClauses.Count > 0
                && Instructions(method: rigging.GetMethod(name: "Refresh", bindingAttr: All))
                    .Any(i =>
                        i.Code == OpCodes.Stfld
                        && i.Operand is FieldInfo f
                        && f.Name == "retryAfter"
                    ),
            "Missing donor controls need a retry path rather than a failed installation."
        );
        foreach (var method in rigging.GetMethods(bindingAttr: All | BindingFlags.DeclaredOnly))
        foreach (var instruction in Instructions(method: method))
        {
            Check(
                !(
                    instruction.Code == OpCodes.Stfld
                    && instruction.Operand is FieldInfo f
                    && (f.DeclaringType.Name == "Mast" || f.Name == "currentLength")
                ),
                "Allocation and replacement must preserve native arrays and current sheet lengths."
            );
            Check(
                !(
                    instruction.Operand is FieldInfo field
                    && field.Name is "leftAngleWinch" or "rightAngleWinch"
                ),
                "Short or missing native arrays must be handled by discovery, not indexed by the spritsail."
            );
        }
        var configure = prefab.GetMethod(name: "ConfigureSheet", bindingAttr: All);
        Check(
            Instructions(method: configure)
                .Any(i => i.Operand is FieldInfo f && f.Name == "SheetLineAttachment")
                && Instructions(method: configure)
                    .Any(i => i.Operand is FieldInfo f && f.Name == "jibAngleMaster"),
            "Native sheets must terminate at the custom clew and reference the installed hinge master."
        );
        CheckNativeForceBindings(physics: physics);
        ReefingIntegrationChecks.Run(assembly: assembly);
        Check(
            Calls(type: rig, method: "MeasureMast", called: "RelativeMatrix")
                && !Calls(type: rig, method: "MeasureMast", called: "get_worldToLocalMatrix"),
            "Mast fitting must not depend on cancelling moving world transforms."
        );
        Check(
            Calls(type: rigging, method: "SetSheetsActive", called: "HideClothRope"),
            "Unavailable controls must hide native 3D ropes outside the controller hierarchy."
        );
        Console.WriteLine(
            "PASS (structural): spritsail native force bindings, sheet ownership, retries, lifecycle, stable mast geometry and physics admission."
        );
    }

    private static void CheckNativeForceBindings(Type physics)
    {
        var clear = physics.GetMethod(name: "Clear", bindingAttr: All);
        var sailType = clear.GetParameters()[0].ParameterType;
        // Installed Harmony targets Unity's Mono runtime; inspect its bindings without
        // initializing AccessTools in this .NET host or manufacturing Unity objects.
        var initializer = Instructions(method: physics.TypeInitializer).ToArray();
        var resets = Instructions(method: clear).ToArray();
        string[] fields =
        {
            "unamplifiedForwardForce",
            "unamplifiedSidewayForce",
            "totalWindForce",
            "outFinalForwardForce",
        };
        foreach (string field in fields)
            Check(
                sailType.GetField(name: field, bindingAttr: All)?.FieldType == typeof(float)
                    && initializer.Any(i => i.Operand as string == field),
                "Missing installed native force binding: " + field
            );
        foreach (
            string cache in new[]
            {
                "forwardForce",
                "sideForce",
                "totalForce",
                "finalForce",
                "backwards",
            }
        )
            Check(
                resets.Any(i => i.Operand is FieldInfo f && f.Name == cache),
                "Clearing force omits " + cache
            );
        Check(
            resets.Count(i => i.Code == OpCodes.Stind_R4) == 4
                && resets.Any(i => i.Code == OpCodes.Stind_I1)
                && resets.Any(i =>
                    i.Code == OpCodes.Stfld
                    && i.Operand is FieldInfo f
                    && f.Name == "appliedWindForce"
                ),
            "Clearing force must write all native force accumulators and backwinding state."
        );
        Check(
            sailType.GetField(name: "pushingBoatBackwards", bindingAttr: All)?.FieldType
                == typeof(bool)
                && sailType.GetMethod(name: "UpdateWindForceOnSail", bindingAttr: All)?.ReturnType
                    == typeof(void),
            "Installed native wind calculation contract changed."
        );
    }

    private static bool Calls(Type type, string method, string called) =>
        CalledMethods(method: type.GetMethod(name: method, bindingAttr: All))
            .Any(m => m.Name == called);

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
