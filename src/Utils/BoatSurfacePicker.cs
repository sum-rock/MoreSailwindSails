using UnityEngine;

namespace MoreSailwindSails.Utils
{
    // Finds visible boat surfaces across the world and displaced walking colliders.
    internal static class BoatSurfacePicker
    {
        // Installed GoPointer.DoRaycast mask for ordinary world obstructions.
        private const int PointerMask = -604165;
        private const float Range = 10f;

        internal static bool CanInspect =>
            GameState.playing && !GameState.currentlyLoading && !GameState.inCursorMenu;

        internal static bool TryPick(out BoatSurfaceHit surface, out string error)
        {
            surface = null;
            error = null;
            var camera = Camera.main;
            if (!camera)
            {
                error = "No active game camera; nothing captured.";
                return false;
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
                error =
                    "No solid surface within 10 metres, including boat walking models; aim again.";
                return false;
            }
            if (!boat)
            {
                error =
                    "Hit "
                    + hit.collider.transform.name
                    + ", which is not a recognised boat surface.";
                return false;
            }
            surface = new BoatSurfaceHit
            {
                Boat = boat,
                Hit = hit,
                HitToWorld = hitToWorld,
                WalkingModel = walkingModel,
                Distance = distance,
            };
            return true;
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
    }
}
