using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Bypasses only the native visual block, after rope mechanics and before settings cleanup.
    [HarmonyPatch(typeof(RopeEffect), "LateUpdate")]
    internal static class HiddenNativeRopeVisualsPatch
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            const BindingFlags all =
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic;
            var codes = instructions.Select(c => new CodeInstruction(c)).ToList();
            int Call(Type type, string name)
            {
                var method = type.GetMethod(name: name, bindingAttr: all);
                if (method == null || codes.Count(c => c.Calls(method: method)) != 1)
                    throw new InvalidOperationException(
                        message: "RopeEffect.LateUpdate changed; review hidden native rope bypass: "
                            + name
                    );
                return codes.FindIndex(c => c.Calls(method: method));
            }

            int attachments = Call(type: typeof(RopeEffect), name: "UpdateAttachments");
            int limits = Call(type: typeof(RopeEffect), name: "ApplyLimits");
            int tension = Call(type: typeof(RopeEffect), name: "UpdateTension");
            int display = Call(type: typeof(RopeEffect), name: "DisplayRope");
            int cloth = Call(type: typeof(ClothRope), name: "UpdateRope");
            var setting = typeof(Settings).GetField(name: "clothRopes", bindingAttr: all);
            int cleanup = codes.FindLastIndex(c => c.LoadsField(field: setting));
            if (
                attachments >= limits
                || limits >= tension
                || display != tension + 2
                || display + 2 >= codes.Count
                || codes[display - 1].opcode != OpCodes.Ldarg_0
                || !codes[display + 1].LoadsField(field: setting)
                || (
                    codes[display + 2].opcode != OpCodes.Brfalse
                    && codes[display + 2].opcode != OpCodes.Brfalse_S
                )
                || codes[display + 2].operand == null
                || cloth <= display
                || cleanup <= cloth
                || cleanup + 1 >= codes.Count
                || (
                    codes[cleanup + 1].opcode != OpCodes.Brtrue
                    && codes[cleanup + 1].opcode != OpCodes.Brtrue_S
                )
                || codes.Any(c => c.blocks.Count != 0)
            )
                throw new InvalidOperationException(
                    message: "RopeEffect.LateUpdate visual/cleanup boundaries changed; review hidden native rope bypass."
                );

            // Reuse the native "3D ropes disabled" branch's destination, preserving cleanup.
            var destination = codes[display + 2].operand;
            var start = new CodeInstruction(opcode: OpCodes.Ldarg_0).MoveLabelsFrom(
                other: codes[display - 1]
            );
            codes.InsertRange(
                index: display - 1,
                collection: new[]
                {
                    start,
                    new CodeInstruction(
                        opcode: OpCodes.Call,
                        operand: typeof(HiddenNativeRopeVisuals).GetMethod(
                            name: nameof(HiddenNativeRopeVisuals.ShouldSkip),
                            bindingAttr: all
                        )
                    ),
                    new CodeInstruction(opcode: OpCodes.Brtrue, operand: destination),
                }
            );
            return codes;
        }
    }
}
