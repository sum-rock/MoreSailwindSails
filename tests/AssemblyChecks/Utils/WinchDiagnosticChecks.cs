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
        foreach (var method in type.GetMethods(flags))
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
                    owner != "MoreSailwindSails.Controls.WinchReservations",
                    "Diagnostic participates in placement reservations: " + method.Name
                );
            }
        }
        var trace = assembly.GetType("MoreSailwindSails.Utils.WinchMountTrace", throwOnError: true);
        Check(
            CalledMethods(trace.GetMethod("Status", flags))
                .Any(m => m.DeclaringType.Name == "NativeWinchSeats" && m.Name == "Occupied"),
            "Diagnostic must use the native occupancy predicate, including hidden bound ropes."
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
