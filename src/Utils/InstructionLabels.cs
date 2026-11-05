// Bridges the installed Harmony's framework-only Label type to our netstandard2.0 target.
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace MoreSailwindSails.Utils
{
    internal static class InstructionLabels
    {
        // Used only while transpilers run at startup, never during gameplay.
        private static readonly FieldInfo labels = typeof(CodeInstruction).GetField(name: "labels");

        internal static IList Get(CodeInstruction instruction) =>
            (IList)labels.GetValue(obj: instruction);
    }
}
