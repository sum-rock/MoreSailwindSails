// Groups native Sailwind patches for managed control handoff and inventory invalidation.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Controls.Patches
{
    // Guard only the installed game's indexed reef and non-square sheet binding blocks.
    [HarmonyPatch(typeof(Mast), "UpdateControllerAttachments")]
    internal static class NativeSailControlsPatch
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            var code = instructions.ToList();
            int reef = -1,
                sheets = -1;
            for (int i = 0; i < code.Count; i++)
            {
                if (Matches(code: code, start: i, winch: "reefWinch", controller: "reefController"))
                {
                    if (reef >= 0)
                        throw new InvalidOperationException("Ambiguous native reef binding.");
                    reef = i;
                }
                if (
                    Matches(
                        code: code,
                        start: i,
                        winch: "leftAngleWinch",
                        controller: "angleControllerLeft"
                    )
                    && Matches(
                        code: code,
                        start: i + 7,
                        winch: "rightAngleWinch",
                        controller: "angleControllerRight"
                    )
                    && Same(a: code[i + 2], b: code[i + 9])
                    && Same(a: code[i + 4], b: code[i + 11])
                )
                {
                    if (sheets >= 0)
                        throw new InvalidOperationException("Ambiguous native sheet binding.");
                    sheets = i;
                }
            }
            if (reef < 0 || sheets <= reef)
                throw new InvalidOperationException(
                    "Native mast binding changed; cannot safely guard managed sails."
                );
            Replace(
                code: code,
                start: sheets,
                count: 14,
                method: nameof(NativeSailControls.BindSheets)
            );
            Replace(code: code, start: reef, count: 7, method: nameof(NativeSailControls.BindReef));
            return code;
        }

        private static bool Matches(
            List<CodeInstruction> code,
            int start,
            string winch,
            string controller
        ) =>
            start + 7 <= code.Count
            && code[start].opcode == OpCodes.Ldarg_0
            && code[start + 1].opcode == OpCodes.Ldfld
            && Equals(code[start + 1].operand, typeof(Mast).GetField(name: winch))
            && IsLocal(code: code[start + 2])
            && code[start + 3].opcode == OpCodes.Ldelem_Ref
            && IsLocal(code: code[start + 4])
            && code[start + 5].opcode == OpCodes.Ldfld
            && Equals(code[start + 5].operand, typeof(SailConnections).GetField(name: controller))
            && code[start + 6].opcode == OpCodes.Callvirt
            && Equals(
                code[start + 6].operand,
                typeof(GPButtonRopeWinch).GetMethod(name: "AttachToController")
            );

        private static bool IsLocal(CodeInstruction code) =>
            code.opcode == OpCodes.Ldloc
            || code.opcode == OpCodes.Ldloc_S
            || code.opcode == OpCodes.Ldloc_0
            || code.opcode == OpCodes.Ldloc_1
            || code.opcode == OpCodes.Ldloc_2
            || code.opcode == OpCodes.Ldloc_3;

        private static bool Same(CodeInstruction a, CodeInstruction b) =>
            a.opcode == b.opcode && Equals(a.operand, b.operand);

        private static void Replace(List<CodeInstruction> code, int start, int count, string method)
        {
            if (
                code.Skip(start + 1)
                    .Take(count - 1)
                    .Any(i =>
                        Utils.InstructionLabels.Get(instruction: i).Count != 0
                        || i.blocks.Count != 0
                    )
            )
                throw new InvalidOperationException(
                    "Unexpected branch inside native winch binding."
                );
            var replacement = new[]
            {
                code[start],
                code[start + 2],
                code[start + 4],
                new CodeInstruction(
                    opcode: OpCodes.Call,
                    operand: typeof(NativeSailControls).GetMethod(
                        name: method,
                        bindingAttr: BindingFlags.Static | BindingFlags.NonPublic
                    )
                ),
            };
            code.RemoveRange(index: start, count: count);
            code.InsertRange(index: start, collection: replacement);
        }

        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(Mast __instance)
        {
            // Release every managed binding before any consumer can allocate a seat.
            foreach (var item in __instance.sails)
                if (item && NativeSailControls.Managed(sail: item.transform))
                    NativeSailControls.ReleaseNativeBindings(
                        mast: __instance,
                        connections: item.GetComponent<SailConnections>()
                    );
            foreach (var item in __instance.sails)
                if (item)
                    item.GetComponent<INativeSailControlRig>()?.AttachControls();
        }
    }

    // Installed native callbacks invalidate discovery; the one-second sweep also
    // catches delayed/inactive SE parts that have not run their Unity callbacks.
    [HarmonyPatch]
    internal static class WinchInventoryInvalidationPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            yield return typeof(Mast).GetMethod(name: "Awake", bindingAttr: flags);
            yield return typeof(Mast).GetMethod(name: "OnEnable", bindingAttr: flags);
            yield return typeof(Mast).GetMethod(name: "OnDisable", bindingAttr: flags);
            yield return typeof(Mast).GetMethod(
                name: "UpdateControllerAttachments",
                bindingAttr: flags
            );
            yield return typeof(BoatCustomParts).GetMethod(
                name: "RefreshParts",
                bindingAttr: flags
            );
            yield return typeof(BoatCustomParts).GetMethod(
                name: "RefreshPartsWithOrder",
                bindingAttr: flags
            );
            yield return typeof(SaveableBoatCustomization).GetMethod(
                name: "Awake",
                bindingAttr: flags
            );
        }

        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(Component __instance) =>
            WinchControls.InvalidateInventory(component: __instance);
    }
}
