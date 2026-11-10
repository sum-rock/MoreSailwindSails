// Shows one quiet bad-tack status for active spritsails aboard the player's current boat.
using System.Collections.Generic;
using UnityEngine;

namespace MoreSailwindSails.Sails.LooseFootedSpritsail
{
    internal sealed class LooseFootedSpritsailNotice : MonoBehaviour
    {
        private static readonly HashSet<LooseFootedSpritsailRig> rigs =
            new HashSet<LooseFootedSpritsailRig>();
        private GUIStyle style;
        private Transform boat;
        private float elapsed;
        private float opacity;
        private const float VisibleSeconds = 30f;
        private const float FadeSeconds = 2f;
        private const string Message = "Spritsail bad tack: −10% force";

        internal static void Register(LooseFootedSpritsailRig rig) => rigs.Add(item: rig);

        internal static void Unregister(LooseFootedSpritsailRig rig) => rigs.Remove(item: rig);

        private static bool CanShow =>
            GameState.playing
            && !GameState.currentlyLoading
            && !GameState.inCursorMenu
            && !GameState.currentShipyard
            && !GameState.shipyardUpdatingOrder
            && !GameState.sleeping
            && GameState.currentBoat;

        private void OnDisable()
        {
            boat = null;
            elapsed = 0;
            opacity = 0;
        }

        private void LateUpdate()
        {
            if (boat != GameState.currentBoat || GameState.currentlyLoading)
            {
                boat = GameState.currentBoat;
                elapsed = 0;
            }
            opacity = 0;
            // Pause the display timer while menus or other game states hide the notice.
            if (!CanShow)
                return;
            foreach (var rig in rigs)
            {
                if (
                    rig
                    && rig.SailingReady
                    && !rig.IsStruck
                    && rig.transform.IsChildOf(parent: GameState.currentBoat)
                    && rig.TackPowerMultiplier < 1
                )
                {
                    elapsed += Time.deltaTime;
                    opacity = Mathf.Clamp01(value: 1 - (elapsed - VisibleSeconds) / FadeSeconds);
                    return;
                }
            }
            // Re-arm only once no deployed spritsail on this boat is on the bad tack.
            elapsed = 0;
        }

        private void OnGUI()
        {
            if (CanShow && opacity > 0)
                Draw();
        }

        private void Draw()
        {
            if (style == null)
                style = new GUIStyle(other: GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    wordWrap = false,
                    richText = false,
                };
            float scale = Mathf.Clamp(value: Screen.height / 1080f, min: 0.8f, max: 2f);
            style.fontSize = Mathf.RoundToInt(f: 14 * scale);
            var bounds = new Rect(
                Screen.width - 326 * scale,
                Screen.height - 40 * scale,
                310 * scale,
                24 * scale
            );
            var previous = GUI.color;
            try
            {
                GUI.color = new Color(1, 1, 1, opacity);
                style.normal.textColor = new Color(0, 0, 0, 0.8f);
                GUI.Label(
                    position: new Rect(
                        bounds.x + scale,
                        bounds.y + scale,
                        bounds.width,
                        bounds.height
                    ),
                    text: Message,
                    style: style
                );
                style.normal.textColor = new Color(0.9f, 0.85f, 0.7f, 0.9f);
                GUI.Label(position: bounds, text: Message, style: style);
            }
            finally
            {
                GUI.color = previous;
            }
        }
    }
}
