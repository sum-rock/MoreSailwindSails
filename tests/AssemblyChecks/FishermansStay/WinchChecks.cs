using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.FishermansStay;

internal static class WinchChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags all =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.Instance;
        var manager = assembly.GetType("MoreSailwindSails.Controls.FishermanWinchControls", true);
        var owned = manager.GetNestedType("OwnedWinch", BindingFlags.NonPublic);
        var constructor = owned.GetConstructors(all).Single();
        var source = manager.GetMethod("Source", all);
        var sourceInstructions = Instructions(source).ToArray();
        if (!sourceInstructions.Any(i => i.Operand is FieldInfo f && f.Name == "SourceIndex"))
            throw new Exception("Runtime winch selection ignores the authored donor row.");
        if (
            !sourceInstructions.Any(i => i.Operand is FieldInfo f && f.Name == "SourceMast")
            || CalledMethods(source).Count(m => m.Name == "Sources") != 2
            || CalledMethods(source).Contains(source)
        )
            throw new Exception(
                "Authored mast overrides must directly reselect their native role array."
            );
        // The Jong regression passed geometry checks but failed during stay
        // construction: native Awake had not populated boat.masts[7] yet.
        // Verify the actual lookup includes inactive hierarchy objects and uses
        // the authored ID, without depending on or forcing native registration.
        int donorSearch = Array.FindIndex(
            sourceInstructions,
            i =>
                i.Operand is MethodInfo m
                && m.Name == "GetComponentsInChildren"
                && m.IsGenericMethod
                && m.GetGenericArguments().Single().Name == "Mast"
        );
        if (
            donorSearch <= 0
            || sourceInstructions[donorSearch - 1].Code != OpCodes.Ldc_I4_1
            || sourceInstructions.Any(i => i.Operand is FieldInfo f && f.Name == "masts")
        )
            throw new Exception(
                "Override donors must be found before Awake, including inactive native options."
            );
        var donorPredicate = CalledMethods(source)
            .OfType<MethodInfo>()
            .Single(m => m.Name.StartsWith("<Source>b__", StringComparison.Ordinal));
        var predicateInstructions = Instructions(donorPredicate).ToArray();
        if (
            !predicateInstructions.Any(i => i.Operand is FieldInfo f && f.Name == "orderIndex")
            || !predicateInstructions.Any(i => i.Operand is FieldInfo f && f.Name == "SourceMast")
            || !predicateInstructions.Any(i => i.Code == OpCodes.Ceq)
        )
            throw new Exception("Hierarchy donor lookup must match the authored mast ID.");
        if (
            CalledMethods(source)
                .Concat(CalledMethods(donorPredicate))
                .Any(m =>
                    m.Name
                        is "SetActive"
                            or "RegisterMast"
                            or "get_activeInHierarchy"
                            or "get_activeSelf"
                )
        )
            throw new Exception(
                "Donor lookup must neither require activation nor force native registration."
            );
        var nativeMast = source.GetParameters()[1].ParameterType;
        if (!CalledMethods(nativeMast.GetMethod("Awake", all)).Any(m => m.Name == "RegisterMast"))
            throw new Exception("Recheck the installed native mast-registration lifecycle.");
        int firstControl = Array.FindIndex(
            sourceInstructions,
            i => i.Operand is MethodBase m && m.Name == "FirstOrDefault"
        );
        int profileLookup = Array.FindIndex(
            sourceInstructions,
            i =>
                i.Operand is MethodBase m
                && m.Name == "Find"
                && m.DeclaringType.Name == "BoatRigCatalog"
        );
        if (
            firstControl < 0
            || profileLookup <= firstControl
            || !sourceInstructions
                .Skip(firstControl + 1)
                .Take(profileLookup - firstControl - 1)
                .Any(i => i.Code == System.Reflection.Emit.OpCodes.Ret)
        )
            throw new Exception(
                "Absent optional native controls must return before requiring a mounting profile."
            );
        foreach (string method in new[] { "Create", "Reconcile" })
            if (!CalledMethods(manager.GetMethod(method, all)).Contains(source))
                throw new Exception(method + " bypasses common authored winch selection.");
        var cloneCalls = CalledMethods(constructor).ToArray();
        if (
            !cloneCalls.Any(m => m.Name == "ResetClonedOutline")
            || !cloneCalls.Any(m => m.Name == "SetActive")
            || !cloneCalls.Any(m => m.Name == "Instantiate")
        )
            throw new Exception(
                "Common winch construction must initialize inactive clones and reset outlines."
            );
        var placement = CalledMethods(owned.GetMethod("Position", all)).ToArray();
        if (placement.Any(m => m.Name is "set_localRotation" or "set_localEulerAngles" or "Rotate"))
            throw new Exception("Mount refresh must not overwrite native winch input rotation.");
        var dispose = CalledMethods(owned.GetMethod("Dispose", all)).ToArray();
        if (
            !dispose.Any(m => m.Name == "Suspend")
            || !dispose.Any(m => m.Name == "SetParent")
            || !dispose.Any(m => m.Name == "Destroy")
        )
            throw new Exception(
                "Winch disposal must release its slot, preserve the native controller, and destroy owned objects."
            );
        var suspend = CalledMethods(owned.GetMethod("Suspend", all)).ToArray();
        if (!suspend.Any(m => m.Name == "Release") || !suspend.Any(m => m.Name == "SetActive"))
            throw new Exception("Unused controls must be inactive and release allocation.");
        var refresh = owned.GetMethod("Refresh", all);
        if (
            !CalledMethods(refresh).Any(m => m.Name == "PlacementContext")
            || !CalledMethods(refresh)
                .Any(m => m.Name == "Acquire" && m.GetParameters().Length == 6)
            || !CalledMethods(refresh).Any(m => m.Name == "Suspend")
        )
            throw new Exception(
                "Exhaustion must collect rejection counts and context while suspending safely."
            );
        foreach (string family in new[] { "FishermansFlyingSail", "FishermansStaysail" })
        {
            var type = assembly.GetType($"MoreSailwindSails.Sails.{family}.{family}Rigging", true);
            if (
                !CalledMethods(type.GetMethod("AttachControls", all))
                    .Any(m => m.DeclaringType == manager && m.Name == "Reconcile")
                || !CalledMethods(type.GetMethod("OnDestroy", all))
                    .Any(m => m.DeclaringType == owned && m.Name == "Dispose")
            )
                throw new Exception(family + " bypasses shared control ownership.");
        }
        var stay = assembly.GetType("MoreSailwindSails.Stays.FishermansStay.FishermansStay", true);
        if (
            !CalledMethods(stay.GetMethod("CloneWinches", all))
                .Any(m => m.DeclaringType == manager && m.Name == "Create")
        )
            throw new Exception("Stay-owned vanilla controls bypass the shared clone factory.");
        Console.WriteLine(
            "PASS: structural winch checks for common clone setup, mounting/input separation and all three ownership paths; Unity activation, handles and outlines are not executed."
        );
    }
}
