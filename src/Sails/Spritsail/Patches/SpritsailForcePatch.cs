using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Scales only the propulsion-local power, retaining native force application and reporting.
    [HarmonyPatch(typeof(Sail), "ApplyForce")]
    internal static class SpritsailForcePatch
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            var codes = instructions.ToList();
            var target = typeof(Sail).GetMethod(name: "GetRealSailPower");
            if (codes.Count(c => c.Calls(method: target)) != 1)
                throw new InvalidOperationException(
                    message: "Sail.ApplyForce power calculation changed; review spritsail propulsion integration."
                );
            foreach (var code in codes)
            {
                yield return code;
                if (code.Calls(method: target))
                {
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(
                        opcode: OpCodes.Call,
                        operand: typeof(SpritsailCategory).GetMethod(
                            name: nameof(SpritsailCategory.ScalePropulsion),
                            bindingAttr: BindingFlags.Static | BindingFlags.NonPublic
                        )
                    );
                }
            }
        }
    }
}
