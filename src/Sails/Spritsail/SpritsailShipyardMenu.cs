using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Owns the added category button and native-only pagination for one shipyard UI instance.
    internal sealed class SpritsailShipyardMenu : MonoBehaviour
    {
        private static readonly MethodInfo compatible = AccessTools.Method(
            type: typeof(ShipyardUI),
            name: "SailMastCompatible"
        );
        private ShipyardUI owner;
        private GameObject listMenu;
        private GameObject[] sailButtons;
        private ShipyardButton categoryButton;
        private ShipyardButton previous;
        private ShipyardButton next;
        private int page;

        internal static SpritsailShipyardMenu Ensure(
            ShipyardUI ui,
            GameObject categoryMenu,
            GameObject addMenu,
            GameObject[] buttons
        )
        {
            var menu =
                ui.GetComponent<SpritsailShipyardMenu>()
                ?? ui.gameObject.AddComponent<SpritsailShipyardMenu>();
            menu.owner = ui;
            menu.listMenu = addMenu;
            menu.sailButtons = buttons;
            if (!menu.categoryButton)
                menu.AddCategory(categoryMenu: categoryMenu);
            return menu;
        }

        private void AddCategory(GameObject categoryMenu)
        {
            var buttons = categoryMenu
                .GetComponentsInChildren<ShipyardButton>(includeInactive: true)
                .Where(b => b.function == ShipyardButton.ButtonFunction.selectSailCategory)
                .OrderByDescending(b => b.transform.localPosition.y)
                .ToArray();
            if (buttons.Any(b => b.index == SpritsailRules.CategoryId))
                throw new InvalidOperationException(
                    message: "Shipyard category button 6 is already occupied; Spritsails did not replace it."
                );
            var donor = buttons.FirstOrDefault(b => b.index == (int)SailCategory.other);
            if (!donor || buttons.Length < 2)
                throw new InvalidOperationException(
                    message: "Expected native shipyard category buttons were not found."
                );
            categoryButton = Object.Instantiate(
                original: donor,
                parent: donor.transform.parent,
                worldPositionStays: false
            );
            categoryButton.name = "MoreSailwindSails Spritsails category";
            categoryButton.index = SpritsailRules.CategoryId;
            categoryButton.currentPrefab = null;
            categoryButton.SetText(text: SpritsailCategory.Name);
            // Fit seven rows inside the original six-row footprint, including collider and label.
            float top = buttons[0].transform.localPosition.y;
            float bottom = buttons[buttons.Length - 1].transform.localPosition.y;
            float ratio = (float)(buttons.Length - 1) / buttons.Length;
            for (int i = 0; i <= buttons.Length; i++)
            {
                var button = i == buttons.Length ? categoryButton : buttons[i];
                var position = button.transform.localPosition;
                position.y = Mathf.Lerp(a: top, b: bottom, t: (float)i / buttons.Length);
                button.transform.localPosition = position;
                var scale = button.transform.localScale;
                scale.y *= ratio;
                button.transform.localScale = scale;
            }
            categoryButton.gameObject.SetActive(value: true);
        }

        internal void ResetPage()
        {
            page = 0;
            HidePager();
            SpritsailAllSails.Prepare(reset: true);
        }

        internal void HidePager()
        {
            if (previous)
                previous.gameObject.SetActive(value: false);
            if (next)
                next.gameObject.SetActive(value: false);
        }

        internal void ShowNative()
        {
            var available = SpritsailCatalog.Available(
                source: GameState.currentShipyard.sailPrefabs
            );
            int size = sailButtons.Length;
            page = SpritsailRules.ClampPage(page: page, count: available.Length, size: size);
            listMenu.SetActive(value: true);
            for (int i = 0; i < size; i++)
            {
                int index = page * size + i;
                var button = sailButtons[i];
                button.SetActive(value: index < available.Length);
                if (index >= available.Length)
                    continue;
                var prefab = available[index];
                button.GetComponent<ShipyardButton>().RegisterPrefab(prefab: prefab);
                bool allowed = (bool)
                    compatible.Invoke(obj: owner, parameters: new object[] { prefab });
                SetEnabled(button: button, allowed: allowed);
            }
            if (SpritsailRules.PageCount(count: available.Length, size: size) <= 1)
            {
                HidePager();
                return;
            }
            if (!previous)
            {
                previous = CreatePageButton(direction: -1);
                next = CreatePageButton(direction: 1);
            }
            previous.gameObject.SetActive(value: true);
            next.gameObject.SetActive(value: true);
            SetEnabled(button: previous.gameObject, allowed: page > 0);
            SetEnabled(
                button: next.gameObject,
                allowed: page + 1 < SpritsailRules.PageCount(count: available.Length, size: size)
            );
            previous.SetText(text: $"<  {page + 1}");
            next.SetText(
                text: $"{SpritsailRules.PageCount(count: available.Length, size: size)}  >"
            );
        }

        private void SetEnabled(GameObject button, bool allowed)
        {
            button.GetComponent<Renderer>().sharedMaterial = allowed
                ? owner.parchmentMaterial
                : owner.darkParchmentMaterial;
            button.GetComponent<Collider>().enabled = allowed;
        }

        private ShipyardButton CreatePageButton(int direction)
        {
            var donor = sailButtons[0].GetComponent<ShipyardButton>();
            var button = Object.Instantiate(
                original: donor,
                parent: donor.transform.parent,
                worldPositionStays: false
            );
            button.name = $"Spritsails page {direction}";
            button.currentPrefab = null;
            var marker = button.gameObject.AddComponent<SpritsailPageButton>();
            marker.Menu = this;
            marker.Direction = direction;
            var position = donor.transform.localPosition;
            // Use the same side gutter occupied by All Sails' navigation when it is installed.
            position.x += 5f;
            position.y -= direction > 0 ? 0.85f : 2.85f;
            button.transform.localPosition = position;
            button.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            return button;
        }

        internal void ChangePage(int direction)
        {
            page += direction;
            ShowNative();
        }
    }
}
