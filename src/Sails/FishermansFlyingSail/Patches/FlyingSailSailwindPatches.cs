// Groups FlyingSail patches for native Sailwind behavior.
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansFlyingSail.Patches
{
    // Refreshes the custom aerodynamic frame before native wind-force calculation.
    [HarmonyPatch(typeof(Sail), "UpdateWindForceOnSail")]
    internal static class FishermansFlyingSailWindFramePatch
    {
        [HarmonyPrefix]
        private static void Prefix(Sail __instance)
        {
            var rig = __instance.GetComponent<FishermansFlyingSailRig>();
            if (rig)
                rig.RefreshAerodynamics();
        }
    }

    // Supplies force direction from the custom sail aerodynamic frame.
    [HarmonyPatch(typeof(Sail), "GetSailForceDirection")]
    internal static class FishermansFlyingSailForceDirectionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail __instance, ref Vector3 __result)
        {
            var rig = __instance.GetComponent<FishermansFlyingSailRig>();
            if (!rig || !rig.RefreshAerodynamics())
                return true;
            __result = FishermansFlyingSailAerodynamics.ForceDirection(
                __instance.apparentWind,
                __instance.windcenter.up
            );
            return false;
        }
    }

    // Aligns the native installation collision sweep with the custom sail frame.
    [HarmonyPatch(typeof(ShipyardSailColChecker), "UpdateRotation")]
    internal static class FishermansFlyingSailCollisionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(
            ShipyardSailColChecker __instance,
            Sail ___sail,
            int ___checkDelay,
            ref UnityEngine.Quaternion ___initialRot
        )
        {
            var rig = ___sail ? ___sail.GetComponent<FishermansFlyingSailRig>() : null;
            if (!rig || !rig.RefreshFlyingFrame())
                return true;
            // The native checker first waits above the ship for contacts to
            // clear. After that, sweep around the same mast axis as the sail.
            if (___checkDelay <= 2)
            {
                // The final step ends the sweep. Return both position and
                // rotation to neutral before native code publishes the result.
                float angle =
                    __instance.currentAngle > __instance.startMaxAngle
                        ? 0
                        : __instance.currentAngle;
                if (!rig.PositionCollisionChecker(__instance.transform, angle, out var neutral))
                    return true;
                // FixedUpdate restores initialRot after the final sweep. Preserve
                // our aligned neutral panel instead of the donor's old axes.
                ___initialRot = neutral;
            }
            return false;
        }
    }

    // Refreshes custom cloth through its rig instead of the native animation path.
    [HarmonyPatch(typeof(ReefEffectAnimUniversal), "RefreshCloth")]
    internal static class FishermansFlyingSailClothRefreshPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ReefEffectAnimUniversal __instance)
        {
            var rig = __instance.GetComponent<FishermansFlyingSailRig>();
            if (!rig)
                return true;
            rig.RefreshCloth();
            return false;
        }
    }

    // Limits native mast selection to valid custom sail supports.
    [HarmonyPatch(typeof(ShipyardUI), "SailMastCompatible")]
    internal static class FishermansFlyingSailMastCompatiblePatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameObject sailPrefab, ref bool __result)
        {
            if (__result && sailPrefab.GetComponent<FishermansFlyingSailRig>())
                __result = FishermansFlyingSailRigging.TryResolve(
                    GameState.currentShipyard.sailInstaller.GetCurrentMast(),
                    out _
                );
        }
    }

    // Reports support-geometry errors through native installation validation.
    [HarmonyPatch(typeof(ShipyardSailInstaller), "GetInstallError")]
    internal static class FishermansFlyingSailInstallErrorPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail sail, Mast mast, ref bool error, ref string __result)
        {
            if (!sail.GetComponent<FishermansFlyingSailRig>())
                return true;
            string reason = FishermansFlyingSailRigging.InstallError(sail, mast);
            if (reason == null)
                return true;
            error = true;
            __result = reason;
            return false;
        }
    }

    // Rejects native installation when custom sail supports are invalid.
    [HarmonyPatch(typeof(ShipyardSailInstaller), "InstallSail")]
    internal static class FishermansFlyingSailInstallGuardPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Mast mast, Sail sail)
        {
            if (!sail.GetComponent<FishermansFlyingSailRig>())
                return true;
            string error = FishermansFlyingSailRigging.InstallError(sail, mast);
            if (error == null)
                return true;
            Plugin.Log.LogWarning("FishermansFlyingSail installation rejected: " + error);
            return false;
        }
    }

    // Reports custom sail height from its posed geometry and scale.
    [HarmonyPatch(typeof(Sail), "GetScaledHeight")]
    internal static class FishermansFlyingSailMastHeightPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail __instance, ref float __result)
        {
            var rig = __instance.GetComponent<FishermansFlyingSailRig>();
            if (!rig || rig.Corners == null)
                return true;
            __result = -rig.Corners[2].x * __instance.cloth.transform.parent.localScale.x;
            return false;
        }
    }

    // Rejects removal of parts supporting installed custom sails.
    [HarmonyPatch(typeof(BoatCustomParts), "CanUninstall")]
    internal static class FishermansFlyingSailSupportRemovalPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            BoatCustomParts __instance,
            int partIndex,
            int optionIndex,
            ref bool __result,
            ref string dependentOptionNames
        )
        {
            var option = __instance.availableParts[partIndex].partOptions[optionIndex];
            if (
                !__instance
                    .GetComponentsInChildren<FishermansFlyingSailRigging>(true)
                    .Any(r => r.DependsOn(option))
            )
                return;
            __result = false;
            dependentOptionNames = ": supports a Fisherman's Flying Sail; remove the sail first.";
        }
    }

    // Keeps occupied support parts active during native option previews.
    [HarmonyPatch(typeof(BoatPart), "SetOptionEnabled")]
    internal static class FishermansFlyingSailSupportPreviewPatch
    {
        [HarmonyPrefix]
        private static void Prefix(BoatPart __instance, int i, ref bool state)
        {
            if (state)
                return;
            var option = __instance.partOptions[i];
            var boat = option.GetComponentInParent<BoatRefs>();
            if (
                boat
                && boat.GetComponentsInChildren<FishermansFlyingSailRigging>(true)
                    .Any(r => r.DependsOn(option))
            )
                state = true;
        }
    }

    // Invalidates custom rigging when native boat parts refresh.
    [HarmonyPatch(typeof(BoatCustomParts), "RefreshParts")]
    internal static class FishermansFlyingSailPartsRefreshPatch
    {
        [HarmonyPostfix]
        internal static void Postfix(BoatCustomParts __instance)
        {
            foreach (
                var rig in __instance.GetComponentsInChildren<FishermansFlyingSailRigging>(true)
            )
                rig.Invalidate();
        }
    }

    // Refreshes feature state when native shipyard order previews change.
    [HarmonyPatch(typeof(BoatCustomParts), "RefreshPartsWithOrder")]
    internal static class FishermansFlyingSailOrderRefreshPatch
    {
        [HarmonyPostfix]
        private static void Postfix(BoatCustomParts __instance) =>
            FishermansFlyingSailPartsRefreshPatch.Postfix(__instance);
    }

    // Adds registered custom sails to native shipyard selections.
    [HarmonyPatch(typeof(Shipyard), "Awake")]
    internal static class FishermansFlyingSailShipyardPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shipyard __instance) =>
            FishermansFlyingSailPrefab.AddToShipyard(__instance);
    }

    // Adds custom sails when documents open after an early shipyard awakening.
    [HarmonyPatch(typeof(Shipyard), "ActivateDocuments")]
    internal static class FishermansFlyingSailShipyardFallbackPatch
    {
        // Covers shipyards that awakened before PrefabsDirectory.Start.
        [HarmonyPrefix]
        private static void Prefix(Shipyard __instance) =>
            FishermansFlyingSailPrefab.AddToShipyard(__instance);
    }

    // Suppresses native sheet rendering where the custom rig owns the visual.
    [HarmonyPatch(typeof(RopeEffect), "LateUpdate")]
    internal static class FishermansFlyingSailSheetVisualPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            RopeEffect __instance,
            LineRenderer ___lineRenderer,
            ClothRope ___clothRope
        )
        {
            var visual = __instance.GetComponent<FishermansFlyingSailNativeSheetVisual>();
            if (visual)
                visual.Suppress(___lineRenderer, ___clothRope);
        }
    }

    // Constrains native sheet and hinge travel to the custom sail limits.
    [HarmonyPatch(typeof(JibAngleMaster), "Update")]
    internal static class FishermansFlyingSailTravelPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Sail ___sail, out bool __state)
        {
            __state = ___sail && ___sail.GetComponent<FishermansFlyingSailRig>();
            if (!__state)
                return;
            // Native Update caches these for both sheet controllers. Saved
            // limits and later mast refreshes may still contain the old range.
            ___sail.minAngle = FishermansFlyingSailTravel.Clamp(___sail.minAngle);
            ___sail.maxAngle = FishermansFlyingSailTravel.Clamp(___sail.maxAngle);
        }

        [HarmonyPostfix]
        private static void Postfix(JibAngleMaster __instance, Sail ___sail, bool __state)
        {
            if (!__state)
                return;
            var hinge = __instance.sailHinge;
            var limits = hinge.limits;
            float min = limits.min,
                max = limits.max;
            // ApplySway runs inside native Update after the sheet limits are
            // assigned. Bound its final result, including stale sheet values,
            // without changing the sail pose or resetting Cloth.
            FishermansFlyingSailTravel.ConstrainHinge(
                ref min,
                ref max,
                ___sail.minAngle,
                ___sail.maxAngle
            );
            limits.min = min;
            limits.max = max;
            hinge.limits = limits;
        }
    }

    // Applies initial custom sail size and placement after native and optional SE initialization.
    [HarmonyPatch(typeof(ShipyardSailInstaller), "AddNewSail")]
    internal static class FishermansFlyingSailNewSailPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(ShipyardSailInstaller __instance, GameObject sailObject)
        {
            var rig = sailObject.GetComponent<FishermansFlyingSailRig>();
            if (!rig)
                return;
            var mast = __instance.GetCurrentMast();
            var sail = rig.Sail;
            // Native AddNewSail chooses the shipyard's first palette entry.
            // Use the existing white swatch for this sail's initial selection.
            sail.ChangeSailColor(FishermansFlyingSailAppearance.WhiteColorIndex);
            // Start at 100% of the smaller base mesh.
            sail.LoadScale(y: 1f, z: 1f);
            sail.ChangeInstallHeight(mast.mastHeight - sail.GetCurrentInstallHeight());
            sail.UpdateInstallPosition();
            sail.currentUnroll = 1f;
            rig.RefreshFlyingFrame();
            mast.UpdateControllerAttachments();
            ShipyardUI.instance.UpdateDescriptionText();
            GameState.currentShipyard.UpdateOrder();
        }
    }

    // Registers custom prefabs from the native donor after optional SE initialization.
    [HarmonyPatch(typeof(PrefabsDirectory), "Start")]
    internal static class FishermansFlyingSailRegistrationPatch
    {
        // When installed, let SE initialize donors first. Register before
        // All Sails captures the prefab array for its cached menu pages.
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        [HarmonyBefore("NatoriusG.AllSailsAllShipyards")]
        private static void Postfix(PrefabsDirectory __instance) =>
            FishermansFlyingSailPrefab.Register(__instance);
    }
}
