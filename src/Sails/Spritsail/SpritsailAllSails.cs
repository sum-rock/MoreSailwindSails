using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Adapts the optional All Sails page cache without a hard assembly dependency.
    internal static class SpritsailAllSails
    {
        internal const string HarmonyId = "NatoriusG.AllSailsAllShipyards";
        private static bool resolved;
        private static FieldInfo pages;
        private static FieldInfo completeList;
        private static FieldInfo currentPage;
        private static FieldInfo currentCategory;
        private static ConstructorInfo pageConstructor;
        private static Type pageType;

        internal static bool HasContract(Type patch, Type page, Type button, Type main)
        {
            if (patch == null || page == null || button == null || main == null)
                return false;
            const BindingFlags fields =
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var cache = patch.GetField(name: "pagesByCategory", bindingAttr: fields);
            return cache != null
                && cache.IsStatic
                && cache.FieldType
                    == typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(
                        typeof(SailCategory),
                        page.MakeArrayType()
                    )
                && page.GetConstructor(types: new[] { typeof(SailCategory), typeof(GameObject[]) })
                    != null
                && button.GetField(name: "page", bindingAttr: fields)?.FieldType == typeof(int)
                && button.GetField(name: "currentCategory", bindingAttr: fields)?.FieldType
                    == typeof(SailCategory)
                && main.GetField(name: "completeShipyardList", bindingAttr: fields)?.FieldType
                    == typeof(GameObject[]);
        }

        private static void Resolve()
        {
            if (resolved)
                return;
            resolved = true;
            var patch = AccessTools.TypeByName(name: "AllSailsAllShipyards.ShipyardUIPatch");
            if (patch == null)
                return;
            pageType = AccessTools.TypeByName(name: "AllSailsAllShipyards.ShipyardSailPage");
            var button = AccessTools.TypeByName(
                name: "AllSailsAllShipyards.ShipyardSailPageButton"
            );
            var main = AccessTools.TypeByName(name: "AllSailsAllShipyards.Main");
            if (
                pageType == null
                || button == null
                || main == null
                || !HasContract(patch: patch, page: pageType, button: button, main: main)
            )
                throw new InvalidOperationException(
                    message: "All Sails paging API changed; review Spritsails shipyard integration."
                );
            pages = AccessTools.Field(type: patch, name: "pagesByCategory");
            completeList = AccessTools.Field(type: main, name: "completeShipyardList");
            currentPage = AccessTools.Field(type: button, name: "page");
            currentCategory = AccessTools.Field(type: button, name: "currentCategory");
            pageConstructor = pageType.GetConstructor(
                types: new[] { typeof(SailCategory), typeof(GameObject[]) }
            );
        }

        internal static bool Prepare(bool reset)
        {
            Resolve();
            if (!(pages?.GetValue(obj: null) is IDictionary cache))
                return false;
            var available = SpritsailCatalog.Available(
                source: (GameObject[])completeList.GetValue(obj: null)
            );
            const int pageSize = 12;
            var count = SpritsailRules.PageCount(count: available.Length, size: pageSize);
            var result = Array.CreateInstance(elementType: pageType, length: count);
            for (int page = 0; page < count; page++)
            {
                var chunk = new GameObject[Math.Min(pageSize, available.Length - page * pageSize)];
                Array.Copy(
                    sourceArray: available,
                    sourceIndex: page * pageSize,
                    destinationArray: chunk,
                    destinationIndex: 0,
                    length: chunk.Length
                );
                result.SetValue(
                    value: pageConstructor.Invoke(
                        parameters: new object[] { SpritsailCategory.Value, chunk }
                    ),
                    index: page
                );
            }
            cache[SpritsailCategory.Value] = result;
            if ((SailCategory)currentCategory.GetValue(obj: null) == SpritsailCategory.Value)
                currentPage.SetValue(
                    obj: null,
                    value: reset
                        ? 0
                        : SpritsailRules.ClampPage(
                            page: (int)currentPage.GetValue(obj: null),
                            count: available.Length,
                            size: pageSize
                        )
                );
            return true;
        }
    }
}
