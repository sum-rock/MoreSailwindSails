// Exercises binding transpilers against installed IL and checks native controller ownership.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;
using static MoreSailwindSails.Tests.AssemblyChecks.Shared.IlReader;

namespace MoreSailwindSails.Tests.AssemblyChecks.Controls;

internal static class NativeSailControlChecks
{
    private const BindingFlags All =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    internal static void Run(Assembly assembly, string gameDir)
    {
        Type Find(string name) => assembly.GetType("MoreSailwindSails." + name, true);
        var helper = Find("Controls.NativeSailControls");
        var marker = Find("Controls.INativeSailControlRig");
        foreach (string family in new[] { "FishermansFlyingSail", "LooseFootedSpritsail" })
        {
            var rig = Find($"Sails.{family}.{family}Rig");
            Check(
                marker.IsAssignableFrom(rig),
                "Native handoff marker must exist on both prefab rigs."
            );
        }
        var sprit = Find("Sails.LooseFootedSpritsail.LooseFootedSpritsailRigging");
        foreach (
            var role in new[]
            {
                ("Left", "angleControllerLeft"),
                ("Right", "angleControllerRight"),
                ("Reef", "reefController"),
            }
        )
        {
            Check(
                sprit.GetField(role.Item1, All) == null,
                "Duplicate serialized controller ownership: " + role.Item1
            );
            Check(
                Instructions(sprit.GetProperty(role.Item1, All).GetMethod)
                    .Any(i => i.Operand is FieldInfo f && f.Name == role.Item2),
                "Controller access must read SailConnections."
            );
        }
        Check(
            assembly.GetType("MoreSailwindSails.Compatibility.SailInfoControls") == null,
            "Manual SailInfo Awake bridge must be removed."
        );
        foreach (var method in helper.GetMethods(All))
            Check(
                !Instructions(method)
                    .Any(i =>
                        i.Code == OpCodes.Stfld
                        && i.Operand is FieldInfo f
                        && f.DeclaringType.Name is "SailConnections" or "Mast"
                    ),
                "Handoff must preserve native sail references and mast arrays."
            );

        var patch = Find("Controls.Patches.NativeSailControlsPatch");
        var target = patch
            .GetCustomAttribute<HarmonyPatch>()
            .info.declaringType.GetMethod("UpdateControllerAttachments");
        var transpiler = patch.GetMethod("Transpiler", All);
        var generator = Generator();
        var original = PatchInstructions.Read(target, generator);
        var changed = Transform(transpiler, original);
        Check(
            changed.Count == original.Count - 13,
            "Only reef and indexed sheet attachment blocks should change."
        );
        Check(
            Calls(changed, helper, "BindReef") == 1 && Calls(changed, helper, "BindSheets") == 1,
            "Both indexed binding guards must be installed once."
        );
        Check(
            changed.Count(i => i.operand is MethodInfo m && m.Name == "AttachToController") == 3,
            "Native middle sheet and square-sail bindings must remain unchanged."
        );
        Check(
            Instructions(target).Any(i => i.Operand is MethodInfo m && m.Name == "UpdateSailOrder"),
            "Native sail ordering must remain intact."
        );
        CheckPreserved(original, changed);
        foreach (
            var instruction in changed.Where(i =>
                i.operand is MethodInfo m && m.DeclaringType == helper
            )
        )
        {
            int index = changed.IndexOf(instruction);
            Check(
                changed[index - 3].opcode == OpCodes.Ldarg_0
                    && ((LocalBuilder)changed[index - 2].operand).LocalType == typeof(int)
                    && changed[index - 1].opcode == OpCodes.Ldloc_3
                    && target.GetMethodBody().LocalVariables[3].LocalType.Name == "SailConnections",
                "Guard arguments must preserve the native mast, index and connections."
            );
        }
        Reject(
            transpiler,
            original.Where(i => i.operand is not FieldInfo f || f.Name != "reefWinch").ToList()
        );
        int guard = changed.FindIndex(i => i.operand is MethodInfo m && m.Name == "BindReef");
        Check(guard > 0, "Missing guarded reef call.");
        int reefStart =
            original.FindIndex(i => i.operand is FieldInfo f && f.Name == "reefWinch") - 1;
        Reject(transpiler, original.Concat(original.Skip(reefStart).Take(7)).ToList());
        var insideBranch = PatchInstructions.Read(target, Generator());
        insideBranch[reefStart + 3].labels.Add(Generator().DefineLabel());
        Reject(transpiler, insideBranch);

        var available = helper.GetMethod("Available", All);
        var winch = available.GetParameters()[0].ParameterType.GetElementType();
        bool Available(Array array, int index) =>
            (bool)available.Invoke(null, new object[] { array, index, null });
        Check(
            !Available(null, 0)
                && !Available(Array.CreateInstance(winch, 0), 0)
                && !Available(Array.CreateInstance(winch, 1), -1)
                && !Available(Array.CreateInstance(winch, 1), 1),
            "Missing, negative and out-of-range slots must be rejected before touching Unity objects."
        );
        var availability = CalledMethods(available).ToArray();
        Check(
            availability.Any(m => m.Name == "Usable")
                && availability.Any(m => m.Name == "op_Equality"),
            "Indexed bindings must check usable controls and controller ownership."
        );
        var sheets = CalledMethods(helper.GetMethod("BindSheets", All)).ToArray();
        Check(
            sheets.Count(m => m.Name == "Available") == 2
                && Array.FindLastIndex(sheets, m => m.Name == "Available")
                    < Array.FindIndex(sheets, m => m.Name == "AttachToController"),
            "Both sheet slots must be validated before either controller is attached."
        );
        Check(
            CalledMethods(helper.GetMethod("BindReef", All)).Any(m => m.Name == "Managed")
                && sheets.Any(m => m.Name == "Managed"),
            "Availability restrictions must be limited to participating sails."
        );

        CheckSailInfo(assembly, gameDir, helper);
        Console.WriteLine(
            "PASS: installed native binding transpiler, preserved square/middle bindings and branch metadata, malformed IL rejection, missing slots, atomic sheet guards, retained spritsail connections and shared handoff."
        );
    }

