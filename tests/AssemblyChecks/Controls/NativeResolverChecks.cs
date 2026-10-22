using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
        var sheetResolver = Type("Placement.SheetingWinchPlacementResolver");
        foreach (string method in new[] { "Resolve", "ValidateCurrent" })
            Check(
                sheetResolver
                    .GetMethod(method, flags)
                    .GetParameters()
                    .Any(p => p.Name == "aftBoundaryZ" && p.ParameterType == typeof(float)),
                "Sheet selection and retention must both receive the fitted boundary."
            );
        var rig = assembly.GetType(
            "MoreSailwindSails.Sails.FishermansStaysail.FishermansStaysailRig",
            true
        );
        Check(
            CalledMethods(rig.GetMethod("RefreshFrame", flags)).Any(m => m.Name == "SheetSpan")
                && CalledMethods(rig.GetMethod("OnDisable", flags))
                    .Any(m => m.Name == "InvalidateSheetSpan")
                && !CalledMethods(rig.GetMethod("get_FittedSheetSpan", flags))
                    .Any(m => m.Name is "SheetSpan" or "GetComponent" or "RefreshFrame"),
            "Fitted span must be cached by the shared rig and invalidated when disabled."
        );
        var spanGroup = Type("Runtime.WinchGroup");
        var boundaryCalls = CalledMethods(spanGroup.GetMethod("SheetBoundary", flags)).ToArray();
        Check(
            boundaryCalls.Any(m => m.Name == "get_FittedSheetSpan")
                && boundaryCalls.Any(m => m.Name == "Boundary")
                && !boundaryCalls.Any(m => m.Name is "GetComponentsInChildren" or "RefreshFrame"),
            "Placement must read cached span without traversing sail geometry."
        );
        Check(
            CalledMethods(Type("WinchControls").GetMethod("Configure", flags))
                .Any(m => m.Name == "ResetSheetSpan")
                && CalledMethods(spanGroup.GetMethod("ResetSheetSpan", flags))
                    .Any(m => m.Name == "InvalidateSheetSpan"),
            "Supporting mast changes must invalidate the fitted span."
        );
        var native = Type("Discovery.NativeWinchInventory");
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
            var validate = Type("Placement." + resolver).GetMethod("ValidateCurrent", flags);
            foreach (
                var method in new[]
                {
                    validate,
                    Type("Placement." + resolver).GetMethod("Resolve", flags),
                }
            )
                Check(
                    method
                        .GetParameters()
                        .Any(p => p.Name == "forward" && p.ParameterType.Name == "Mast")
                        && method
                            .GetParameters()
                            .Any(p => p.Name == "requested" && p.ParameterType.Name == "Mast"),
                    "Allocation and retention must receive both physical masts."
                );
            Check(
                CalledMethods(validate).Any(m => m.Name == "Current")
                    && CalledMethods(validate).Any(m => m.Name == "IsStaySource")
                    && !CalledMethods(validate)
                        .Any(m => m.Name is "Candidate" or "Resolve" or "StaySources"),
                "Current-placement validation searches alternatives or skips live seats."
            );
        }
        var manager = Type("WinchControls");
        var configure = manager.GetMethod("Configure", flags);
        Check(
            CalledMethods(configure).Count(m => m.Name == "PhysicalMast") == 2
                && Instructions(configure)
                    .Any(i => i.Operand is FieldInfo f && f.Name == "ForwardBase"),
            "Both mast sections must resolve to physical bases when the group is configured."
        );
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
        var group = Type("Runtime.WinchGroup");
        var groupRefresh = group
            .GetMethods(flags)
            .Single(m => m.Name == "Refresh" && m.GetParameters().Length == 3);
        Check(
            Instructions(groupRefresh)
                .Count(i => i.Operand is FieldInfo f && f.Name == "ForwardBase") == 4,
            "Both resolvers must receive the forward base for allocation and retention."
        );
        var groupCalls = CalledMethods(groupRefresh).ToArray();
        Check(
            Array.FindIndex(groupCalls, m => m.Name == "TryRetain")
                < Array.FindIndex(groupCalls, m => m.Name == "Resolve"),
            "Stable claims search before validation."
        );
        var halyard = Type("Placement.HalyardWinchPlacementResolver").GetMethod("Resolve", flags);
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
        var halyardValidate = Type("Placement.HalyardWinchPlacementResolver")
            .GetMethod("ValidateCurrent", flags);
        var halyardFallback = Type("Placement.HalyardWinchPlacementResolver")
            .GetMethod("Fallback", flags);
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
                && CalledMethods(
                        Type("Placement.HalyardWinchPlacementResolver").GetMethod("Template", flags)
                    )
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
        foreach (var entry in new[] { moveNext, selected })
            Check(
                CalledMethods(entry)
                    .Any(m =>
                        m.Name == "op_Equality" && m.DeclaringType.FullName == "UnityEngine.Object"
                    ),
                "Native donor search and retention must reject a pair with identical endpoints."
            );
        Check(
            CalledMethods(moveNext).Any(m => m == eligibility)
                && CalledMethods(selected).Any(m => m == eligibility)
                && CalledMethods(selected)
                    .Any(m => m.Name == "IndexOf" && m.DeclaringType == typeof(Array))
                && Instructions(selected).Any(i => i.Operand is FieldInfo f && f.Name == "options")
                && !CalledMethods(selected).Any(m => m.Name == "StaySources"),
            "Discovery and retention must share live eligibility, with selected donors still in the inventory."
        );
        foreach (string method in new[] { "ReachesBoth", "TryFind", "Any" })
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
                Type("Placement.SheetingWinchPlacementResolver"),
                Type("Placement.HalyardWinchPlacementResolver"),
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
        var sheets = Type("Placement.SheetingWinchPlacementResolver").GetMethod("Resolve", flags);
        Check(
            CalledMethods(sheets)
                .Any(m => m.DeclaringType.Name == "WinchPlacementPolicy" && m.Name == "Resolve"),
            "Sheet resolver bypasses atomic allocation policy."
        );
        var sheetType = Type("Placement.SheetingWinchPlacementResolver");
        var boundaryBody = Instructions(group.GetMethod("SheetBoundary", flags)).ToArray();
        Check(
            boundaryBody.Any(i => i.Operand is FieldInfo f && f.Name == "ForwardBase")
                && boundaryCalls.Any(m => m.Name == "InverseTransformPoint")
                && CalledMethods(sheetType.GetMethod("Fallbacks", flags))
                    .Any(m => m.Name == "FromBoundary")
                && CalledMethods(sheets).Any(m => m.Name == "Fallbacks"),
            "Fallback selection must use the live forward base position and cached fitted span."
        );
        var refreshBody = Instructions(groupRefresh).ToArray();
        Check(
            Array.FindIndex(
                refreshBody,
                i => i.Operand is MethodInfo m && m.Name == "SheetBoundary"
            )
                < Array.FindIndex(
                    refreshBody,
                    i => i.Operand is FieldInfo f && f.Name == "RetryAfter"
                ),
            "Fitted size changes must be read before the retry gate."
        );
        foreach (var method in new[] { sheets, sheetType.GetMethod("ValidateCurrent", flags) })
            Check(
                CalledMethods(method).Count(m => m.Name == "ActiveSupport") == 2,
                "Sheet selection and retention must check both physical mast supports."
            );
        var sheetValidateCalls = CalledMethods(sheetType.GetMethod("ValidateCurrent", flags))
            .ToArray();
        Check(
            sheetValidateCalls.Any(m => m.Name == "FindCurrent")
                && !sheetValidateCalls.Any(m => m.Name == "InverseTransformPoint")
                && !sheetValidateCalls.Any(m => m.Name is "Fallbacks" or "FirstOrDefault"),
            "Retained sheet fallbacks must look up the selected pair directly using the live mast position."
        );
        foreach (string method in new[] { "Fallback", "ValidFallbackSide" })
        {
            var calls = CalledMethods(sheetType.GetMethod(method, flags)).ToArray();
            Check(
                calls.Any(m => m.DeclaringType == sheetType && m.Name == "Template")
                    && calls.Any(m => m.DeclaringType == native && m.Name == "Rotation"),
                "Sheet allocation and retention must resolve the pair's donor and cached mounting frame."
            );
        }
        var templateGet = sheetType.GetMethod("Template", flags);
        var templateCalls = CalledMethods(templateGet).ToArray();
        Check(
            templateCalls.Any(m => m.DeclaringType == native && m.Name == "Mast")
                && templateCalls.Any(m => m.DeclaringType == native && m.Name == "Sources")
                && templateCalls.Any(m => m.DeclaringType == native && m.Name == "TemplateUsable")
                && Instructions(templateGet).Any(i => i.Code == OpCodes.Ldlen),
            "Sheet donors must resolve the configured local mast/array, check its bounds, and validate the control."
        );
        Check(
            !templateCalls.Any(m =>
                m.Name
                    is "SetActive"
                        or "AttachToController"
                        or "Occupied"
                        or "ActiveSupport"
                        or "FindObjectsOfTypeAll"
                        or "GetComponentsInChildren"
            ),
            "Donor lookup must use the existing inventory and allow occupied or unfitted sources without mutation."
        );
        var rotation = native.GetMethod("Rotation", flags);
        Check(
            CalledMethods(rotation).Any(m => m.Name == "TryGetValue")
                && Instructions(rotation)
                    .Any(i => i.Operand is FieldInfo f && f.Name == "wheelFrames"),
            "Donor mounting rotation must retain the cached wheel frame across wheel input."
        );
        var templateUsable = native.GetMethod("TemplateUsable", flags);
        Check(
            CalledMethods(templateUsable).Any(m => m.Name == "Usable")
                && Instructions(templateUsable)
                    .Any(i => i.Operand is FieldInfo f && f.Name == "owned"),
            "Missing/destroyed controls and mod-owned clones must not become donors."
        );
        var seatType = Type("Configuration.SheetFallbackSeat");
        var seatRotation = CalledMethods(seatType.GetMethod("Rotation", flags)).ToArray();
        Check(
            seatRotation.Any(m => m.Name == "FromToRotation")
                && seatRotation.Any(m => m.Name == "op_Multiply"),
            "Sheet mounting must align the donor normal to the destination before applying the donor rotation."
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
