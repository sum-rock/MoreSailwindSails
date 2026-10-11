using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Spritsail;

// Applies the diagnostic transform to installed IL and verifies its visual-only branch boundaries.
internal static class HiddenNativeRopeChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags flags =
            BindingFlags.Static
            | BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;
        const string prefix = "MoreSailwindSails.Sails.Spritsail.";
        var patch = assembly.GetType(
            name: prefix + "Patches.HiddenNativeRopeVisualsPatch",
            throwOnError: true
        );
        var transpiler = patch.GetMethod(name: "Transpiler", bindingAttr: flags);
        var native = patch.GetCustomAttribute<HarmonyPatch>().info.declaringType;
        var generator = new DynamicMethod(
            name: "HiddenRopeBranchLabels",
            returnType: typeof(void),
            parameterTypes: Type.EmptyTypes
        ).GetILGenerator();
        var original = NativeInstructions(
            method: native.GetMethod(name: "LateUpdate", bindingAttr: flags),
            generator: generator
        );
        CodeInstruction[] Transform(CodeInstruction[] input) =>
            (
                (IEnumerable<CodeInstruction>)
                    transpiler.Invoke(obj: null, parameters: new object[] { input })
            ).ToArray();
        int display = Array.FindIndex(
            original,
            c => c.operand is MethodInfo m && m.Name == "DisplayRope"
        );
        var incoming = generator.DefineLabel();
        original[display - 1].labels.Add(item: incoming);
        var transformed = Transform(input: original);
        var helper = assembly
            .GetType(name: prefix + "HiddenNativeRopeVisuals", throwOnError: true)
            .GetMethod(name: "ShouldSkip", bindingAttr: flags);
        int gate = Array.FindIndex(transformed, c => c.Calls(method: helper));
        Require(
            value: transformed.Length == original.Length + 3
                && gate == display
                && transformed[gate - 1].opcode == OpCodes.Ldarg_0
                && transformed[gate - 2].operand is MethodInfo tension
                && tension.Name == "UpdateTension"
                && transformed[gate + 1].opcode == OpCodes.Brtrue,
            message: "The bypass gate must run after mechanics and before native visual work."
        );
        var destination = (Label)transformed[gate + 1].operand;
        int cleanup = Array.FindIndex(transformed, c => c.labels.Contains(destination));
        int cloth = Array.FindIndex(
            transformed,
            c => c.operand is MethodInfo m && m.Name == "UpdateRope"
        );
        Require(
            value: cleanup > cloth
                && transformed[cleanup].opcode == OpCodes.Ldsfld
                && transformed[cleanup].operand is FieldInfo field
                && field.Name == "clothRopes"
                && transformed
                    .Skip(cleanup)
                    .Any(c => c.operand is MethodInfo m && m.Name == "Destroy"),
            message: "Bypass must skip line generation and 3D creation/update, retaining settings cleanup."
        );
        var retained = transformed.Where((_, i) => i < gate - 1 || i > gate + 1).ToArray();
        Require(
            value: retained
                .Zip(original)
                .All(pair =>
                    pair.First.opcode == pair.Second.opcode
                    && Equals(pair.First.operand, pair.Second.operand)
                )
                && transformed[gate - 1].labels.Contains(incoming)
                && !transformed[gate + 2].labels.Contains(incoming)
                && original[display - 1].labels.Contains(incoming),
            message: "Keep every native instruction and move incoming visual-block labels onto the gate without mutating input."
        );
        void Reject(CodeInstruction[] input)
        {
            try
            {
                Transform(input: input);
            }
            catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException)
            {
                return;
            }
            throw new InvalidOperationException(
                message: "Changed native IL must reject the diagnostic transform."
            );
        }
        Reject(input: Array.Empty<CodeInstruction>());
        Reject(input: original.Concat(original).ToArray());
        Reject(
            input: original
                .Where(c => !(c.operand is MethodInfo m && m.Name == "UpdateTension"))
                .ToArray()
        );
        var changed = original.Select(c => new CodeInstruction(c)).ToArray();
        changed[display - 1].opcode = OpCodes.Nop;
        Reject(input: changed);

        var calls = IlReader.CalledMethods(method: helper).OfType<MethodInfo>().ToArray();
        var markers = calls
            .Where(m => m.Name == "GetComponent" && m.IsGenericMethod)
            .Select(m => m.GetGenericArguments().Single().Name)
            .OrderBy(n => n)
            .ToArray();
        Require(
            value: markers.SequenceEqual(
                new[]
                {
                    "BoomedSpritsailReplacedRopeVisual",
                    "LooseFootedSpritsailNativeSheetVisual",
                }
            )
                && calls.All(m =>
                    m.Name
                        is "GetComponent"
                            or "op_Implicit"
                            or "IsBypassed"
                            or "Selected"
                            or "Count"
                ),
            message: "Only explicitly hidden spritsail ropes qualify; the gate must not mutate mechanics or renderer state."
        );
        foreach (
            string type in new[]
            {
                "LooseFootedSpritsail.Patches.LooseFootedSpritsailSheetVisualPatch",
                "BoomedSpritsail.Patches.BoomedSpritsailReplacedRopeVisualPatch",
            }
        )
        {
            var postfix = assembly
                .GetType(name: prefix + type, throwOnError: true)
                .GetMethod(name: "Postfix", bindingAttr: flags);
            Require(
                value: postfix.IsDefined(attributeType: typeof(HarmonyPostfix))
                    && IlReader.CalledMethods(method: postfix).Any(m => m.Name == "Suppress"),
                message: "Existing postfixes must still hide the replaced native visuals in every mode."
            );
        }
        Console.WriteLine(
            "PASS (structural): hidden native rope bypass preserves mechanics, cleanup, original instructions and suppression; changed IL rejected. Live Unity execution remains untested."
        );
    }

    // Retain real branch destinations without asking Harmony to create native Unity patch stubs.
    private static CodeInstruction[] NativeInstructions(MethodInfo method, ILGenerator generator)
    {
        var codes = IlReader
            .Instructions(method: method)
            .Select(i => new CodeInstruction(opcode: i.Code, operand: i.Operand))
            .ToArray();
        var bytes = method.GetMethodBody().GetILAsByteArray();
        var offsets = new int[codes.Length];
        var branches = new List<(int Index, int Target)>();
        int offset = 0;
        for (int i = 0; i < codes.Length; i++)
        {
            offsets[i] = offset;
            offset += codes[i].opcode.Size;
            var operand = codes[i].opcode.OperandType;
            int size = operand switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget
                or OperandType.ShortInlineI
                or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => throw new InvalidOperationException(
                    "Unexpected switch in native rope method."
                ),
                _ => 4,
            };
            if (operand == OperandType.ShortInlineBrTarget)
                branches.Add((i, offset + size + unchecked((sbyte)bytes[offset])));
            else if (operand == OperandType.InlineBrTarget)
                branches.Add((i, offset + size + BitConverter.ToInt32(bytes, offset)));
            offset += size;
        }
        Require(
            value: offset == bytes.Length,
            message: "Native rope IL decoding must cover the entire method."
        );
        var labels = new Dictionary<int, Label>();
        foreach (var branch in branches)
        {
            int target = Array.IndexOf(offsets, branch.Target);
            Require(
                value: target >= 0,
                message: "Every native branch must target an instruction boundary."
            );
            if (!labels.TryGetValue(branch.Target, out var label))
            {
                label = generator.DefineLabel();
                labels.Add(branch.Target, label);
                codes[target].labels.Add(label);
            }
            codes[branch.Index].operand = label;
        }
        return codes;
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
