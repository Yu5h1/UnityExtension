using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Yu5h1Lib.UI.Acceptance.UxUiAcceptanceHost;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Builds a scrollable grid and filters the registered demos as the query changes.</summary>
    internal sealed class UxUiAcceptanceCatalog
    {
        private const float CardHeight = 100f;
        private const float ScrollInset = 3f;

        private static readonly Color TextColor = new Color32(233, 239, 249, 255);
        private static readonly Color MutedColor = new Color32(178, 193, 215, 255);
        private static readonly Color AccentColor = new Color32(147, 198, 255, 255);

        private readonly Action<string> openCase;
        private readonly float preferredCardWidth;
        private readonly float columnGap;
        private readonly List<(string Id, string SearchText, Button Button)> entries = new();
        private readonly List<Button> visibleButtons = new();
        private readonly InputField search;
        private readonly ScrollRect scroll;
        private readonly GridLayoutGroup grid;
        private readonly RectTransform gridRect;
        private readonly Text noResults;
        private Vector2 lastViewportSize;
        private GameObject lastFocused;

        internal RectTransform Root { get; }

        internal UxUiAcceptanceCatalog(Transform parent, Action<string> openCase, float preferredCardWidth, float columnGap)
        {
            this.openCase = openCase;
            this.preferredCardWidth = preferredCardWidth;
            this.columnGap = columnGap;
            Root = CreateRect(parent, "Catalog");
            Stretch(Root);
            var layout = Root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 12);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var title = CreateText(Root, "CatalogTitle", "展示案例", 22, TextColor, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
            search = CreateSearch();

            var scrollRect = CreateRect(Root, "CatalogScroll");
            scrollRect.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            scroll.viewport = CreateRect(scrollRect, "Viewport");
            Stretch(scroll.viewport);
            scroll.viewport.gameObject.AddComponent<RectMask2D>();
            var scrollHitArea = scroll.viewport.gameObject.AddComponent<Image>();
            scrollHitArea.color = Color.clear;

            gridRect = CreateRect(scroll.viewport, "ModuleGrid");
            gridRect.anchorMin = new Vector2(0f, 1f);
            gridRect.anchorMax = Vector2.one;
            gridRect.pivot = new Vector2(0.5f, 1f);
            gridRect.sizeDelta = Vector2.zero;
            gridRect.anchoredPosition = Vector2.zero;
            scroll.content = gridRect;

            grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset((int)ScrollInset, (int)ScrollInset, (int)ScrollInset, (int)ScrollInset);
            grid.spacing = new Vector2(columnGap, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.childAlignment = TextAnchor.UpperLeft;

            noResults = CreateText(scrollRect, "NoResults", "", 17, MutedColor, TextAnchor.UpperLeft);
            Stretch(noResults.rectTransform, ScrollInset);
            search.onValueChanged.AddListener(value => Filter(value, resetScroll: true));
        }

        internal void Refresh(string targetId)
        {
            var focusedId = GetFocusedCard()?.name;
            ClearChildren(gridRect);
            entries.Clear();
            foreach (var demoCase in UxUiModuleRegistry.AllCases)
            {
                var id = demoCase.Id;
                var description = UxUiModuleRegistry.GetDescription(id);
                var current = string.Equals(id, targetId, StringComparison.OrdinalIgnoreCase);
                var button = AddCard(demoCase, description, current);
                entries.Add((id, $"{demoCase.DisplayName} {description} {id}", button));
            }
            Filter(search.text, resetScroll: false);
            if (focusedId != null)
                FocusCard(focusedId);
        }

        internal void Reflow()
        {
            var viewportSize = scroll.viewport.rect.size;
            if (viewportSize.x <= 0f || viewportSize == lastViewportSize)
            {
                RevealFocusedCard();
                return;
            }

            lastViewportSize = viewportSize;
            var available = Mathf.Max(0f, viewportSize.x - grid.padding.horizontal);
            var columns = Mathf.Clamp(Mathf.FloorToInt((available + columnGap) / (preferredCardWidth + columnGap)), 1, Mathf.Max(1, visibleButtons.Count));
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(Mathf.Max(0f, (available - (columns - 1) * columnGap) / columns), CardHeight);
            var rows = Mathf.CeilToInt((float)visibleButtons.Count / columns);
            var height = rows * CardHeight + Mathf.Max(0, rows - 1) * grid.spacing.y + grid.padding.vertical;
            gridRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
            WireKeyboardNavigation(columns);
            lastFocused = null;
            RevealFocusedCard();
        }

        private void WireKeyboardNavigation(int columns)
        {
            search.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnDown = visibleButtons.Count == 0 ? null : visibleButtons[0]
            };

            for (var index = 0; index < visibleButtons.Count; index++)
            {
                visibleButtons[index].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = index > 0 ? visibleButtons[index - 1] : null,
                    selectOnRight = index + 1 < visibleButtons.Count ? visibleButtons[index + 1] : null,
                    selectOnUp = index >= columns ? visibleButtons[index - columns] : search,
                    selectOnDown = index + columns < visibleButtons.Count ? visibleButtons[index + columns] : null
                };
            }
        }

        internal void FocusFirstCard()
        {
            if (visibleButtons.Count > 0)
                visibleButtons[0].Select();
        }

        internal void FocusCard(string id)
        {
            if (EventSystem.current == null || !Root.gameObject.activeInHierarchy)
                return;
            foreach (var entry in entries)
            {
                if (entry.Button.gameObject.activeSelf && string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    entry.Button.Select();
                    RevealFocusedCard();
                    return;
                }
            }
            search.Select();
        }

        private InputField CreateSearch()
        {
            var rect = CreateRect(Root, "Search demos");
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
            var background = rect.gameObject.AddComponent<Image>();
            background.color = new Color32(31, 42, 57, 255);
            var input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = background;
            input.lineType = InputField.LineType.SingleLine;

            var value = CreateText(rect, "SearchText", "", 18, TextColor, TextAnchor.MiddleLeft);
            var placeholder = CreateText(rect, "Placeholder", "搜尋展示案例…", 18, MutedColor, TextAnchor.MiddleLeft);
            foreach (var label in new[] { value, placeholder })
            {
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(12f, 2f);
                label.rectTransform.offsetMax = new Vector2(-48f, -2f);
                label.supportRichText = false;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
            }

            input.textComponent = value;
            input.placeholder = placeholder;
            var colors = input.colors;
            colors.highlightedColor = new Color32(209, 228, 255, 255);
            colors.selectedColor = new Color32(209, 228, 255, 255);
            input.colors = colors;

            var clear = ButtonAddon.Create(rect, label: "×", showIcon: false, size: 44f);
            clear.gameObject.name = "Clear search";
            clear.labelText.color = new Color32(23, 31, 43, 255);
            clear.labelText.fontSize = 18;
            var clearRect = (RectTransform)clear.transform;
            clearRect.anchorMin = new Vector2(1f, .5f);
            clearRect.anchorMax = clearRect.anchorMin;
            clearRect.pivot = new Vector2(1f, .5f);
            clearRect.anchoredPosition = new Vector2(-1f, 0f);
            clearRect.sizeDelta = new Vector2(44f, 44f);
            clear.ui.onClick.AddListener(() =>
            {
                input.text = string.Empty;
                input.Select();
            });
            clear.gameObject.SetActive(false);
            input.onValueChanged.AddListener(valueText => clear.gameObject.SetActive(!string.IsNullOrEmpty(valueText)));
            return input;
        }

        private Button AddCard(IUxUiModuleDemoCase demoCase, string description, bool current)
        {
            var card = CreateRect(gridRect, demoCase.Id);
            var background = card.gameObject.AddComponent<Image>();
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.colors = new ColorBlock
            {
                normalColor = current ? new Color32(35, 64, 97, 255) : new Color32(30, 40, 55, 255),
                highlightedColor = new Color32(52, 71, 97, 255),
                selectedColor = new Color32(55, 83, 119, 255),
                pressedColor = new Color32(64, 99, 142, 255),
                disabledColor = new Color32(32, 38, 50, 255),
                colorMultiplier = 1f,
                fadeDuration = .08f
            };
            button.onClick.AddListener(() => openCase(demoCase.Id));

            var subtitle = current && string.IsNullOrEmpty(description) ? "本輪驗收目標" : description;
            if (current && !string.IsNullOrEmpty(description))
                subtitle = $"本輪驗收目標 · {description}";

            var title = CreateText(card, "Title", demoCase.DisplayName, 19, TextColor, TextAnchor.MiddleLeft);
            Stretch(title.rectTransform, 12f);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 14;
            title.resizeTextMaxSize = 19;
            if (!string.IsNullOrEmpty(subtitle))
            {
                title.rectTransform.anchorMin = new Vector2(0f, .38f);
                title.rectTransform.offsetMin = new Vector2(12f, 0f);
                title.rectTransform.offsetMax = new Vector2(-12f, -7f);
                var detail = CreateText(card, "Description", subtitle, 14, current ? AccentColor : MutedColor, TextAnchor.UpperLeft);
                Stretch(detail.rectTransform);
                detail.rectTransform.anchorMax = new Vector2(1f, .38f);
                detail.rectTransform.offsetMin = new Vector2(12f, 6f);
                detail.rectTransform.offsetMax = new Vector2(-12f, 0f);
            }

            return button;
        }

        private GameObject GetFocusedCard()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            return selected != null && selected.transform.parent == gridRect ? selected : null;
        }

        private void RevealFocusedCard()
        {
            if (!Root.gameObject.activeInHierarchy)
                return;
            var selected = GetFocusedCard();
            if (selected == lastFocused)
                return;
            lastFocused = selected;
            if (selected == null)
                return;
            var card = (RectTransform)selected.transform;
            var viewport = scroll.viewport;
            var visible = viewport.rect;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, card);
            if (bounds.min.y < visible.yMin)
                gridRect.anchoredPosition += Vector2.up * (visible.yMin - bounds.min.y);
            else if (bounds.max.y > visible.yMax)
                gridRect.anchoredPosition -= Vector2.up * (bounds.max.y - visible.yMax);
            scroll.StopMovement();
        }

        private void Filter(string query, bool resetScroll)
        {
            var focused = GetFocusedCard();
            var words = query.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            visibleButtons.Clear();
            foreach (var entry in entries)
            {
                var matches = Array.TrueForAll(words, word => entry.SearchText.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
                entry.Button.gameObject.SetActive(matches);
                if (matches)
                    visibleButtons.Add(entry.Button);
            }

            if (focused != null && !focused.activeSelf && Root.gameObject.activeInHierarchy)
                search.Select();

            noResults.gameObject.SetActive(visibleButtons.Count == 0);
            noResults.text = entries.Count == 0 ? "目前沒有展示案例。" : "找不到符合的展示案例。";
            lastViewportSize = Vector2.zero;
            Reflow();
            if (resetScroll)
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1f;
            }
        }
    }
}
