using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Yu5h1Lib.UI.SettingsLayout;

namespace Yu5h1Lib.UI
{
    /// <summary>Changes selection without recreating hosted content or resetting its fields.</summary>
    public sealed class Tabs : MonoBehaviour
    {
        public enum Direction { Row, Column }

        public sealed class Item
        {
            public string Id { get; }
            public string Label { get; }
            public RectTransform Content { get; }

            public Item(string id, string label, RectTransform content = null)
            {
                Id = id;
                Label = label;
                Content = content;
            }
        }

        private static readonly Regex idFormat = new(@"\A[a-z][a-z0-9-]*\z");
        private readonly List<(Item Item, ButtonAddon Button, Image Line)> entries = new();
        private RectTransform rail;
        private RectTransform content;
        private ScrollView scroll;
        private Text empty;
        private RectTransform row;
        private RectTransform column;
        private UguiControlPalette palette;
        private Direction direction;
        private int selected = -1;
        private bool interactable = true;
        private bool selectionOnly;

        public event Action<string> Changed;
        internal RectTransform ContentRoot => content;
        public string SelectedId => selected < 0 ? null : entries[selected].Item.Id;
        public Selectable ActiveButton => selected < 0 ? null : entries[selected].Button.ui;
        public Direction Orientation
        {
            get => direction;
            set
            {
                if (direction == value)
                    return;
                direction = value;
                ApplyDirection();
            }
        }

        public bool Interactable
        {
            get => interactable;
            set
            {
                interactable = value;
                foreach (var entry in entries)
                    entry.Button.ui.interactable = value;
            }
        }

        public static Tabs Create(Transform parent, UguiControlPalette palette = null) =>
            Create(parent, palette, false);

        internal static Tabs Create(Transform parent, UguiControlPalette palette, bool selectionOnly)
        {
            var root = Rect(parent, selectionOnly ? "Group choices" : "Tabs");
            var tabs = root.gameObject.AddComponent<Tabs>();
            tabs.palette = palette ?? UguiControlPalette.Light;
            tabs.selectionOnly = selectionOnly;
            tabs.content = Rect(root, "Tab content");
            tabs.empty = MakeText(tabs.content, "Empty", "尚無設定分類。");
            tabs.empty.color = tabs.palette.ItemText;
            Stretch(tabs.empty.rectTransform);
            tabs.scroll = Scroll(Rect(root, "Tab strip"), out tabs.row);
            tabs.column = Rect(tabs.scroll.viewport, "Column");
            Layout<HorizontalLayoutGroup>(tabs.row, 2);
            Layout<VerticalLayoutGroup>(tabs.column, 2).childForceExpandWidth = true;
            tabs.direction = selectionOnly ? Direction.Column : Direction.Row;
            tabs.ApplyDirection();
            return tabs;
        }

        internal static void ValidateItems(IReadOnlyList<Item> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var contents = new HashSet<RectTransform>();
            foreach (var item in items)
            {
                if (item == null || item.Id == null || !idFormat.IsMatch(item.Id)
                    || string.IsNullOrWhiteSpace(item.Label) || !ids.Add(item.Id)
                    || (item.Content != null && !contents.Add(item.Content)))
                    throw new ArgumentException("Each choice requires a unique ID, a label and independent content.", nameof(items));
            }
        }

        /// <summary>Replaces tab definitions. Removed content is hidden and remains owned by the caller.</summary>
        public void SetItems(IReadOnlyList<Item> items)
        {
            ValidateItems(items);
            var previous = SelectedId;
            foreach (var entry in entries)
            {
                entry.Button.gameObject.SetActive(false);
                entry.Button.transform.SetParent(null, false);
                if (Application.isPlaying) Destroy(entry.Button.gameObject);
                else DestroyImmediate(entry.Button.gameObject);
                if (entry.Item.Content != null)
                    entry.Item.Content.gameObject.SetActive(false);
            }
            entries.Clear();
            selected = -1;
            foreach (var item in items)
            {
                var index = entries.Count;
                var button = MakeButton(rail, item.Id, item.Label);
                button.ui.onClick.AddListener(() => SetSelection(index, true, true));
                button.ui.navigation = new Navigation { mode = Navigation.Mode.Explicit };
                button.gameObject.AddComponent<TabNavigation>().Initialize(this);
                var line = Rect(button.transform, "Selection");
                var image = line.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                Size(line).ignoreLayout = true;
                ConfigureLine(line);
                if (item.Content != null)
                {
                    item.Content.SetParent(content, false);
                    Stretch(item.Content);
                }
                ApplyEntryPalette(button, image);
                entries.Add((item, button, image));
            }
            var retained = entries.FindIndex(entry => entry.Item.Id == previous);
            selected = retained >= 0 ? retained : (entries.Count > 0 ? 0 : -1);
            Interactable = interactable;
            UpdateSelection();
        }

