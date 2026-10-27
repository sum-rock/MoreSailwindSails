// Integrates spritsail fitting, paired sheets and native sailing physics with Sailwind.
using HarmonyLib;
using MoreSailwindSails.Sails.LooseFootedSpritsail.MkA;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail.Patches
{
    [HarmonyPatch(typeof(PrefabsDirectory), "Start")]
    internal static class SpritsailRegistrationPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        [HarmonyBefore("NatoriusG.AllSailsAllShipyards")]
        private static void Postfix(PrefabsDirectory __instance) =>
            LooseFootedSpritsailMkA.Register(directory: __instance);
    }

    [HarmonyPatch(typeof(Shipyard), "Awake")]
    internal static class SpritsailShipyardPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shipyard __instance) =>
            LooseFootedSpritsailMkA.AddToShipyard(shipyard: __instance);
    }

    [HarmonyPatch(typeof(Shipyard), "ActivateDocuments")]
    internal static class SpritsailShipyardFallbackPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Shipyard __instance) =>
            LooseFootedSpritsailMkA.AddToShipyard(shipyard: __instance);
    }

    [HarmonyPatch(typeof(ShipyardUI), "SailMastCompatible")]
    internal static class SpritsailMastCompatiblePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(GameObject sailPrefab, ref bool __result)
        {
            if (!sailPrefab.GetComponent<LooseFootedSpritsailRig>())
                return true;
            __result =
                GameState.currentShipyard
                && GameState.currentShipyard.sailInstaller.GetCurrentMast();
            return false;
        }
    }

    [HarmonyPatch(typeof(ShipyardSailInstaller), "AddNewSail")]
    internal static class SpritsailNewSailPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(ShipyardSailInstaller __instance, GameObject sailObject)
        {
            var rig = sailObject.GetComponent<LooseFootedSpritsailRig>();
            if (!rig)
                return;
            var sail = rig.Sail;
            var mast = __instance.GetCurrentMast();
            sail.ChangeSailColor(newColor: 11);
            sail.LoadScale(y: 1, z: 1);
            sail.ChangeInstallHeight(changeValue: mast.mastHeight - sail.GetCurrentInstallHeight());
            sail.UpdateInstallPosition();
            mast.UpdateControllerAttachments();
            __instance.RecheckAllSailsCols();
            ShipyardUI.instance.UpdateDescriptionText();
            GameState.currentShipyard.UpdateOrder();
        }
    }

    [HarmonyPatch(typeof(Sail), "UpdateInstallPosition")]
    internal static class SpritsailPositionPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(Sail __instance) =>
            __instance.GetComponent<LooseFootedSpritsailRig>()?.Refresh();
    }

    [HarmonyPatch(typeof(Sail), "GetScaledHeight")]
    internal static class SpritsailHeightPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail __instance, ref float __result)
        {
            var rig = __instance.GetComponent<LooseFootedSpritsailRig>();
            if (!rig)
                return true;
            __result = rig.BaseLuff * rig.ScaleRoot.localScale.y;
            return false;
        }
    }

    [HarmonyPatch(typeof(Sail), "ChangeScale")]
    internal static class SpritsailScalePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail __instance, float changeY, float changeZ)
        {
            var rig = __instance.GetComponent<LooseFootedSpritsailRig>();
            if (!rig)
                return true;
            rig.ChangeScale(height: changeY, width: changeZ);
            return false;
        }
    }

    [HarmonyPatch(typeof(Sail), "GetSailArea")]
    internal static class SpritsailAreaPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail __instance, ref float __result)
        {
            var rig = __instance.GetComponent<LooseFootedSpritsailRig>();
            if (!rig)
                return true;
            __result = rig.BaseArea * rig.ScaleRoot.localScale.y * rig.ScaleRoot.localScale.z;
            return false;
        }
    }

    [HarmonyPatch(typeof(ShipyardSailColChecker), "UpdateRotation")]
    internal static class SpritsailCollisionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(
            ShipyardSailColChecker __instance,
            Sail ___sail,
            int ___checkDelay,
            ref Quaternion ___initialRot
        )
        {
            var rig = ___sail ? ___sail.GetComponent<LooseFootedSpritsailRig>() : null;
            if (!rig)
                return true;
            if (___checkDelay <= 2)
            {
                float angle =
                    __instance.currentAngle > __instance.startMaxAngle
                        ? 0
                        : __instance.currentAngle;
                rig.PositionCollisionChecker(target: __instance, angle: angle);
                ___initialRot = Quaternion.identity;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Sail), "ChangeSailColor")]
    internal static class SpritsailColorPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter("com.nandbrew.shipyardexpansion")]
        private static void Postfix(Sail __instance) =>
            __instance.GetComponent<LooseFootedSpritsailRig>()?.SyncColor();
    }

    [HarmonyPatch(typeof(ReefEffectAnimUniversal), "RefreshCloth")]
    internal static class SpritsailClothRefreshPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ReefEffectAnimUniversal __instance) =>
            !__instance.GetComponent<LooseFootedSpritsailRig>();
    }

    [HarmonyPatch(typeof(Sail), "Update")]
    internal static class SpritsailUpdatePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail __instance)
        {
            var rig = __instance.GetComponent<LooseFootedSpritsailRig>();
            return !rig || rig.PrepareSailing();
        }
    }

    [HarmonyPatch(typeof(Sail), "FixedUpdate")]
    internal static class SpritsailPhysicsPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail __instance)
        {
            var rig = __instance.GetComponent<LooseFootedSpritsailRig>();
            if (!rig)
                return true;
            if (!rig.PrepareSailing())
                return false;
            // FixedUpdate may precede Update after placement recovery or loading.
            // Recompute native force before applying it, never reuse a stale trim.
            LooseFootedSpritsailPhysics.Refresh(sail: __instance);
            return true;
        }
    }

    [HarmonyPatch(typeof(Sail), "UpdateWindForceOnSail")]
    internal static class SpritsailWindFramePatch
    {
        [HarmonyPrefix]
        private static void Prefix(Sail __instance) =>
            __instance.GetComponent<LooseFootedSpritsailRig>()?.RefreshAerodynamics();
    }

    [HarmonyPatch(typeof(Sail), "GetSailForceDirection")]
    internal static class SpritsailForceDirectionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Sail __instance, ref Vector3 __result)
        {
            if (!__instance.GetComponent<LooseFootedSpritsailRig>())
                return true;
            __result = LooseFootedSpritsailSailingGeometry.ForceDirection(
                wind: __instance.apparentWind,
                normal: __instance.windcenter.up
            );
            return false;
        }
    }

    [HarmonyPatch(typeof(Sail), "GetRealSailPower")]
    internal static class SpritsailPowerPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Sail __instance, ref float __result)
        {
            if (__instance.GetComponent<LooseFootedSpritsailRig>())
                __result *= LooseFootedSpritsailSailingGeometry.PowerMultiplier;
        }
    }

    [HarmonyPatch(typeof(JibAngleMaster), "ResetHingeRestingRot")]
    internal static class SpritsailHingeResetPatch
    {
        // Mast schedules this native reset after attachment. The rig has already set
        // its own neutral pivot; toggling it again would retire freshly placed sheets.
        [HarmonyPrefix]
        private static bool Prefix(JibAngleMaster __instance) =>
            !__instance.GetComponent<LooseFootedSpritsailRig>();
    }

    [HarmonyPatch(typeof(JibAngleMaster), "Update")]
    internal static class SpritsailSheetLimitsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(JibAngleMaster __instance)
        {
            var rig = __instance.GetComponent<LooseFootedSpritsailRig>();
            if (!rig || rig.Sail.minAngle >= rig.Sail.maxAngle)
                return;
            // Native jib sway extends its limits. Spritsail hardware must remain
            // inside the clearance angles established by the shipyard sweep.
            var hinge = __instance.sailHinge;
            var limits = hinge.limits;
            limits.min = Mathf.Clamp(
                value: limits.min,
                min: rig.Sail.minAngle,
                max: rig.Sail.maxAngle
            );
            limits.max = Mathf.Clamp(value: limits.max, min: limits.min, max: rig.Sail.maxAngle);
            hinge.limits = limits;
        }
    }

    [HarmonyPatch(typeof(ShipyardSailInstaller), "MoveHeldSail")]
    internal static class SpritsailMovementPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ShipyardSailInstaller __instance, float distance)
        {
            var sail = __instance.GetCurrentSail();
            var rig = sail ? sail.GetComponent<LooseFootedSpritsailRig>() : null;
            var mast = __instance.GetCurrentMast();
            if (!rig || !mast)
                return true;
            if (sail.IsInstalled())
                return false;
            float minimum = Mathf.Min(a: sail.GetScaledHeight(), b: mast.mastHeight);
            float next = Mathf.Clamp(
                value: sail.GetCurrentInstallHeight() + distance,
                min: minimum,
                max: mast.mastHeight
            );
            sail.ChangeInstallHeight(changeValue: next - sail.GetCurrentInstallHeight());
            sail.UpdateInstallPosition();
            mast.UpdateSailOrder();
            mast.UpdateControllerAttachments();
            ShipyardUI.instance.UpdateMastSailsList();
            ShipyardUI.instance.UpdateButtonSelections();
            __instance.RecheckAllSailsCols();
            GameState.currentShipyard.UpdateOrder();
            return false;
        }
    }
}
