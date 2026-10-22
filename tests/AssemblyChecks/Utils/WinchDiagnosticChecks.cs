using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.Utils;

// Guards the diagnostics' read-only boundary against native game-state mutations.
internal static class WinchDiagnosticChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags flags =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly;
        foreach (
            var type in assembly.GetTypes().Where(t => t.Namespace == "MoreSailwindSails.Utils")
        )
        foreach (
            var method in type.GetMethods(flags)
                .Cast<MethodBase>()
                .Concat(type.GetConstructors(flags))
        )
        {
            foreach (var instruction in Instructions(method))
                if (instruction.Code == OpCodes.Stfld || instruction.Code == OpCodes.Stsfld)
                    Check(
                        instruction.Operand is not FieldInfo field
                            || field.DeclaringType.Assembly.GetName().Name != "Assembly-CSharp",
                        "Diagnostic writes native game state: " + method.Name
                    );
            foreach (var called in CalledMethods(method))
            {
                Check(
                    called.Name
                        is not (
                            "SetActive"
                            or "Instantiate"
                            or "AddComponent"
                            or "AttachToController"
                            or "RegisterMast"
                            or "ShowWinch"
                        ),
                    "Diagnostic activates/clones/binds a game object: " + method.Name
                );
                string owner = called.DeclaringType.FullName;
                Check(
                    !called.Name.StartsWith("set_", StringComparison.Ordinal)
                        || owner
                            is not (
                                "UnityEngine.Transform"
                                or "UnityEngine.Renderer"
                                or "UnityEngine.Collider"
                                or "UnityEngine.Mesh"
                                or "UnityEngine.MeshFilter"
                                or "UnityEngine.GameObject"
                            ),
                    "Diagnostic mutates a native rendering/physics source: " + method.Name
                );
                Check(
                    owner != "MoreSailwindSails.Controls.Placement.WinchReservations",
                    "Diagnostic participates in placement reservations: " + method.Name
                );
            }
        }
        var trace = assembly.GetType("MoreSailwindSails.Utils.WinchMountTrace", throwOnError: true);
        var overlay = assembly.GetType(
            "MoreSailwindSails.Utils.WinchMountOverlay",
            throwOnError: true
        );
        var boxes = overlay.GetMethod("CacheFallbackBoxes", flags);
        Check(
            CalledMethods(boxes)
                .Count(m => m.DeclaringType.Name == "SheetFallbackSeat" && m.Name == "Position")
                == 2
                && Instructions(boxes)
                    .Count(i =>
                        i.Operand is FieldInfo f
                        && f.DeclaringType.Name == "SheetFallbackPair"
                        && f.Name == "Offset"
                    ) == 2
                && !CalledMethods(boxes)
                    .Any(m =>
                        m.Name == "Template" || m.DeclaringType.Name == "NativeWinchInventory"
                    ),
            "Both sheet overlay boxes must use the pair's offset even when its donor is unavailable."
        );
        Check(
            CalledMethods(trace.GetMethod("Status", flags))
                .Any(m => m.DeclaringType.Name == "NativeWinchInventory" && m.Name == "Occupied"),
            "Diagnostic must use the native occupancy predicate, including hidden bound ropes."
        );
        var preview = assembly.GetType(
            "MoreSailwindSails.Utils.WinchModelPreview",
            throwOnError: true
        );
        var mesh = assembly.GetType("MoreSailwindSails.Utils.WinchPreviewMesh", throwOnError: true);
        var draw = CalledMethods(mesh.GetMethod("Draw", flags)).Single(m => m.Name == "DrawMesh");
        Check(
            draw.DeclaringType.FullName == "UnityEngine.Graphics"
                && draw.GetParameters().Any(p => p.ParameterType.FullName == "UnityEngine.Camera"),
            "Model previews must use the installed camera-scoped mesh drawing API."
        );
        var collection = CalledMethods(preview.GetMethod("BelongsTo", flags)).ToArray();
        Check(
            collection.Any(m =>
                m.IsGenericMethod && m.GetGenericArguments().Any(t => t.Name == "RopeController")
            )
                && collection.Any(m =>
                    m.IsGenericMethod
                    && m.GetGenericArguments().Any(t => t.Name == "GPButtonRopeWinch")
                ),
            "Preview geometry must exclude attached ropes and nested winches."
        );
        Check(
            CalledMethods(overlay.GetMethod("Template", flags))
                .Any(m =>
                    m.IsGenericMethod
                    && m.GetGenericArguments().Any(t => t.Name == "OwnedWinchMarker")
                ),
            "Fallback preview donors must exclude mod-owned controls."
        );
        Check(
            CalledMethods(overlay.GetMethod("Clear", flags)).Any(m => m.Name == "ClearModels")
                && CalledMethods(overlay.GetMethod("ClearModels", flags))
                    .Any(m => m.DeclaringType == preview && m.Name == "Dispose"),
            "Clearing diagnostics must release preview resources."
        );
        Console.WriteLine(
            "PASS (structural): diagnostics do not activate/clone/bind native objects, write native rendering/physics state, or reserve seats; overlay uses native occupancy. Live rendering and lifecycle remain in-game checks."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
