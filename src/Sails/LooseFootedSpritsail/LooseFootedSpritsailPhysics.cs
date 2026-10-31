// Refreshes native force state before physics and clears it while the rig cannot sail.
using System;
using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal static class LooseFootedSpritsailPhysics
    {
        private static readonly Action<Sail> updateWindForce = AccessTools.MethodDelegate<
            Action<Sail>
        >(AccessTools.Method(type: typeof(Sail), name: "UpdateWindForceOnSail"));
        private static readonly AccessTools.FieldRef<Sail, float> forwardForce =
            AccessTools.FieldRefAccess<Sail, float>(fieldName: "unamplifiedForwardForce");
        private static readonly AccessTools.FieldRef<Sail, float> sideForce =
            AccessTools.FieldRefAccess<Sail, float>(fieldName: "unamplifiedSidewayForce");
        private static readonly AccessTools.FieldRef<Sail, float> totalForce =
            AccessTools.FieldRefAccess<Sail, float>(fieldName: "totalWindForce");
        private static readonly AccessTools.FieldRef<Sail, float> finalForce =
            AccessTools.FieldRefAccess<Sail, float>(fieldName: "outFinalForwardForce");
        private static readonly AccessTools.FieldRef<Sail, bool> backwards =
            AccessTools.FieldRefAccess<Sail, bool>(fieldName: "pushingBoatBackwards");

        internal static void Refresh(Sail sail)
        {
            sail.apparentWind = Wind.currentWind - sail.shipRigidbody.velocity;
            updateWindForce(sail);
        }

        internal static void Clear(Sail sail)
        {
            sail.apparentWind = Vector3.zero;
            sail.appliedWindForce = 0;
            forwardForce(sail) = sideForce(sail) = totalForce(sail) = finalForce(sail) = 0;
            backwards(sail) = false;
        }
    }
}
