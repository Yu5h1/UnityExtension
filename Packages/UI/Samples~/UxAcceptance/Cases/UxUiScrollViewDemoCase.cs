using UnityEngine;
using UnityEngine.UI;
using static Yu5h1Lib.UI.Acceptance.UxUiAcceptanceHost;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Shows both axes and loop mode on one ScrollView with recycled items.</summary>
    public sealed class UxUiScrollViewDemoCase : IUxUiModuleDemoCase
    {
        private const int ItemCount = 1000;
        private RectTransform root;
        private RectTransform itemTemplate;
        private ScrollView view;
        private ScrollViewScrollbar scrollbar;
        private ButtonAddon axisButton;
        private ButtonAddon loopButton;
        private Text counters;

        public string Id => "scrollview";
        public string DisplayName => "ScrollView 捲動視窗";
        public string ModuleSpec => "Yu5h1Lib.UX/Collections/ScrollView";

        public void Mount(RectTransform container)
        {
            Unmount();
            root = CreateRect(container, "ScrollViewDemo");
            Stretch(root, 12f);
            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var controls = CreateRect(root, "Controls");
            controls.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
            var toolbar = controls.gameObject.AddComponent<HorizontalLayoutGroup>();
            toolbar.childControlWidth = toolbar.childControlHeight = true;
            toolbar.spacing = 8f;
            axisButton = AddControl(controls, "Direction", "方向：垂直", () => SetAxis(!view.horizontal));
            loopButton = AddControl(controls, "Loop", "循環：關閉", ToggleLoop);

            counters = CreateText(root, "Counters", "", 15, Palette.SecondaryText, TextAnchor.MiddleLeft);
            counters.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            var frame = CreateRect(root, "ScrollView");
            frame.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            view = frame.gameObject.AddComponent<ScrollView>();
            view.ContentUpdateBehavior = ContentUpdateBehavior.PreservePosition;
            view.movementType = ScrollRect.MovementType.Clamped;
            view.scrollSensitivity = 32f;
            view.viewport = CreateRect(frame, "Viewport");
            Stretch(view.viewport);
            view.viewport.gameObject.AddComponent<Image>().color = Palette.PanelBackground;
            view.viewport.gameObject.AddComponent<RectMask2D>();
            view.content = CreateRect(view.viewport, "Items");
            scrollbar = CreateScrollbar(frame);
            itemTemplate = CreateItemTemplate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            SetAxis(false);
            view.onValueChanged.AddListener(_ => RefreshCounters());
        }

        public void Unmount()
        {
            if (root == null)
                return;
            root.gameObject.SetActive(false);
            root.SetParent(null, false);
            if (Application.isPlaying)
                Object.Destroy(root.gameObject);
            else
                Object.DestroyImmediate(root.gameObject);
            root = null;
        }

        private void SetAxis(bool horizontal)
        {
            view.ScrollToOffset(0f);
            view.horizontalScrollbar = null;
            view.verticalScrollbar = null;
            view.horizontal = horizontal;
            view.vertical = !horizontal;

            var track = (RectTransform)scrollbar.transform;
            track.anchorMin = horizontal ? Vector2.zero : Vector2.right;
            track.anchorMax = horizontal ? Vector2.right : Vector2.one;
            track.offsetMin = horizontal ? new Vector2(0f, 0f) : new Vector2(-20f, 0f);
            track.offsetMax = horizontal ? new Vector2(0f, 20f) : Vector2.zero;
            scrollbar.direction = horizontal ? Scrollbar.Direction.LeftToRight : Scrollbar.Direction.BottomToTop;
            if (horizontal)
                view.horizontalScrollbar = scrollbar;
            else
                view.verticalScrollbar = scrollbar;

            view.SetItems(itemTemplate, ItemCount, horizontal ? 128f : 48f, BindItem);
            axisButton.label = horizontal ? "方向：水平" : "方向：垂直";
            RefreshCounters();
        }

        private void ToggleLoop()
        {
            view.Loop = !view.Loop;
            loopButton.label = view.Loop ? "循環：開啟" : "循環：關閉";
            RefreshCounters();
        }

        private void RefreshCounters() =>
            counters.text = $"{ItemCount:N0} 筆    已建立 {view.CreatedItemCount}    使用中 {view.content.childCount}";

        private RectTransform CreateItemTemplate()
        {
            var template = CreateRect(root, "Item Template");
            template.gameObject.SetActive(false);
            var surface = CreateRect(template, "Surface");
            Stretch(surface, 4f);
            var image = surface.gameObject.AddComponent<Image>();
            image.color = Palette.Hover;
            image.raycastTarget = false;
            var label = CreateText(surface, "Label", "", 17, Palette.ItemText, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            return template;
        }

        private void BindItem(RectTransform item, int index)
        {
            item.name = $"Item {index + 1}";
            var surface = (RectTransform)item.GetChild(0);
            surface.offsetMin = view.horizontal ? new Vector2(4f, 22f) : new Vector2(4f, 4f);
            surface.offsetMax = view.horizontal ? new Vector2(-4f, -4f) : new Vector2(-22f, -4f);
            surface.GetComponentInChildren<Text>(true).text = $"項目 {index + 1:N0}";
        }

        private static ScrollViewScrollbar CreateScrollbar(RectTransform frame)
        {
            var track = CreateRect(frame, "Scrollbar");
            track.gameObject.AddComponent<Image>().color = Palette.PageBackground;
            var slidingArea = CreateRect(track, "Sliding Area");
            Stretch(slidingArea, 4f);
            var handle = CreateRect(slidingArea, "Handle");
            Stretch(handle);
            var image = handle.gameObject.AddComponent<Image>();
            image.color = Palette.Accent;
            var bar = track.gameObject.AddComponent<ScrollViewScrollbar>();
            bar.targetGraphic = image;
            bar.handleRect = handle;
            return bar;
        }

        private static ButtonAddon AddControl(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction action)
        {
            var button = ButtonAddon.Create(parent, label: label, showIcon: false, size: 36f);
            button.name = name;
            Palette.ApplyButton(button.ui, button.labelText);
            button.ui.onClick.AddListener(action);
            return button;
        }
    }
}
