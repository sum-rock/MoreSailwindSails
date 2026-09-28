using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MoreSailwindSails.Utils
{
    // Read-only, on-demand diagnostics. Capturing does not reserve or move a winch.
    internal static class LogFallbackWinchPlacement
    {
        private static int captureNumber;

        internal static void Capture()
        {
            if (!GameState.playing || GameState.currentlyLoading || GameState.inCursorMenu)
            {
                Report("Close menus and aim at a boat surface during normal play.");
                return;
            }
            if (!BoatSurfacePicker.TryPick(out var surface, out string error))
            {
                Report(error);
                return;
            }
            var boat = surface.Boat;
            var hit = surface.Hit;
            var hitToWorld = surface.HitToWorld;
            bool walkingModel = surface.WalkingModel;
            float distance = surface.Distance;
            var target = hit.collider.transform;

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
