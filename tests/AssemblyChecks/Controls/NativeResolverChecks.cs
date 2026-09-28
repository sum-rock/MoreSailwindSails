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
        var prepare = native.GetMethod("Prepare", flags);
        Check(
            CalledMethods(prepare).Any(m => m.Name == "ArraysChanged")
                && CalledMethods(prepare).Any(m => m.Name == "Due")
                && CalledMethods(prepare).Any(m => m.Name == "Refresh"),
            "Discovery no longer watches native arrays or periodic/lifecycle invalidation."
        );
        var current = native.GetMethod("Current", flags);
        foreach (string check in new[] { "Mounted", "SamePose", "SequenceEqual", "Available" })
            Check(
                CalledMethods(current).Any(m => m.Name == check),
                "Current placement misses live " + check
            );
        Check(
            CalledMethods(native.GetMethod("Available", flags)).Any(m => m.Name == "Occupied"),
            "Alias occupancy is frozen between discovery scans."
        );
        foreach (
            string resolver in new[]
            {
                "SheetingWinchPlacementResolver",
                "HalyardWinchPlacementResolver",
            }
        )
        {
            var validate = Type(resolver).GetMethod("ValidateCurrent", flags);
            Check(
                CalledMethods(validate).Any(m => m.Name == "Current")
                    && !CalledMethods(validate).Any(m => m.Name is "Candidate" or "Resolve"),
                "Current-placement validation searches alternatives or skips live seats."
            );
        }
        var manager = Type("FishermanWinchControls");
        var tick = manager.GetMethod("LateUpdate", flags);
        var instructions = Instructions(tick).ToArray();
        int firstReturn = Array.FindIndex(
            instructions,
            i => i.Code == System.Reflection.Emit.OpCodes.Ret
        );
        int inventoryWork = Array.FindIndex(
            instructions,
            i => i.Operand is MethodInfo m && m.Name == "Prepare"
        );
        Check(
            firstReturn >= 0
                && firstReturn < inventoryWork
                && instructions
                    .Take(firstReturn)
                    .Any(i => i.Operand is MethodInfo m && m.Name == "get_Count"),
            "Empty manager cannot skip inventory work."
        );
        var group = manager.GetNestedType("ControlGroup", BindingFlags.NonPublic);
        var groupRefresh = group
            .GetMethods(flags)
            .Single(m => m.Name == "Refresh" && m.GetParameters().Length == 3);
        var groupCalls = CalledMethods(groupRefresh).ToArray();
        Check(
            Array.FindIndex(groupCalls, m => m.Name == "TryRetain")
                < Array.FindIndex(groupCalls, m => m.Name == "Resolve"),
            "Stable claims search before validation."
        );
        var halyard = Type("HalyardWinchPlacementResolver").GetMethod("Resolve", flags);
        Check(
            Instructions(halyard).Any(i => i.Operand is FieldInfo f && f.Name == "reefWinch"),
            "Halyard array not inspected."
        );
        Check(
            CalledMethods(halyard).Any(m => m.Name == "HalyardSources")
                && CalledMethods(halyard).Any(m => m.Name == "ActiveSupport")
                && !CalledMethods(halyard).Any(m => m.Name == "SheetCategory"),
            "Halyard bypasses authored sources or requested active support."
        );
        var halyardValidate = Type("HalyardWinchPlacementResolver")
            .GetMethod("ValidateCurrent", flags);
        Check(
            CalledMethods(halyardValidate).Any(m => m.Name == "HalyardSources")
                && CalledMethods(halyardValidate).Any(m => m.Name == "ActiveSupport")
                && Instructions(halyardValidate)
                    .Any(i => i.Operand is FieldInfo f && f.Name == "reefWinch"),
            "Retained halyard does not validate permitted sources, active mast and source array."
        );
        Check(
            CalledMethods(Type("WinchBootstrapTemplates").GetMethod("TryGet", flags))
                .Any(m => m.Name == "HalyardSources"),
            "Bootstrap does not use the same authored halyard sources."
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
            "PASS (structural): native rope/visibility occupancy, inactive hierarchy lookup, authored halyard sources with active requested support and non-mutating resolvers."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
