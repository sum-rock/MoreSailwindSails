using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.Controls.Runtime;

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
        var manager = assembly.GetType("MoreSailwindSails.Controls.WinchControls", true);
        var owned = assembly.GetType(
            "MoreSailwindSails.Controls.Runtime.OwnedWinch",
            throwOnError: true
        );
        var clone = assembly.GetType(
            "MoreSailwindSails.Controls.Runtime.WinchClone",
            throwOnError: true
        );
        var group = assembly.GetType(
            "MoreSailwindSails.Controls.Runtime.WinchGroup",
            throwOnError: true
        );
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
        var tick = manager.GetMethod("LateUpdate", all);
        var cleanupCalls = CalledMethods(tick).ToArray();
        Check(
            cleanupCalls.Any(m => m.DeclaringType == owned && m.Name == "Dispose")
                && cleanupCalls.Any(m => m.DeclaringType == owned && m.Name == "Retire"),
            "Stale-control cleanup must distinguish lost owners from broken live controls."
        );
        Check(
            !cleanupCalls.Any(m => m.Name == "ToArray")
                && cleanupCalls
                    .Where(m => m.Name is "Snapshot" or "ReleaseUnused" or "Refresh")
                    .Select(m => m.Name)
                    .SequenceEqual(
                        new[] { "Snapshot", "Snapshot", "ReleaseUnused", "Snapshot", "Refresh" }
                    ),
            "Tick must snapshot cleanup and each group pass separately, releasing before refresh."
        );
        Check(
            cleanupCalls.Count(m => m.Name == "Clear") == 2
                && tick.GetMethodBody()
                    .ExceptionHandlingClauses.Any(c =>
                        c.Flags == ExceptionHandlingClauseOptions.Finally
                    )
                && CalledMethods(manager.GetMethod("OnDestroy", all)).Any(m => m.Name == "ToArray"),
            "Tick must clear both buffers with final cleanup and keep teardown's snapshot independent."
        );
        CheckSnapshots(method: manager.GetMethod("Snapshot", all));
        Check(
            CalledMethods(manager.GetMethod("InvalidateInventory", all))
                .Any(m => m.Name == "NativeBindingComplete"),
            "Native rebinding must preserve controllers beneath hidden mount-owned winches."
        );
        var rebound = CalledMethods(owned.GetMethod("NativeBindingComplete", all)).ToArray();
        Check(
            rebound.Any(m => m.Name == "get_activeInHierarchy")
                && rebound.Any(m => m.Name == "Detach")
                && rebound.Any(m => m.Name == "Dirty"),
            "Hidden native bindings must detach controllers and request a placement retry."
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
        foreach (string family in new[] { "FishermansFlyingSail", "FishermansStaysail" })
        {
            var type = assembly.GetType($"MoreSailwindSails.Sails.{family}.{family}Rigging", true);
            foreach (string method in new[] { "AttachControls", "TryResolve", "OnDestroy" })
                Check(
                    !CalledMethods(type.GetMethod(method, all))
                        .Any(m => m.DeclaringType == manager || m.DeclaringType == owned),
                    family
                        + " must use its mounting Mast's controls rather than independent clones or placement searches."
                );
            Check(
                !type.GetFields(all).Any(field => field.FieldType == owned.MakeArrayType()),
                family + " retains independent owned winches."
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
        var createCalls = CalledMethods(manager.GetMethod("Create", all)).ToArray();
        Check(
            createCalls.Any(m => m.Name == "Sources")
                && createCalls.Any(m => m.Name == "FirstOrDefault"),
            "Stay startup must clone its existing donor assets independently of placement availability."
        );
        Check(
            assembly.GetType("MoreSailwindSails.Controls.WinchBootstrapTemplates") == null,
            "Obsolete placement source searches remain in bootstrap."
        );
        Check(
            CalledMethods(manager.GetMethod("Configure", all)).Any(m => m.Name == "PhysicalMast"),
            "Stay allocation must resolve the aft physical base mast."
        );
        var native = assembly.GetType(
            "MoreSailwindSails.Controls.Discovery.NativeWinchInventory",
            true
        );
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

    private static void CheckSnapshots(MethodInfo method)
    {
        var snapshot = method
            .MakeGenericMethod(typeof(object))
            .CreateDelegate<Func<List<object>, List<object>, List<object>>>();
        var first = new object();
        var second = new object();
        var source = new List<object> { first, second };
        var buffer = new List<object>();
        Check(ReferenceEquals(snapshot(source, buffer), buffer), "Snapshot must reuse its buffer.");
        int visited = 0;
        foreach (var item in buffer)
        {
            source.Remove(item);
            visited++;
        }
        Check(visited == 2 && source.Count == 0, "Source removals must not skip snapshot entries.");
        source.Add(second);
        snapshot(source, buffer);
        Check(
            buffer.Count == 1 && buffer[0] == second,
            "Next pass must capture the updated source."
        );
        source.Clear();
        snapshot(source, buffer);
        Check(buffer.Count == 0, "Empty capture must release stale references.");
        source.Add(first);
        for (int i = 0; i < 1000; i++)
            snapshot(source, buffer);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            snapshot(source, buffer);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, "Warmed snapshot copies must not allocate replacement collections.");
        Console.WriteLine(
            "PASS: snapshot mutation isolation, fresh passes, empty cleanup and 0 allocated bytes for 1000 warmed copies (.NET test host)."
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
