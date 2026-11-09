// Adapts SE's saved flip toggle to the spritsail's mirrored fitting frame.
using System;
using System.Reflection;
using HarmonyLib;
using MoreSailwindSails.Sails.LooseFootedSpritsail;
using UnityEngine;

namespace MoreSailwindSails.Compatibility.Patches
{
    internal static class ShipyardExpansionSpritsailFlipPatch
    {
        internal static void Install(Harmony harmony)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type =
                    assembly.GetType(name: "ShipyardExpansion.SailScaler")
                    ?? assembly.GetType(name: "ShipyardExpansion.Scripts.SailScaler");
                if (type == null)
                    continue;
                var awake = AccessTools.Method(type: type, name: "Awake");
                var angle = AccessTools.Method(
                    type: type,
                    name: "SetAngle",
                    parameters: new[] { typeof(float) }
                );
                var flip = AccessTools.Method(
                    type: type,
                    name: "FlipJib",
                    parameters: new[] { typeof(bool) }
                );
                var setter = AccessTools.PropertySetter(type: type, name: "Flipped");
                var flag = AccessTools.Field(type: type, name: "flippable");
                if (
                    awake == null
                    || angle == null
                    || flip == null
                    || setter == null
                    || flag?.FieldType != typeof(bool)
                )
                {
                    Plugin.Log?.LogWarning(
                        "SE spritsail flipping unavailable: incompatible SailScaler API."
                    );
                    return;
                }
                flippedSetter = setter;
                flippable = flag;
                var postfix = new HarmonyMethod(
                    typeof(ShipyardExpansionSpritsailFlipPatch),
                    nameof(EnableFlip)
                );
                harmony.Patch(original: awake, postfix: postfix);
                harmony.Patch(original: angle, postfix: postfix);
                harmony.Patch(
                    original: flip,
                    prefix: new HarmonyMethod(
                        typeof(ShipyardExpansionSpritsailFlipPatch),
                        nameof(Flip)
                    )
                );
                return;
            }
        }

        private static MethodInfo flippedSetter;
        private static FieldInfo flippable;

        private static void EnableFlip(Component __instance)
        {
            if (__instance.GetComponent<LooseFootedSpritsailRig>())
                flippable.SetValue(obj: __instance, value: true);
        }

        private static bool Flip(Component __instance, bool __0)
        {
            var rig = __instance.GetComponent<LooseFootedSpritsailRig>();
            if (!rig)
                return true;
            flippedSetter.Invoke(obj: __instance, parameters: new object[] { __0 });
            rig.SetFlipped(flipped: __0);
            return false;
        }
    }
}
