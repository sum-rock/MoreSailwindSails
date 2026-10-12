// Adds pages of native part rows when MSS outgrows the shipyard's original panel.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Stays.FishermansStay
{
    internal sealed class FishermansStayPartsPages : MonoBehaviour
    {
        private readonly List<GameObject> pages = new List<GameObject>();
        private int pageSize;
        private int page;
        private int pageCount;
        private int category = -1;
        private BoatCustomParts boat;
        private GameObject navigation;
        private TextMesh pageText;
        private GameObject previous;
        private GameObject next;

        internal void Prepare(
            Transform parent,
            BoatCustomParts parts,
            int requestedCategory,
            ref ShipyardButton[] left,
            ref ShipyardButton[] right,
            ref TextMesh[] texts
        )
        {
            if (pages.Count == 0)
            {
                pageSize = texts.Length;
                if (pageSize < 2 || left.Length != pageSize || right.Length != pageSize)
                    throw new InvalidOperationException("Unexpected native parts panel layout.");
                var first = NewPage(parent: parent);
                for (int i = 0; i < pageSize; i++)
                {
                    left[i].transform.SetParent(parent: first.transform, worldPositionStays: true);
                    right[i].transform.SetParent(parent: first.transform, worldPositionStays: true);
                    texts[i].transform.SetParent(parent: first.transform, worldPositionStays: true);
                }
                navigation = new GameObject("Fisherman's Stay pages");
                navigation.transform.SetParent(parent: parent, worldPositionStays: false);
                var step =
                    texts[pageSize - 1].transform.localPosition
                    - texts[pageSize - 2].transform.localPosition;
                previous = CreateButton(source: left[pageSize - 1], step: step, delta: -1);
                next = CreateButton(source: right[pageSize - 1], step: step, delta: 1);
                pageText = Object.Instantiate(
                    original: texts[pageSize - 1],
                    parent: navigation.transform,
                    worldPositionStays: false
                );
                pageText.transform.localPosition += step;
            }
            if (boat != parts || category != requestedCategory)
                page = 0;
            boat = parts;
            category = requestedCategory;
            int count = parts.availableParts.Count(p => p.category == category);
            pageCount = Math.Max(1, (count + pageSize - 1) / pageSize);
            while (pages.Count < pageCount)
            {
                var container = NewPage(parent: parent);
                int start = texts.Length;
                Array.Resize(ref left, start + pageSize);
                Array.Resize(ref right, start + pageSize);
                Array.Resize(ref texts, start + pageSize);
                for (int i = 0; i < pageSize; i++)
                {
                    left[start + i] = Object.Instantiate(
                        original: left[i],
                        parent: container.transform,
                        worldPositionStays: false
                    );
                    right[start + i] = Object.Instantiate(
                        original: right[i],
                        parent: container.transform,
                        worldPositionStays: false
                    );
                    texts[start + i] = Object.Instantiate(
                        original: texts[i],
                        parent: container.transform,
                        worldPositionStays: false
                    );
                }
            }
        }

        private GameObject NewPage(Transform parent)
        {
            var result = new GameObject("Fisherman's Stay parts page " + pages.Count);
            result.transform.SetParent(parent: parent, worldPositionStays: false);
            pages.Add(result);
            return result;
        }

        private GameObject CreateButton(ShipyardButton source, Vector3 step, int delta)
        {
            var clone = Object.Instantiate(
                original: source,
                parent: navigation.transform,
                worldPositionStays: false
            );
            clone.transform.localPosition += step;
            var button = clone.gameObject.AddComponent<FishermansStayPageButton>();
            button.Owner = this;
            button.Delta = delta;
            return clone.gameObject;
        }

        internal void Show(int delta = 0)
        {
            page = Math.Max(0, Math.Min(page + delta, pageCount - 1));
            for (int i = 0; i < pages.Count; i++)
                pages[i].SetActive(value: i == page);
            navigation.SetActive(value: pageCount > 1);
            previous.SetActive(value: page > 0);
            next.SetActive(value: page + 1 < pageCount);
            pageText.text = $"Page {page + 1} / {pageCount}";
        }
    }
}
