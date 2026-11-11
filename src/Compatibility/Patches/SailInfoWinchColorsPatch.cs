// Keeps SailInfo's optional loading-time colors from indexing slots managed by our allocator.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MoreSailwindSails.Controls;

namespace MoreSailwindSails.Compatibility.Patches
{
    internal static class SailInfoWinchColorsPatch
    {
        internal static MethodInfo FindTarget(Type type)
        {
            var method = type?.GetMethod(
                name: "UpdateControllerAttachmentsPatch",
                bindingAttr: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(Mast) },
                modifiers: null
            );
            return method != null && method.ReturnType == typeof(void) && !method.IsGenericMethod
                ? method
                : null;
        }

        internal static void Install(Harmony harmony)
        {
            var type = AccessTools.TypeByName(name: "SailInfo.SailInfoPatches");
            if (type == null)
                return;
            try
            {
                var target = FindTarget(type: type);
                if (target == null)
                    throw new InvalidOperationException("Color hook signature changed.");
                harmony.Patch(
                    original: target,
                    transpiler: new HarmonyMethod(
                        methodType: typeof(SailInfoWinchColorsPatch),
                        methodName: nameof(Transpiler)
                    )
                );
            }
            catch (Exception exception)
            {
                Plugin.Log.LogWarning(
                    $"SailInfo winch color integration unavailable: {exception.Message}"
                );
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            var code = instructions.ToList();
            int sail = -1,
                increment = -1;
            for (int i = 0; i < code.Count - 3; i++)
            {
                if (
                    code[i].opcode == OpCodes.Callvirt
                    && code[i].operand is MethodInfo method
                    && method.Name == "GetComponent"
                    && method.DeclaringType == typeof(UnityEngine.GameObject)
                    && method.IsGenericMethod
                    && method.GetGenericArguments().SequenceEqual(new[] { typeof(Sail) })
                    && StoreToLoad(code: code[i + 1]) != null
                )
                {
                    if (sail >= 0)
                        throw new InvalidOperationException("Ambiguous SailInfo sail local.");
                    sail = i + 1;
                }
                if (
                    code[i].opcode == OpCodes.Ldsfld
                    && code[i].operand is FieldInfo field
                    && field.Name == "winchColorIndex"
                    && field.FieldType == typeof(int)
                    && field.DeclaringType.FullName == "SailInfo.SailInfoPatches"
                    && code[i + 1].opcode == OpCodes.Ldc_I4_1
                    && code[i + 2].opcode == OpCodes.Add
                    && code[i + 3].opcode == OpCodes.Stsfld
                    && Equals(code[i + 3].operand, field)
                )
                {
                    if (increment >= 0)
                        throw new InvalidOperationException("Ambiguous SailInfo color increment.");
                    increment = i;
                }
            }
            if (
                sail < 0
                || increment <= sail
                || Utils.InstructionLabels.Get(instruction: code[increment]).Count == 0
            )
                throw new InvalidOperationException("SailInfo color loop changed.");
            var skip = Utils.InstructionLabels.Get(instruction: code[increment])[0];
            var load = StoreToLoad(code: code[sail]);
            var labels = Utils.InstructionLabels.Get(instruction: code[sail + 1]);
            foreach (var label in labels)
                Utils.InstructionLabels.Get(instruction: load).Add(value: label);
            labels.Clear();
            load.blocks.AddRange(collection: code[sail + 1].blocks);
            code[sail + 1].blocks.Clear();
            code.InsertRange(
                index: sail + 1,
                collection: new[]
                {
                    load,
                    new CodeInstruction(
                        opcode: OpCodes.Call,
                        operand: typeof(NativeSailControls).GetMethod(
                            name: nameof(NativeSailControls.Managed),
                            bindingAttr: BindingFlags.NonPublic | BindingFlags.Static
                        )
                    ),
                    new CodeInstruction(opcode: OpCodes.Brtrue, operand: skip),
                }
            );
            return code;
        }

        private static CodeInstruction StoreToLoad(CodeInstruction code)
        {
            if (code.opcode == OpCodes.Stloc || code.opcode == OpCodes.Stloc_S)
                return new CodeInstruction(opcode: OpCodes.Ldloc, operand: code.operand);
            if (code.opcode == OpCodes.Stloc_0)
                return new CodeInstruction(opcode: OpCodes.Ldloc_0);
            if (code.opcode == OpCodes.Stloc_1)
                return new CodeInstruction(opcode: OpCodes.Ldloc_1);
            if (code.opcode == OpCodes.Stloc_2)
                return new CodeInstruction(opcode: OpCodes.Ldloc_2);
            if (code.opcode == OpCodes.Stloc_3)
                return new CodeInstruction(opcode: OpCodes.Ldloc_3);
            return null;
        }
    }
}
