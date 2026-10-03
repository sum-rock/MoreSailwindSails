using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace MoreSailwindSails.Sails.Spritsail.Patches
{
    // Extends only the native gaff/square overlap exception to spritsails in either order.
    [HarmonyPatch(typeof(ShipyardSailInstaller), "CheckSailOverlap")]
    internal static class SpritsailOverlapPatch
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            var codes = instructions.ToList();
            var field = typeof(Sail).GetField(name: nameof(Sail.category));
            if (codes.Count(c => c.LoadsField(field: field)) != 4)
                throw new InvalidOperationException(
                    message: "Shipyard overlap rules changed; review the spritsail exception."
                );
            foreach (var code in codes)
            {
                if (code.LoadsField(field: field))
                {
                    // Preserve branch labels and exception blocks on the replaced instruction.
                    code.opcode = OpCodes.Call;
                    code.operand = typeof(SpritsailCategory).GetMethod(
                        name: nameof(SpritsailCategory.OverlapCategory),
                        bindingAttr: BindingFlags.Static | BindingFlags.NonPublic
                    );
                }
                yield return code;
            }
        }
    }
}
