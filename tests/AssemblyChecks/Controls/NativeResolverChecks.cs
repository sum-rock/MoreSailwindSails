using System;
using System.Linq;
using System.Reflection;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.Controls;

internal static class NativeResolverChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags flags =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static;
        Type Type(string name) => assembly.GetType("MoreSailwindSails.Controls." + name, true);
        var native = Type("NativeWinchSeats");
        var occupied = native.GetMethod("Occupied", flags);
        Check(
            Instructions(occupied).Any(i => i.Operand is FieldInfo f && f.Name == "rope"),
            "Bound native rope not checked."
        );
        foreach (string component in new[] { "Renderer", "Collider" })
            Check(
                CalledMethods(occupied)
                    .Any(m => m.Name == "get_enabled" && m.DeclaringType.Name == component),
                "Visibility occupancy missing " + component
            );
        var refresh = native.GetMethod("Refresh", flags);
        Check(
            !Instructions(refresh).Any(i => i.Operand is FieldInfo f && f.Name == "masts"),
            "Inventory depends on live mast array."
        );
        Check(
            CalledMethods(refresh).Any(m => m.Name == "GetComponentsInChildren"),
            "Inactive hierarchy discovery missing."
        );
        var halyard = Type("HalyardWinchPlacementResolver").GetMethod("Resolve", flags);
        Check(
            Instructions(halyard).Any(i => i.Operand is FieldInfo f && f.Name == "reefWinch"),
            "Halyard array not inspected."
        );
        Check(
            !CalledMethods(halyard).Any(m => m.Name is "SheetCategory" or "Mast"),
            "Halyard substitutes another mast."
        );
        foreach (
            var type in new[]
            {
                native,
                Type("SheetingWinchPlacementResolver"),
                Type("HalyardWinchPlacementResolver"),
            }
        )
        foreach (var method in type.GetMethods(flags).Where(m => m.DeclaringType == type))
            Check(
                !CalledMethods(method)
                    .Any(m =>
                        m.Name
                            is "SetActive"
                                or "RegisterMast"
                                or "AttachToController"
                                or "Candidates"
                    ),
                "Resolver mutates native controls or generates seats: " + method.Name
            );
        var sheets = Type("SheetingWinchPlacementResolver").GetMethod("Resolve", flags);
        Check(
            CalledMethods(sheets)
                .Any(m => m.DeclaringType.Name == "WinchPlacementPolicy" && m.Name == "Resolve"),
            "Sheet resolver bypasses atomic allocation policy."
        );
        Console.WriteLine(
            "PASS (structural): native rope/visibility occupancy, inactive hierarchy lookup, mast-local halyards and non-mutating resolvers."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