        internal void ApplyPalette(UguiControlPalette value)
        {
            palette = value;
            empty.color = palette.ItemText;
            foreach (var entry in entries)
                ApplyEntryPalette(entry.Button, entry.Line);
        }

        private void ApplyEntryPalette(ButtonAddon button, Image line)
        {
            palette.ApplyButton(button.ui, button.labelText);
            line.color = palette.Accent;
        }

        public bool Select(string id, bool focus = false)
        {
            var index = entries.FindIndex(entry => entry.Item.Id == id);
            return index >= 0 && SetSelection(index, focus, false);
        }

        public bool Navigate(MoveDirection movement) => (direction, movement) switch
        {
            (Direction.Row, MoveDirection.Left) or (Direction.Column, MoveDirection.Up) => Move(-1),
            (Direction.Row, MoveDirection.Right) or (Direction.Column, MoveDirection.Down) => Move(1),
            _ => false
        };

        public bool FocusEdge(bool last) => entries.Count > 0 && SetSelection(last ? entries.Count - 1 : 0, true, true);

        private bool Move(int delta) => selected >= 0 && SetSelection((selected + delta + entries.Count) % entries.Count, true, true);

        private bool SetSelection(int index, bool focus, bool fromUser)
        {
            if (fromUser && !interactable)
                return false;
            var changed = selected != index;
            selected = index;
            UpdateSelection();
            if (focus && interactable)
                entries[index].Button.ui.Select();
            if (changed)
                Changed?.Invoke(SelectedId);
            return true;
        }

        private void UpdateSelection()
        {
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                var active = index == selected;
                entry.Line.gameObject.SetActive(active);
                if (entry.Item.Content != null)
                    entry.Item.Content.gameObject.SetActive(active);
            }
            empty.gameObject.SetActive(selected < 0);
            RevealSelection();
        }

        private void RevealSelection()
        {
            if (selected < 0 || !isActiveAndEnabled || scroll.viewport.rect.width <= 0)
                return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rail);
            scroll.RefreshContent();
            var item = (RectTransform)entries[selected].Button.transform;
            var viewport = scroll.viewport;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item);
            var rect = viewport.rect;
            var offset = direction == Direction.Row
                ? bounds.min.x < rect.xMin ? bounds.min.x - rect.xMin : Mathf.Max(0, bounds.max.x - rect.xMax)
                : bounds.max.y > rect.yMax ? rect.yMax - bounds.max.y : Mathf.Max(0, rect.yMin - bounds.min.y);
            if (Mathf.Abs(offset) > .5f)
                scroll.ScrollToOffset(scroll.Offset + offset);
        }

        private void ApplyDirection()
        {
            var horizontal = direction == Direction.Row;
            rail = horizontal ? row : column;
            rail.gameObject.SetActive(true);
            foreach (var entry in entries)
                entry.Button.transform.SetParent(rail, false);
            (horizontal ? column : row).gameObject.SetActive(false);
            scroll.content = rail;
            ConfigureScroll(scroll, horizontal);

            var strip = (RectTransform)scroll.transform;
            Stretch(strip);
            Stretch(content);
            if (selectionOnly)
                content.gameObject.SetActive(false);
            else if (horizontal)
            {
                strip.anchorMin = new Vector2(0, 1);
                strip.offsetMin = new Vector2(0, -38);
                content.offsetMax = new Vector2(0, -40);
            }
            else
                PositionColumn(strip);

            foreach (var entry in entries)
                ConfigureLine(entry.Line.rectTransform);
        }

        private void PositionColumn(RectTransform strip)
        {
            var width = Mathf.Max(80, ((RectTransform)transform).rect.width * .28f);
            strip.anchorMax = new Vector2(0, 1);
            strip.offsetMax = new Vector2(width, 0);
            content.offsetMin = new Vector2(width + 4, 0);
        }

        private void ConfigureLine(RectTransform line)
        {
            Stretch(line);
            if (direction == Direction.Row)
            {
                line.anchorMin = new Vector2(0, 1);
                line.offsetMin = new Vector2(0, -3);
            }
            else
            {
                line.anchorMax = new Vector2(0, 1);
                line.offsetMax = new Vector2(3, 0);
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (rail != null && !selectionOnly && direction == Direction.Column)
                PositionColumn((RectTransform)scroll.transform);
        }

        private void Update()
        {
            if (interactable && ActiveButton != null
                && EventSystem.current?.currentSelectedGameObject == ActiveButton.gameObject)
            {
                if (InputHandler.GetKeyDown(KeyCode.Home))
                    FocusEdge(false);
                else if (InputHandler.GetKeyDown(KeyCode.End))
                    FocusEdge(true);
            }
        }
    }
}
