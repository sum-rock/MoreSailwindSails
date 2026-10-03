using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Stores one make-selected tack state shared by propulsion and the procedural sail pose.
    internal sealed class SpritsailObstruction : MonoBehaviour
    {
        public bool StarboardAffected = true;
        internal float Amount { get; private set; }
        private float target;

        internal void UpdateState(Transform boat, Vector3 apparentWind, bool valid)
        {
            if (!valid || !boat || !isActiveAndEnabled)
            {
                ResetState();
                return;
            }
            var wind = boat.InverseTransformDirection(direction: apparentWind);
            if (
                !SpritsailDeployment.Finite(value: wind.x)
                || !SpritsailDeployment.Finite(value: wind.y)
                || !SpritsailDeployment.Finite(value: wind.z)
            )
            {
                ResetState();
                return;
            }
            target = SpritsailObstructionGeometry.Target(
                boatWind: wind,
                starboardAffected: StarboardAffected,
                previous: target
            );
            Amount = SpritsailObstructionGeometry.Step(
                previous: Amount,
                target: target,
                seconds: Time.deltaTime
            );
        }

        internal void ResetState()
        {
            Amount = target = 0;
        }

        private void OnDisable() => ResetState();
    }
}
