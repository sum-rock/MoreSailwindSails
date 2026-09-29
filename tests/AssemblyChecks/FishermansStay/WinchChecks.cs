using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.FishermansStay;

// Checks winch ownership and controller lifetime contracts without running Unity.
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
        var clone = manager.GetNestedType("Clone", BindingFlags.NonPublic);
        var group = manager.GetNestedType("ControlGroup", BindingFlags.NonPublic);
        var prepare = owned.GetMethod("Prepare", all);
        var calls = CalledMethods(prepare).ToArray();
        Check(
            calls.Any(m => m.Name == "ResetClonedOutline")
                && calls.Any(m => m.Name == "Instantiate")
                && calls.Any(m => m.Name == "SetActive"),
            "Inactive clone/outline initialization missing."
        );
        var position = CalledMethods(owned.GetMethod("Position", all)).ToArray();
        Check(
            position.Any(m => m.Name == "SetPositionAndRotation")
                && !position.Any(m =>
                    m.Name is "set_localRotation" or "set_localEulerAngles" or "Rotate"
                ),
            "Placement overwrites wheel input rotation."
        );
        var suspend = Instructions(owned.GetMethod("Suspend", all)).ToArray();
        Check(
            suspend[0].Code == OpCodes.Ldarg_1
                && (suspend[1].Code == OpCodes.Brfalse_S || suspend[1].Code == OpCodes.Brfalse)
                && suspend.Any(i => i.Operand is MethodInfo m && m.Name == "Detach"),
            "Suspension must gate controller detachment on the preservation argument."
        );
        Check(
            CalledMethods(clone.GetMethod("Detach", all)).Any(m => m.Name == "SetParent"),
            "Controllers are not preserved outside destroyed mounts."
        );
        Check(
            !CalledMethods(clone.GetMethod("Destroy", all))
                .Any(m => m.Name is "Detach" or "SetParent"),
            "Final clone destruction must not reparent controllers into a dying boat."
        );
        var disposeCalls = CalledMethods(owned.GetMethod("Dispose", all)).ToArray();
        Check(
            disposeCalls.Any(m => m.Name == "Remove")
                && disposeCalls.Any(m => m.DeclaringType == clone && m.Name == "Destroy")
                && !disposeCalls.Any(m => m.Name is "Detach" or "Retire" or "SetParent"),
            "Disposal bypasses group claim cleanup."
        );
        CheckLastArgument(group.GetMethod("Remove", all), "Suspend", OpCodes.Ldc_I4_0, 1);
        CheckLastArgument(group.GetMethod("Suspend", all), "Release", OpCodes.Ldarg_1, 2);
        CheckLastArgument(group.GetMethod("Release", all), "Suspend", OpCodes.Ldarg_3, 1);
        var retire = owned.GetMethod("Retire", all);
        CheckLastArgument(retire, "Suspend", OpCodes.Ldc_I4_1, 1);
        CheckOrderedCalls(retire, "Suspend", "Detach", "Dispose");
        foreach (string method in new[] { "Dispose", "Retire" })
        {
            var instructions = Instructions(owned.GetMethod(method, all)).ToArray();
            Check(
                instructions.Take(4).Any(i => i.Code == OpCodes.Ret)
                    && instructions
                        .Take(4)
                        .Any(i => i.Operand is FieldInfo f && f.Name == "disposed"),
                method + " no longer returns early for already disposed controls."
            );
        }
        var reconcileCalls = CalledMethods(manager.GetMethod("Reconcile", all)).ToArray();
        Check(
            reconcileCalls.Count(m => m.DeclaringType == owned && m.Name == "Retire") == 2
                && !reconcileCalls.Any(m => m.DeclaringType == owned && m.Name == "Dispose")
                && reconcileCalls.Any(m => m.DeclaringType == group && m.Name == "Set"),
            "Reconciliation replacement/rollback must preserve controllers and restore group slots."
        );
        var cleanupCalls = CalledMethods(manager.GetMethod("LateUpdate", all)).ToArray();
        Check(
            cleanupCalls.Any(m => m.DeclaringType == owned && m.Name == "Dispose")
                && cleanupCalls.Any(m => m.DeclaringType == owned && m.Name == "Retire"),
            "Stale-control cleanup must distinguish lost owners from broken live controls."
        );
        var adopt = owned.GetMethod("Adopt", all);
        CheckOrderedCalls(adopt, "Detach", "Destroy", "Bind");
        foreach (string field in new[] { "reefWinch", "leftAngleWinch", "rightAngleWinch" })
            Check(
                Instructions(adopt).Any(i => i.Operand is FieldInfo f && f.Name == field),
                "Template replacement leaves a stale native mount array: " + field
            );
        var refresh = group
            .GetMethods(all)
            .Single(m => m.Name == "Refresh" && m.GetParameters().Length == 3);
        var refreshCalls = CalledMethods(refresh).ToArray();
        foreach (
            string resolver in new[]
            {
                "SheetingWinchPlacementResolver",
                "HalyardWinchPlacementResolver",
            }
        )
            Check(
                refreshCalls.Any(m => m.DeclaringType.Name == resolver && m.Name == "Resolve"),
                "Coordinator bypasses " + resolver
            );
        Check(
            Array.FindIndex(refreshCalls, m => m.Name == "Prepare")
                < Array.FindIndex(refreshCalls, m => m.Name == "Adopt"),
            "Old clones retired before preparing replacements."
        );
        Check(
            CalledMethods(group.GetMethod("Release", all))
                .Any(m => m.Name == "Release" && m.DeclaringType.Name == "WinchReservations"),
            "Pair cleanup bypasses shared ledger."
        );
        Check(
            CalledMethods(manager.GetMethod("BindSheets", all)).Count(m => m.Name == "Bind") == 2,
            "Sheet controllers are not bound together."
        );
        foreach (string family in new[] { "FishermansFlyingSail", "FishermansStaysail" })
        {
            var type = assembly.GetType($"MoreSailwindSails.Sails.{family}.{family}Rigging", true);
            var attach = type.GetMethod("AttachControls", all);
            Check(
                CalledMethods(attach).Any(m => m.DeclaringType == manager && m.Name == "Reconcile")
                    && CalledMethods(attach)
                        .Any(m => m.DeclaringType == manager && m.Name == "BindSheets")
                    && Instructions(attach).Any(i => i.Operand is FieldInfo f && f.Name == "Fore"),
                family + " does not pass physical forward mast and paired bindings."
            );
            Check(
                CalledMethods(type.GetMethod("OnDestroy", all))
                    .Any(m => m.DeclaringType == owned && m.Name == "Dispose")
                    && !CalledMethods(type.GetMethod("OnDestroy", all))
                        .Any(m => m.Name == "Retire"),
                family + " bypasses control cleanup."
            );
        }
        var stay = assembly.GetType("MoreSailwindSails.Stays.FishermansStay.FishermansStay", true);
        foreach (
            var cleanup in new[]
            {
                manager.GetMethod("OnDestroy", all),
                stay.GetMethod("Destroy", all),
            }
        )
            Check(
                CalledMethods(cleanup).Any(m => m.DeclaringType == owned && m.Name == "Dispose")
                    && !CalledMethods(cleanup).Any(m => m.Name == "Retire"),
                cleanup.DeclaringType.Name + " must use final disposal during destruction."
            );
        Check(
            CalledMethods(stay.GetMethod("Create", all))
                .Any(m => m.DeclaringType == manager && m.Name == "Configure"),
            "Native sails on stays lack physical mast references."
        );
        Check(
            CalledMethods(stay.GetMethod("CloneWinches", all))
                .Any(m => m.DeclaringType == manager && m.Name == "Create"),
            "Stay startup bypasses owned clones."
        );
        Check(
            assembly.GetType("MoreSailwindSails.Controls.WinchPlacementGeometry") == null,
            "Generated placement remains in the plugin."
        );
        var bootstrap = assembly.GetType(
            "MoreSailwindSails.Controls.WinchBootstrapTemplates",
            true
        );
        Check(
            CalledMethods(manager.GetMethod("Create", all))
                .Any(m => m.DeclaringType == bootstrap && m.Name == "TryGet"),
            "Owned startup clones bypass shared bootstrap selection."
        );
        var createCalls = CalledMethods(manager.GetMethod("Create", all)).ToArray();
        Check(
            Array.FindIndex(
                createCalls,
                m => m.Name == "Prepare" && m.DeclaringType.Name == "NativeWinchSeats"
            ) >= 0
                && Array.FindIndex(
                    createCalls,
                    m => m.Name == "Prepare" && m.DeclaringType.Name == "NativeWinchSeats"
                ) < Array.FindIndex(createCalls, m => m.Name == "TryGet"),
            "Startup clones select templates before inventory discovery."
        );
        foreach (string family in new[] { "FishermansFlyingSail", "FishermansStaysail" })
        {
            var type = assembly.GetType($"MoreSailwindSails.Sails.{family}.{family}Rigging", true);
            Check(
                CalledMethods(type.GetMethod("TryResolve", all))
                    .Any(m => m.DeclaringType == manager && m.Name == "BootstrapReady"),
                family + " readiness bypasses shared template policy."
            );
        }
        foreach (var method in bootstrap.GetMethods(all).Where(m => m.DeclaringType == bootstrap))
            Check(
                !CalledMethods(method)
                    .Any(m =>
                        m.Name is "TryAcquireSeats" or "Occupied" or "Available" or "ActiveSupport"
                    ),
                "Bootstrap templates depend on placement vacancy or reserve native seats."
            );
        var native = assembly.GetType("MoreSailwindSails.Controls.NativeWinchSeats", true);
        var usable = native.GetMethod("Usable", all);
        var show = usable.GetParameters()[0].ParameterType.GetMethod("ShowWinch", all);
        foreach (string component in new[] { "Renderer", "Collider" })
            Check(
                CalledMethods(show)
                    .Any(m => m.Name == "set_enabled" && m.DeclaringType.Name == component),
                "Installed ShowWinch visibility contract changed."
            );
        Console.WriteLine(
            "PASS (structural): paired binding, controller-preserving retirement/replacement, final disposal without rope reparenting, native mount references, all ownership paths and generated-search removal. Unity lifecycle remains unexecuted."
        );
    }

    private static void CheckLastArgument(
        MethodInfo method,
        string callee,
        OpCode argument,
        int count
    )
    {
        var instructions = Instructions(method).ToArray();
        var calls = Enumerable
            .Range(1, instructions.Length - 1)
            .Where(i => instructions[i].Operand is MethodInfo m && m.Name == callee)
            .ToArray();
        Check(
            calls.Length == count && calls.All(i => instructions[i - 1].Code == argument),
            method.DeclaringType.Name
                + "."
                + method.Name
                + " loses the preservation policy at "
                + callee
        );
    }

    private static void CheckOrderedCalls(MethodInfo method, params string[] names)
    {
        var calls = CalledMethods(method).ToArray();
        int previous = -1;
        foreach (string name in names)
        {
            int index = Array.FindIndex(calls, m => m.Name == name);
            Check(index > previous, method.Name + " must call " + string.Join(" before ", names));
            previous = index;
        }
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
