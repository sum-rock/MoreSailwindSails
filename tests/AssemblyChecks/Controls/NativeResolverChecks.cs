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
        var activeSupport = native.GetMethod("ActiveSupport", flags);
        var supportCalls = CalledMethods(activeSupport).ToArray();
        Check(
            supportCalls.Any(m => m.Name == "get_activeInHierarchy")
                && !supportCalls.Any(m =>
                    m.DeclaringType.Namespace == "MoreSailwindSails.BoatConfiguration"
                )
                && !Instructions(activeSupport).Any(i => i.Operand is FieldInfo),
            "Support activity must use the mount's hierarchy state without authored ancestry or inventory lookups."
        );
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
        var available = native.GetMethod("Available", flags);
        var availableCalls = CalledMethods(available).ToArray();
        Check(
            availableCalls.Any(m => m.Name == "Occupied")
                && availableCalls.Any(m => m.Name == "Position")
                && availableCalls.Any(m => m.Name == "TryGetValue")
                && availableCalls.Any(m =>
                    m.Name == "op_Implicit" && m.DeclaringType.FullName == "UnityEngine.Object"
                )
                && !availableCalls.Any(m => m.DeclaringType == typeof(Enumerable))
                && !Instructions(available)
                    .Any(i => i.Code == System.Reflection.Emit.OpCodes.Newobj),
            "Occupancy must scan live controls and cached aliases without LINQ or captured predicates."
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
                    && CalledMethods(validate).Any(m => m.Name == "IsStaySource")
                    && !CalledMethods(validate)
                        .Any(m => m.Name is "Candidate" or "Resolve" or "StaySources"),
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
            CalledMethods(halyard).Any(m => m.Name == "StaySources")
                && CalledMethods(halyard).Any(m => m.Name == "ActiveSupport")
                && !CalledMethods(halyard).Any(m => m.Name == "SheetCategory"),
            "Halyard bypasses stay dependencies or requested active support."
        );
        var halyardValidate = Type("HalyardWinchPlacementResolver")
            .GetMethod("ValidateCurrent", flags);
        var halyardFallback = Type("HalyardWinchPlacementResolver").GetMethod("Fallback", flags);
        foreach (var method in new[] { halyardValidate, halyardFallback })
            Check(
                CalledMethods(method).Any(m => m.Name == "get_FallbackId"),
                "Halyard creation and retention must share the authored fallback identifier."
            );
        Check(
            CalledMethods(halyard).Any(m => m.Name == "Fallback")
                && CalledMethods(halyard)
                    .Any(m => m.Name == "Resolve" && m.DeclaringType.Name == "WinchPlacementPolicy")
                && CalledMethods(halyardFallback).Any(m => m.Name == "ActiveSupport")
                && CalledMethods(halyardFallback).Any(m => m.Name == "Template")
                && CalledMethods(halyardValidate).Any(m => m.Name == "Template")
                && CalledMethods(halyardValidate).Any(m => m.Name == "MatchesPose")
                && CalledMethods(Type("HalyardWinchPlacementResolver").GetMethod("Template", flags))
                    .Any(m => m.Name == "TemplateUsable"),
            "Manual halyard fallback bypasses allocation, active support, template validity or retained pose checks."
        );
        Check(
            CalledMethods(halyardValidate).Any(m => m.Name == "IsStaySource")
                && CalledMethods(halyardValidate).Any(m => m.Name == "ActiveSupport")
                && Instructions(halyardValidate)
                    .Any(i => i.Operand is FieldInfo f && f.Name == "reefWinch"),
            "Retained halyard does not validate permitted sources, active mast and source array."
        );
        var discovery = native.GetMethod("StaySources", flags);
        var iteratorType = discovery
            .GetCustomAttribute<System.Runtime.CompilerServices.IteratorStateMachineAttribute>()
            .StateMachineType;
        var moveNext = iteratorType.GetMethod("MoveNext", flags);
        var selected = native.GetMethod("IsStaySource", flags);
        var eligibility = native.GetMethod("EligibleStaySource", flags);
        Check(
            CalledMethods(moveNext).Any(m => m == eligibility)
                && CalledMethods(selected).Any(m => m == eligibility)
                && CalledMethods(selected)
                    .Any(m => m.Name == "IndexOf" && m.DeclaringType == typeof(Array))
                && Instructions(selected).Any(i => i.Operand is FieldInfo f && f.Name == "options")
                && !CalledMethods(selected).Any(m => m.Name == "StaySources"),
            "Discovery and retention must share live eligibility, with selected donors still in the inventory."
        );
        foreach (string method in new[] { "Reaches", "TryFind", "Any" })
            Check(
                CalledMethods(eligibility).Any(m => m.Name == method),
                "Stay discovery must check dependencies, exclude owned stays and skip sails: "
                    + method
            );
        Check(
            Instructions(eligibility)
                .Any(i => i.Operand is FieldInfo f && f.Name == "onlyStaysails"),
            "Donors must be native stays."
        );
        foreach (string field in new[] { "availableParts", "partOptions" })
            Check(
                native
                    .GetMethods(flags)
                    .Concat(native.GetNestedTypes(flags).SelectMany(type => type.GetMethods(flags)))
                    .Any(method =>
                        Instructions(method).Any(i => i.Operand is FieldInfo f && f.Name == field)
                    ),
                "Stay discovery must preserve registered option ordering: " + field
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
        var sheetType = Type("SheetingWinchPlacementResolver");
        var sheetValidateCalls = CalledMethods(sheetType.GetMethod("ValidateCurrent", flags))
            .ToArray();
        Check(
            sheetValidateCalls.Any(m => m.Name == "FindCurrent")
                && sheetValidateCalls.Any(m => m.Name == "InverseTransformPoint")
                && !sheetValidateCalls.Any(m => m.Name is "Fallbacks" or "FirstOrDefault"),
            "Retained sheet fallbacks must look up the selected pair directly using the live mast position."
        );
        foreach (string method in new[] { "Fallback", "ValidFallbackSide" })
        {
            var calls = CalledMethods(sheetType.GetMethod(method, flags)).ToArray();
            Check(
                calls.Any(m =>
                    m.DeclaringType.Name == "SharedSheetWinchTemplate" && m.Name == "Get"
                )
                    && !calls.Any(m =>
                        m.DeclaringType.Name == "NativeWinchSeats" && m.Name == "Rotation"
                    ),
                "Sheet fallback must share a donor and must not inherit live donor wheel rotation."
            );
        }
        var sharedTemplate = Type("SharedSheetWinchTemplate");
        var templateGet = sharedTemplate.GetMethod("Get", flags);
        var templateCalls = CalledMethods(templateGet).ToArray();
        Check(
            templateCalls.Any(m => m.Name == "Usable")
                && templateCalls.Any(m => m.Name == "Due")
                && templateCalls.Any(m => m.Name == "Discovered")
                && templateCalls.Any(m => m.Name == "FindObjectsOfTypeAll")
                && templateCalls.Any(m => m.Name == "IsValid")
                && templateCalls.Any(m =>
                    m.Name == "GetComponentsInChildren" && m.GetParameters().Length == 1
                ),
            "Shared donor discovery must validate the cached object, throttle retries and include inactive hierarchy lookup."
        );
        foreach (
            var method in sharedTemplate
                .GetMethods(flags)
                .Where(m => m.DeclaringType == sharedTemplate)
        )
            Check(
                !CalledMethods(method)
                    .Any(m => m.Name is "SetActive" or "AttachToController" or "Occupied"),
                "Shared template discovery must neither mutate nor require a free native donor."
            );
        var usable = native.GetMethod("Usable", flags);
        Check(
            CalledMethods(usable)
                .Any(m =>
                    m.Name == "op_Implicit" && m.DeclaringType.FullName == "UnityEngine.Object"
                ),
            "Destroyed Unity donors must fail template validation and allow rediscovery."
        );
        Console.WriteLine(
            "PASS (structural): native rope/visibility occupancy, inactive hierarchy lookup, stay dependencies with active requested support and non-mutating resolvers."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
