using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MoreSailwindSails.Utils
{
    // Read-only, on-demand diagnostics. Capturing does not reserve or move a winch.
    internal static class LogFallbackWinchPlacement
    {
        // Installed GoPointer.DoRaycast mask for ordinary world obstructions.
        // Boat walking colliders are queried separately in their displaced frame.
        private const int PointerMask = -604165;
        private const float Range = 10f;
        private static int captureNumber;

        internal static void Capture()
        {
            if (!GameState.playing || GameState.currentlyLoading || GameState.inCursorMenu)
            {
                Report("Close menus and aim at a boat surface during normal play.");
                return;
            }
            var camera = Camera.main;
            if (!camera)
            {
                Report("No active game camera; nothing captured.");
                return;
            }
            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var boats = Object.FindObjectsOfType<BoatRefs>();
            bool found = Physics.Raycast(
                ray,
                out var hit,
                Range,
                PointerMask,
                QueryTriggerInteraction.Ignore
            );
            var boat = found ? FindBoat(hit.collider.transform, boats) : null;
            var hitToWorld = Matrix4x4.identity;
            bool walkingModel = false;
            float distance = found ? hit.distance : Range;
            // The camera observes the visual boat, but its solid deck/rails live
            // in a separate walking model. Transform the ray BEFORE raycasting.
            // Query only that boat's colliders, without changing physics objects.
            foreach (var candidate in boats)
            {
                if (!candidate.boatModel || !candidate.walkCol)
                    continue;
                var worldToWalk =
                    candidate.walkCol.localToWorldMatrix * candidate.boatModel.worldToLocalMatrix;
                var walkRay = WinchCaptureGeometry.TransformRay(worldToWalk, ray, out float scale);
                if (scale <= 0f)
                    continue;
                foreach (var collider in candidate.walkCol.GetComponentsInChildren<Collider>())
                {
                    if (!collider.enabled || collider.isTrigger)
                        continue;
                    if (!collider.Raycast(walkRay, out var walkHit, distance * scale))
                        continue;
                    float worldDistance = walkHit.distance / scale;
                    if (found && worldDistance >= distance)
                        continue;
                    found = true;
                    hit = walkHit;
                    boat = candidate;
                    distance = worldDistance;
                    hitToWorld =
                        candidate.boatModel.localToWorldMatrix
                        * candidate.walkCol.worldToLocalMatrix;
                    walkingModel = true;
                }
            }
            if (!found)
            {
                Report(
                    "No solid surface within 10 metres, including boat walking models; aim again."
                );
                return;
            }
            var target = hit.collider.transform;
            if (!boat)
            {
                Report("Hit " + target.name + ", which is not a recognised boat surface.");
                return;
            }

            var hitToBoat = boat.transform.worldToLocalMatrix * hitToWorld;
            var position = hitToBoat.MultiplyPoint3x4(hit.point);
            // Normals are covectors: inverse-transpose also handles scaled roots.
            var normal = hitToBoat.inverse.transpose.MultiplyVector(hit.normal).normalized;
            int number = ++captureNumber;
            Plugin.Log.LogInfo(
                $"Winch position capture #{number}; boat={boat.name}#{boat.GetInstanceID()}, "
                    + $"position={Vector(position)}, normal={Vector(normal)}, "
                    + $"hitModel={(walkingModel ? "walk" : "boat")}, object={Path(target)}, "
                    + $"collider={hit.collider.GetType().Name}#{hit.collider.GetInstanceID()}, "
                    + $"distance={distance.ToString("F4", CultureInfo.InvariantCulture)}."
            );
            Notify($"Winch point #{number} captured on {target.name}. See LogOutput.log.");
        }

        private static BoatRefs FindBoat(Transform target, BoatRefs[] boats)
        {
            var parentBoat = target.GetComponentInParent<BoatRefs>();
            if (parentBoat)
                return parentBoat;
            foreach (var boat in boats)
                if (
                    (boat.walkCol && target.IsChildOf(boat.walkCol))
                    || (boat.boatModel && target.IsChildOf(boat.boatModel))
                )
                    return boat;
            return null;
        }

        private static string Vector(Vector3 vector) =>
            string.Format(
                CultureInfo.InvariantCulture,
                "({0:F6}, {1:F6}, {2:F6})",
                vector.x,
                vector.y,
                vector.z
            );

        private static string Path(Transform target)
        {
            var names = new Stack<string>();
            for (var node = target; node; node = node.parent)
                names.Push(node.name);
            return string.Join("/", names);
        }

        private static void Report(string message)
        {
            Plugin.Log.LogInfo("Winch position capture: " + message);
            Notify(message);
        }

        private static void Notify(string message)
        {
            if (NotificationUi.instance)
                NotificationUi.instance.ShowNotification(message);
        }
    }
}
