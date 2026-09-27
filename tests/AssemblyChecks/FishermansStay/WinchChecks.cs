using System;
using System.Linq;
using System.Reflection;
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
        Check(
            CalledMethods(owned.GetMethod("Suspend", all)).Any(m => m.Name == "Detach"),
            "Hiding controls can disable their controllers."
        );
        Check(
            CalledMethods(clone.GetMethod("Detach", all)).Any(m => m.Name == "SetParent"),
            "Controllers are not preserved outside destroyed mounts."
        );
        Check(
            CalledMethods(clone.GetMethod("Destroy", all)).Any(m => m.Name == "Detach"),
            "Clone destruction loses controllers."
        );
        Check(
            CalledMethods(owned.GetMethod("Dispose", all)).Any(m => m.Name == "Remove"),
            "Disposal bypasses group claim cleanup."
        );
        var adopt = owned.GetMethod("Adopt", all);
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
                    .Any(m => m.DeclaringType == owned && m.Name == "Dispose"),
                family + " bypasses control cleanup."
            );
        }
        var stay = assembly.GetType("MoreSailwindSails.Stays.FishermansStay.FishermansStay", true);
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
            "PASS (structural): paired binding, controller-preserving template replacement, native mount references, all ownership paths and generated-search removal. Unity lifecycle remains unexecuted."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
