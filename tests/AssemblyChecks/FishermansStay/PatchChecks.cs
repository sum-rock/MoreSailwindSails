using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.FishermansStay;

internal static class PatchChecks
{
    internal static void Run(Assembly assembly)
    {
        // Stay integration uses native save slots; inspect and exercise the pure parts
        // without constructing Unity objects or mutating an installed save.
        const string stayNamespace = "MoreSailwindSails.Stays.FishermansStay.";
        Type StayPatch(string name) =>
            assembly.GetType(stayNamespace + "Patches.FishermansStay" + name + "Patch", true);
        MethodInfo StayMethod(string name, string method) =>
            StayPatch(name)
                .GetMethod(
                    method,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                );
        var anchor = StayMethod("Anchor", "Postfix");
        var anchorCalls = CalledMethods(anchor).ToArray();
        if (
            !anchorCalls.Any(m => m.Name == "TryFind")
            || !anchorCalls.Any(m => m.Name == "InverseTransformPoint")
            || !anchorCalls.Any(m => m.Name == "set_connectedAnchor")
            || anchorCalls.Any(m => m.Name == "get_localPosition")
        )
            throw new Exception(
                "Stay attachment must scope hinge correction to registered stays and use rigidbody coordinates."
            );
        var stay = assembly.GetType(stayNamespace + "FishermansStay", throwOnError: true);
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
        var create = stay.GetMethod("Create", instance);
        if (
            !Instructions(create).Any(i => i.Operand is FieldInfo f && f.Name == "boatModel")
            || !CalledMethods(create).Any(m => m.Name == "SetParent")
            // Only the walking frame's scale should be updated during placement.
            || CalledMethods(stay.GetMethod("Place", instance))
                .Count(m => m.Name == "set_localScale") != 1
        )
            throw new Exception(
                "Stay mounts must join the boat model without resetting their preserved scale."
            );
        foreach (string name in new[] { "Registration", "SaveCapacity", "MountButtons" })
            if (
                !StayMethod(name, "Postfix")
                    .GetCustomAttribute<HarmonyAfter>()
                    .info.after.Contains("com.nandbrew.shipyardexpansion")
            )
                throw new Exception("Stay integration must run after SE: " + name);
        if (
            !StayMethod("Load", "Prefix")
                .GetCustomAttribute<HarmonyBefore>()
                .info.before.Contains("com.nandbrew.shipyardexpansion")
        )
            throw new Exception("Stay snapshot preparation must precede SE load handling.");
        var capacity = StayMethod("SaveCapacity", "Postfix");
        var saveType = capacity.GetParameters()[0].ParameterType;
        var save = Activator.CreateInstance(saveType);
        var mastField = saveType.GetField("masts");
        foreach (int originalCapacity in new[] { 48, 128 })
        {
            var oldSlots = new bool[originalCapacity];
            oldSlots[5] = true;
            mastField.SetValue(save, oldSlots);
            capacity.Invoke(null, new[] { save });
            var extended = (bool[])mastField.GetValue(save);
            if (
                extended.Length != 256
                || !extended[5]
                || extended.Skip(originalCapacity).Any(x => x)
            )
                throw new Exception(
                    "Stay save capacity lost old state or defaulted a new slot on."
                );
        }
        var larger = new bool[512];
        larger[400] = true;
        mastField.SetValue(save, larger);
        capacity.Invoke(null, new[] { save });
        if (!ReferenceEquals(larger, mastField.GetValue(save)))
            throw new Exception("Stay capacity must preserve other mods' larger arrays.");
        if (!StayMethod("OrderRefresh", "Finalizer").IsDefined(typeof(HarmonyFinalizer)))
            throw new Exception("Stay preview state must be restored when native refresh throws.");
        if (!CalledMethods(StayMethod("OccupiedPreview", "Prefix")).Any(m => m.Name == "Protects"))
            throw new Exception("Stay previews must protect occupied mounts and their supports.");
        var loadCalls = CalledMethods(StayMethod("Load", "Prefix")).ToArray();
        if (
            !loadCalls.Any(m => m.DeclaringType == typeof(Array) && m.Name == "Resize")
            || !loadCalls.Any(m => m.Name == "PrepareSnapshot")
        )
            throw new Exception(
                "Stay load handling must prepare old snapshots before native sail removal."
            );
        var install = StayMethod("CanInstall", "Postfix");
        if (
            !CalledMethods(install).Any(m => m.Name == "Concat")
            || !CalledMethods(install).Any(m => m.Name == "ConflictText")
        )
            throw new Exception("Stay install conflicts lost reciprocal named help text.");
        foreach (
            string conflict in new[]
            {
                "Fisherman's Stay (Foremast Top / Mainmast Top)",
                "Fisherman's Stay (Mainmast Top / Mizzenmast Top)",
            }
        )
            if (
                (string)
                    StayMethod("CanInstall", "ConflictText")
                        .Invoke(null, new object[] { new[] { conflict } })
                != "<color=red>incompatible with: " + conflict + ".</color>"
            )
                throw new Exception("Stay conflict help must name the opposing option in red.");
        var updateOrder = saveType
            .Assembly.GetType(name: "ShipyardPartsInstaller", throwOnError: true)
            .GetMethod(
                "UpdateOrder",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
        var updateCalls = CalledMethods(updateOrder).ToArray();
        foreach (
            string method in new[]
            {
                "CanInstall",
                "get_red",
                "ChangePartOptionTextColor",
                "SetPartErrorText",
            }
        )
            if (!updateCalls.Any(m => m.Name == method))
                throw new Exception(
                    "Native order validation no longer displays install errors: " + method
                );
        var errorText = saveType
            .Assembly.GetType(name: "ShipyardUI", throwOnError: true)
            .GetMethod(
                "SetPartErrorText",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
        if (
            !CalledMethods(errorText)
                .Any(m => m.DeclaringType.Name == "MouseoverTextTrigger" && m.Name == "SetText")
        )
            throw new Exception("Shipyard install errors no longer reach hover help.");
        Console.WriteLine(
            "PASS: stay registration ordering, save capacity, preview protection, scoped order text and reciprocal red conflict help wired to native shipyard errors (no live UI execution)."
        );
    }
}
