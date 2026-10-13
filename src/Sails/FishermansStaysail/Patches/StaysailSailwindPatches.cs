// Groups Staysail patches for native Sailwind behavior.
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MoreSailwindSails.Sails.FishermansStaysail.MkA;
using MoreSailwindSails.Sails.FishermansStaysail.MkB;
using MoreSailwindSails.Sails.FishermansStaysail.MkC;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansStaysail.Patches
{
    // Refreshes the custom aerodynamic frame before native wind-force calculation.
    [HarmonyPatch(typeof(Sail), "UpdateWindForceOnSail")]
    internal static class FishermansStaysailWindFramePatch
    {
        [HarmonyPrefix]
        private static void Prefix(Sail __instance)
        {
            var rig = __instance.GetComponent<FishermansStaysailRig>();
            if (rig)
                rig.RefreshAerodynamics();
        }
    }

    // Supplies force direction from the custom sail aerodynamic frame.
    [HarmonyPatch(typeof(Sail), "GetSailForceDirection")]
    internal static class FishermansStaysailForceDirectionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail __instance, ref Vector3 __result)
        {
            var rig = __instance.GetComponent<FishermansStaysailRig>();
            if (!rig || !rig.RefreshAerodynamics())
                return true;
            __result = FishermansStaysailAerodynamics.ForceDirection(
                __instance.apparentWind,
                __instance.windcenter.up
            );
            return false;
        }
    }

    // Aligns the native installation collision sweep with the custom sail frame.
    [HarmonyPatch(typeof(ShipyardSailColChecker), "UpdateRotation")]
    internal static class FishermansStaysailCollisionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(
            ShipyardSailColChecker __instance,
            Sail ___sail,
            int ___checkDelay,
            ref UnityEngine.Quaternion ___initialRot
        )
        {
            var rig = ___sail ? ___sail.GetComponent<FishermansStaysailRig>() : null;
            if (!rig || !rig.RefreshFrame())
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

    // Checks custom support geometry for the native mast-height fitting test.
    [HarmonyPatch(typeof(ShipyardSailInstaller), "MastNotTallEnough")]
    internal static class FishermansStaysailFitPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Mast mast, Sail sail, ref bool __result)
        {
            if (!sail.GetComponent<FishermansStaysailRig>())
                return true;
            __result = FishermansStaysailRigging.InstallError(sail, mast) != null;
            return false;
        }
    }

    // Constrains native sail movement to the available staysail support span.
    [HarmonyPatch(typeof(ShipyardSailInstaller), "MoveHeldSail")]
    internal static class FishermansStaysailMovePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ShipyardSailInstaller __instance, float distance)
        {
            var sail = __instance.GetCurrentSail();
            var rig = sail ? sail.GetComponent<FishermansStaysailRig>() : null;
            if (!rig)
                return true;
            if (sail.IsInstalled() || !rig.RefreshFrame())
                return false;
            var binding = FishermansStaysailRigging.For(sail);
            var mount = __instance.GetCurrentMast();
            float room =
                Vector3.Dot(binding.ForePoint - binding.SectionBottom, binding.ForeAxis)
                - FishermansStaysailInstallationGeometry.HeadClearance;
            float maxDrop = Mathf.Max(0, room - sail.GetScaledHeight());
            float height = Mathf.Clamp(
                sail.GetCurrentInstallHeight() + distance,
                mount.mastHeight - maxDrop,
                mount.mastHeight
            );
            sail.ChangeInstallHeight(height - sail.GetCurrentInstallHeight());
            AccessTools
                .Method(typeof(ShipyardSailInstaller), "ApplySailPosition")
                .Invoke(__instance, null);
            return false;
        }
    }

    // Refreshes custom cloth through its rig instead of the native animation path.
    [HarmonyPatch(typeof(ReefEffectAnimUniversal), "RefreshCloth")]
    internal static class FishermansStaysailClothRefreshPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ReefEffectAnimUniversal __instance)
        {
            var rig = __instance.GetComponent<FishermansStaysailRig>();
            if (!rig)
                return true;
            rig.RefreshCloth();
            return false;
        }
    }

    // Limits native mast selection to valid custom sail supports.
    [HarmonyPatch(typeof(ShipyardUI), "SailMastCompatible")]
    internal static class FishermansStaysailMastCompatiblePatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameObject sailPrefab, ref bool __result)
        {
            if (__result && sailPrefab.GetComponent<FishermansStaysailRig>())
                __result = FishermansStaysailRigging.TryResolve(
                    GameState.currentShipyard.sailInstaller.GetCurrentMast(),
                    out _
                );
        }
    }

    // Reports support-geometry errors through native installation validation.
    [HarmonyPatch(typeof(ShipyardSailInstaller), "GetInstallError")]
    internal static class FishermansStaysailInstallErrorPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail sail, Mast mast, ref bool error, ref string __result)
        {
            if (!sail.GetComponent<FishermansStaysailRig>())
                return true;
            string reason = FishermansStaysailRigging.InstallError(sail, mast);
            if (reason == null)
                return true;
            error = true;
            __result = reason;
            return false;
        }
    }

    // Reports custom sail height from its posed geometry and scale.
    [HarmonyPatch(typeof(Sail), "GetScaledHeight")]
    internal static class FishermansStaysailMastHeightPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail __instance, ref float __result)
        {
            var rig = __instance.GetComponent<FishermansStaysailRig>();
            if (!rig || rig.Corners == null)
                return true;
            __result = -rig.Corners[2].x * __instance.cloth.transform.parent.localScale.x;
            return false;
        }
    }

    // Let native binding process only native-controlled sails. Restore the actual
    // list even on exceptions; saves, mast capacity and overlap checks see all sails.
    [HarmonyPatch(typeof(Mast), "UpdateControllerAttachments")]
    internal static class FishermansStaysailControlsPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Mast __instance, out List<GameObject> __state)
        {
            __state = null;
            if (
                __instance.sails == null
                || !__instance.sails.Any(s => s && s.GetComponent<FishermansStaysailRig>())
            )
                return;
            __state = __instance.sails;
            __instance.sails = __state
                .Where(s => s && !s.GetComponent<FishermansStaysailRig>())
                .ToList();
        }

        [HarmonyFinalizer]
        private static void Finalizer(
            Mast __instance,
            List<GameObject> __state,
            Exception __exception
        )
        {
            if (__state == null)
                return;
            __instance.sails = __state;
            __instance.UpdateSailOrder();
            if (__exception != null)
                return;
            foreach (var item in __state)
                if (item && item.GetComponent<FishermansStaysailRig>())
                    FishermansStaysailRigging.For(item.GetComponent<Sail>()).AttachControls();
        }
    }

    // Rejects removal of parts supporting installed custom sails.
    [HarmonyPatch(typeof(BoatCustomParts), "CanUninstall")]
    internal static class FishermansStaysailSupportRemovalPatch
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
                    .GetComponentsInChildren<FishermansStaysailRigging>(true)
                    .Any(r => r.DependsOn(option))
            )
                return;
            __result = false;
            dependentOptionNames = ": supports a Fisherman's Staysail; remove the sail first.";
        }
    }

    // Keeps occupied support parts active during native option previews.
    [HarmonyPatch(typeof(BoatPart), "SetOptionEnabled")]
    internal static class FishermansStaysailSupportPreviewPatch
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
                && boat.GetComponentsInChildren<FishermansStaysailRigging>(true)
                    .Any(r => r.DependsOn(option))
            )
                state = true;
        }
    }

    // Invalidates custom rigging when native boat parts refresh.
    [HarmonyPatch(typeof(BoatCustomParts), "RefreshParts")]
    internal static class FishermansStaysailPartsRefreshPatch
    {
        [HarmonyPostfix]
        internal static void Postfix(BoatCustomParts __instance)
        {
            foreach (var rig in __instance.GetComponentsInChildren<FishermansStaysailRigging>(true))
                rig.Invalidate();
        }
    }

    // Refreshes feature state when native shipyard order previews change.
    [HarmonyPatch(typeof(BoatCustomParts), "RefreshPartsWithOrder")]
    internal static class FishermansStaysailOrderRefreshPatch
    {
        [HarmonyPostfix]
        private static void Postfix(BoatCustomParts __instance) =>
            FishermansStaysailPartsRefreshPatch.Postfix(__instance);
    }

    // Adds registered custom sails to native shipyard selections.
    [HarmonyPatch(typeof(Shipyard), "Awake")]
    internal static class FishermansStaysailShipyardPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shipyard __instance)
        {
            FishermansStaysailMkA.AddToShipyard(__instance);
            FishermansStaysailMkB.AddToShipyard(__instance);
            FishermansStaysailMkC.AddToShipyard(__instance);
        }
    }

    // Adds custom sails when documents open after an early shipyard awakening.
    [HarmonyPatch(typeof(Shipyard), "ActivateDocuments")]
    internal static class FishermansStaysailShipyardFallbackPatch
    {
        // Covers shipyards that awakened before PrefabsDirectory.Start.
        [HarmonyPrefix]
        private static void Prefix(Shipyard __instance)
        {
            FishermansStaysailMkA.AddToShipyard(__instance);
            FishermansStaysailMkB.AddToShipyard(__instance);
            FishermansStaysailMkC.AddToShipyard(__instance);
        }
    }

    // Constrains native sheet and hinge travel to the custom sail limits.
    [HarmonyPatch(typeof(JibAngleMaster), "Update")]
    internal static class FishermansStaysailTravelPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Sail ___sail, out bool __state)
        {
            __state = ___sail && ___sail.GetComponent<FishermansStaysailRig>();
            if (!__state)
                return;
            // Native Update caches these for both sheet controllers. Saved
            // limits and later mast refreshes may still contain the old range.
            ___sail.minAngle = FishermansStaysailTravel.Clamp(___sail.minAngle);
            ___sail.maxAngle = FishermansStaysailTravel.Clamp(___sail.maxAngle);
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
            FishermansStaysailTravel.ConstrainHinge(
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
    internal static class FishermansStaysailNewSailPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(ShipyardSailInstaller __instance, GameObject sailObject)
        {
            var rig = sailObject.GetComponent<FishermansStaysailRig>();
            if (!rig)
                return;
            var mast = __instance.GetCurrentMast();
            var sail = rig.Sail;
            // Native AddNewSail chooses the shipyard's first palette entry.
            // Use the existing white swatch for this sail's initial selection.
            sail.ChangeSailColor(FishermansStaysailAppearance.WhiteColorIndex);
            // Only new shipyard selections get the smaller initial size.
            // Loaded sails continue to restore their saved SE dimensions.
            sail.LoadScale(y: 0.5f, z: 0.5f);
            sail.ChangeInstallHeight(mast.mastHeight - sail.GetCurrentInstallHeight());
            sail.UpdateInstallPosition();
            sail.currentUnroll = 1f;
            rig.RefreshFrame();
            mast.UpdateControllerAttachments();
            ShipyardUI.instance.UpdateDescriptionText();
            GameState.currentShipyard.UpdateOrder();
        }
    }

    // Registers custom prefabs from the native donor after optional SE initialization.
    [HarmonyPatch(typeof(PrefabsDirectory), "Start")]
    internal static class FishermansStaysailRegistrationPatch
    {
        // When installed, let SE initialize donors first. Register before
        // All Sails captures the prefab array for its cached menu pages.
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        [HarmonyBefore("NatoriusG.AllSailsAllShipyards")]
        private static void Postfix(PrefabsDirectory __instance)
        {
            FishermansStaysailMkA.Register(__instance);
            FishermansStaysailMkB.Register(__instance);
            FishermansStaysailMkC.Register(__instance);
        }
    }
}