    private static void CheckSailInfo(Assembly assembly, string gameDir, Type helper)
    {
        var patch = assembly.GetType(
            "MoreSailwindSails.Compatibility.Patches.SailInfoWinchColorsPatch",
            true
        );
        var find = patch.GetMethod("FindTarget", All);
        Check(
            find.Invoke(null, new object[] { null }) == null
                && find.Invoke(null, new object[] { typeof(string) }) == null,
            "Missing SailInfo must remain optional."
        );
        Check(
            !assembly.GetReferencedAssemblies().Any(a => a.Name == "SailInfo"),
            "SailInfo must not become a hard dependency."
        );
        var path = Path.Combine(gameDir, "BepInEx/plugins/SailInfo.dll");
        if (!File.Exists(path))
        {
            Console.WriteLine(
                "SKIP: installed SailInfo color and HUD checks (optional DLL absent)."
            );
            return;
        }
        var installed = Assembly.LoadFrom(path);
        var type = installed.GetType("SailInfo.SailInfoPatches", true);
        var target = (MethodInfo)find.Invoke(null, new object[] { type });
        Check(target != null, "Installed SailInfo coloring signature changed.");
        var generator = Generator();
        var original = PatchInstructions.Read(target, generator);
        // Snapshot metadata because the transpiler moves labels onto its new guard.
        var labels = original.SelectMany(i => i.labels).ToArray();
        var blocks = original.SelectMany(i => i.blocks).ToArray();
        var transpiler = patch.GetMethod("Transpiler", All);
        var changed = Transform(transpiler, original);
        Check(
            changed.Count == original.Count + 3 && Calls(changed, helper, "Managed") == 1,
            "Color guard must add only a per-sail skip."
        );
        int guard = changed.FindIndex(i => i.operand is MethodInfo m && m.DeclaringType == helper);
        var destination = (Label)changed[guard + 1].operand;
        var increment = changed.Single(i => i.labels.Contains(destination));
        int incrementIndex = changed.IndexOf(increment);
        Check(
            increment.opcode == OpCodes.Ldsfld
                && ((FieldInfo)increment.operand).Name == "winchColorIndex"
                && changed[incrementIndex + 1].opcode == OpCodes.Ldc_I4_1
                && changed[incrementIndex + 2].opcode == OpCodes.Add
                && changed[incrementIndex + 3].opcode == OpCodes.Stsfld,
            "Managed sails must still advance the color sequence for subsequent sails."
        );
        Check(
            original.All(changed.Contains)
                && labels.All(l => changed.Count(i => i.labels.Contains(l)) == 1)
                && blocks.SequenceEqual(changed.SelectMany(i => i.blocks)),
            "Color guard must preserve original instructions, labels and exception blocks."
        );
        Check(
            Calls(changed, type, "WinchColor") == Calls(original, type, "WinchColor"),
            "Native sail coloring must remain intact."
        );
        Reject(transpiler, original.Where(i => i.opcode != OpCodes.Stsfld).ToList());

        var awake = type.GetMethod("AwakePatch", All);
        var hud = installed.GetType("SailInfo.WinchInfoSail", true);
        foreach (string call in new[] { "GetComponent", "AddComponent" })
            Check(
                CalledMethods(awake)
                    .OfType<MethodInfo>()
                    .Count(m =>
                        m.Name == call
                        && m.IsGenericMethod
                        && m.GetGenericArguments().Single() == hud
                    ) == 4,
                "SailInfo must discover HUDs through native controller references."
            );
        Check(
            Instructions(awake)
                .Count(i =>
                    i.Code == OpCodes.Stfld
                    && i.Operand is FieldInfo f
                    && f.DeclaringType == hud
                    && f.Name == "sailComponent"
                ) == 4,
            "SailInfo must associate HUDs with the owning sail."
        );
        Console.WriteLine(
            "PASS: installed SailInfo coloring transpiler, preserved color sequence/branches, optional dependency and native HUD creation contracts."
        );
    }

