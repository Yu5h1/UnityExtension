using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>Native uGUI construction shared by the settings surfaces.</summary>
    internal static class SettingsLayout
    {
        internal static RectTransform Rect(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        internal static T Layout<T>(RectTransform rect, float spacing = 0f) where T : HorizontalOrVerticalLayoutGroup
        {
            var layout = rect.gameObject.AddComponent<T>();
            layout.spacing = spacing;
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = layout.childForceExpandWidth = false;
            return layout;
        }

        internal static LayoutElement Size(Component element, float width = -1, float height = -1,
            float flexibleWidth = -1, float flexibleHeight = -1)
        {
            var size = element.GetComponent<LayoutElement>() ?? element.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = width;
            size.preferredHeight = height;
            size.flexibleWidth = flexibleWidth;
            size.flexibleHeight = flexibleHeight;
            return size;
        }

        internal static ButtonAddon MakeButton(Transform parent, string name, string label)
        {
            var button = ButtonAddon.Create(parent, label: label, showIcon: false, size: 36f);
            button.name = name;
            button.labelText.fontSize = 15;
            button.labelText.supportRichText = false;
            var size = Size(button, Mathf.Max(40, button.labelText.preferredWidth + 20), 36);
            size.minWidth = 0;
            return button;
        }

        internal static Text MakeText(Transform parent, string name, string label)
        {
            var text = Rect(parent, name).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 15;
            text.text = label;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleLeft;
            return text;
        }

        internal static ScrollView Scroll(RectTransform frame, out RectTransform content)
        {
            var view = frame.gameObject.AddComponent<ScrollView>();
            view.movementType = ScrollRect.MovementType.Clamped;
            view.scrollSensitivity = 25;
            var viewport = Rect(frame, "Viewport");
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            content = Rect(viewport, "Content");
            view.viewport = viewport;
            view.content = content;
            return view;
        }

        internal static void ConfigureScroll(ScrollView view, bool horizontal)
        {
            view.horizontal = horizontal;
            view.vertical = !horizontal;
            var content = view.content;
            content.pivot = new Vector2(0, 1);
            content.anchorMin = horizontal ? new Vector2(0, 0) : new Vector2(0, 1);
            content.anchorMax = horizontal ? new Vector2(0, 1) : Vector2.one;
            content.anchoredPosition = content.sizeDelta = Vector2.zero;
            var fit = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = horizontal ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
