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
                InstallButtons(harmony: harmony, assembly: assembly);
                return;
            }
        }

        private static void InstallButtons(Harmony harmony, Assembly assembly)
        {
            var ui = assembly.GetType(name: "ShipyardExpansion.Patches.ShipyardUIPatches");
            if (ui != null)
            {
                rotateForward = AccessTools.Field(type: ui, name: "rotateForwardButton");
                rotateBackward = AccessTools.Field(type: ui, name: "rotateBackwardButton");
            }
            if (
                rotateForward?.FieldType != typeof(GameObject)
                || rotateBackward?.FieldType != typeof(GameObject)
            )
            {
                Plugin.Log?.LogWarning(
                    "SE spritsail tilt buttons could not be hidden: incompatible UI API."
                );
                return;
            }
            harmony.Patch(
                original: AccessTools.Method(type: typeof(ShipyardUI), name: "UpdateMoveButtons"),
                postfix: new HarmonyMethod(
                    typeof(ShipyardExpansionSpritsailFlipPatch),
                    nameof(UpdateButtons)
                )
                {
                    after = new[] { "com.nandbrew.shipyardexpansion" },
                }
            );
        }

        private static FieldInfo rotateForward;
        private static FieldInfo rotateBackward;

        // SE reestablishes normal visibility on every update, including selection changes.
        private static void UpdateButtons()
        {
            if (!GameState.currentShipyard)
                return;
            var sail = GameState.currentShipyard.sailInstaller.GetCurrentSail();
            if (!sail || !sail.GetComponent<LooseFootedSpritsailRig>())
                return;
            var forward = rotateForward.GetValue(obj: null) as GameObject;
            var backward = rotateBackward.GetValue(obj: null) as GameObject;
            if (forward)
                forward.SetActive(value: false);
            if (backward)
                backward.SetActive(value: false);
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