    private static ILGenerator Generator() =>
        new DynamicMethod("PatchCheck", typeof(void), Type.EmptyTypes).GetILGenerator();

    private static List<CodeInstruction> Transform(MethodInfo method, List<CodeInstruction> code) =>
        ((IEnumerable<CodeInstruction>)method.Invoke(null, new object[] { code })).ToList();

    private static int Calls(List<CodeInstruction> code, Type type, string name) =>
        code.Count(i => i.operand is MethodInfo m && m.DeclaringType == type && m.Name == name);

    private static void Reject(MethodInfo transpiler, List<CodeInstruction> code)
    {
        try
        {
            Transform(transpiler, code);
        }
        catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException)
        {
            return;
        }
        throw new Exception("Changed or ambiguous installed IL must be rejected.");
    }

    private static void CheckPreserved(
        List<CodeInstruction> original,
        List<CodeInstruction> changed
    )
    {
        Check(
            original
                .SelectMany(i => i.labels)
                .OrderBy(l => l.GetHashCode())
                .SequenceEqual(changed.SelectMany(i => i.labels).OrderBy(l => l.GetHashCode())),
            "Binding guards must retain every branch destination."
        );
        Check(
            original.SelectMany(i => i.blocks).SequenceEqual(changed.SelectMany(i => i.blocks)),
            "Binding guards must retain exception boundaries."
        );
        var labels = changed.SelectMany(i => i.labels).ToHashSet();
        Check(
            changed.Where(i => i.operand is Label).All(i => labels.Contains((Label)i.operand)),
            "Transpiler emitted a dangling branch."
        );
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
