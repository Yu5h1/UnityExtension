using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Yu5h1Lib.UI.Acceptance.UxUiAcceptanceHost;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Lets either settings panel exchange a UX Theme preference with a local JSON file.</summary>
    [DefaultExecutionOrder(100)]
    internal sealed class SettingsThemeExchangeDialog : MonoBehaviour
    {
        private readonly List<ButtonAddon> buttons = new();
        private readonly List<Selectable> focusOrder = new();
        private Func<string, string> import;
        private Func<string, string> export;
        private Action onHide;
        private GameObject previousSelection;
        private Image shade;
        private Image surface;
        private Text title;
        private Text instructions;
        private Text status;
        private InputField path;

        internal static SettingsThemeExchangeDialog Create(RectTransform parent, Func<string, string> import,
            Func<string, string> export, Action onHide)
        {
            var root = CreateRect(parent, "Theme exchange dialog");
            root.gameObject.SetActive(false);
            Stretch(root);
            var dialog = root.gameObject.AddComponent<SettingsThemeExchangeDialog>();
            dialog.import = import;
            dialog.export = export;
            dialog.onHide = onHide;
            dialog.Build(root);
            return dialog;
        }

        internal void Show(UguiControlPalette palette)
        {
            if (!gameObject.activeSelf)
                previousSelection = EventSystem.current?.currentSelectedGameObject;

            status.text = "";
            ApplyPalette(palette);
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            path.Select();
        }

        internal void ApplyPalette(UguiControlPalette palette)
        {
            shade.color = palette.Overlay;
            surface.color = palette.PanelBackground;
            title.color = palette.PanelHeading;
            instructions.color = status.color = ReadableText(palette);
            palette.ApplyField(path);
            foreach (var button in buttons)
                palette.ApplyButton(button.ui, button.labelText);
        }

        internal void Hide()
        {
            if (!gameObject.activeSelf)
                return;

            var events = EventSystem.current;
            var selected = events?.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(transform))
                events.SetSelectedGameObject(null);

            gameObject.SetActive(false);
            onHide?.Invoke();
            if (previousSelection != null && previousSelection.TryGetComponent<Selectable>(out var target)
                && target.IsActive() && target.IsInteractable())
                target.Select();
            previousSelection = null;
        }

        private void Build(RectTransform root)
        {
            shade = root.gameObject.AddComponent<Image>();
            var panel = CreateRect(root, "Theme exchange panel");
            panel.anchorMin = new Vector2(0f, 0.5f);
            panel.anchorMax = new Vector2(1f, 0.5f);
#if UNITY_EDITOR
            panel.sizeDelta = new Vector2(-48f, 264f);
#else
            panel.sizeDelta = new Vector2(-48f, 224f);
#endif
            surface = panel.gameObject.AddComponent<Image>();

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var header = CreateRow(panel, "Theme exchange header");
            title = CreateText(header, "Theme exchange title", "主題 JSON", 19, Color.white, TextAnchor.MiddleLeft);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var close = CreateButton(header, "Close theme exchange dialog", "關閉", 58f, Hide);
            instructions = CreateText(panel, "Theme exchange instructions", "輸入完整檔案路徑。匯出請使用新的檔名。",
                15, Color.white, TextAnchor.MiddleLeft);
            instructions.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            path = CreatePathField(panel);
            focusOrder.Add(path);

#if UNITY_EDITOR
            var browse = CreateRow(panel, "Theme exchange file pickers");
            focusOrder.Add(CreateButton(browse, "Choose theme import file", "選擇匯入檔", 120f,
                () => SelectPath(UnityEditor.EditorUtility.OpenFilePanel("選擇 Theme v1 JSON", "", "json"))).ui);
            focusOrder.Add(CreateButton(browse, "Choose theme export path", "選擇匯出位置", 120f,
                () => SelectPath(UnityEditor.EditorUtility.SaveFilePanel("選擇匯出位置", "", "theme", "json"))).ui);
#endif
            var actions = CreateRow(panel, "Theme exchange actions");
            focusOrder.Add(CreateButton(actions, "Import theme JSON", "匯入", 92f, () => Run(import)).ui);
            focusOrder.Add(CreateButton(actions, "Export theme JSON", "匯出", 92f, () => Run(export)).ui);
            status = CreateText(panel, "Theme exchange status", "", 15, Color.white, TextAnchor.UpperLeft);
            status.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            focusOrder.Add(close.ui);
            WireNavigation();
        }

        private void Run(Func<string, string> action) => status.text = action(path.text);

#if UNITY_EDITOR
        private void SelectPath(string selected)
        {
            if (string.IsNullOrEmpty(selected))
                return;

            path.SetTextWithoutNotify(selected);
            status.text = "";
        }
#endif

        private static Color ReadableText(UguiControlPalette palette)
        {
            var background = palette.PanelBackground;
            if (SettingsCustomPalette.Contrast(palette.SecondaryText, background) >= 4.5f)
                return palette.SecondaryText;

            return SettingsCustomPalette.Contrast(Color.white, background)
                >= SettingsCustomPalette.Contrast(Color.black, background) ? Color.white : Color.black;
        }

        private static InputField CreatePathField(RectTransform parent)
        {
            var rect = CreateRect(parent, "Theme JSON path");
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
            var field = rect.gameObject.AddComponent<InputField>();
            field.targetGraphic = rect.gameObject.AddComponent<Image>();
            var value = CreateText(rect, "Theme JSON path value", "", 15, Color.white, TextAnchor.MiddleLeft);
            Stretch(value.rectTransform, 7f);
            field.textComponent = value;
            var placeholder = CreateText(rect, "Theme JSON path placeholder", "Theme v1 JSON 檔案路徑",
                15, Color.white, TextAnchor.MiddleLeft);
            Stretch(placeholder.rectTransform, 7f);
            field.placeholder = placeholder;
            field.lineType = InputField.LineType.SingleLine;
            return field;
        }

        private static RectTransform CreateRow(RectTransform parent, string name)
        {
            var row = CreateRect(parent, name);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
            return row;
        }

        private ButtonAddon CreateButton(RectTransform parent, string name, string label, float width, UnityAction action)
        {
            var button = ButtonAddon.Create(parent, label: label, showIcon: false, size: 32f);
            button.gameObject.name = name;
            button.labelText.fontSize = 15;
            button.GetComponent<LayoutElement>().preferredWidth = width;
            button.ui.onClick.AddListener(action);
            buttons.Add(button);
            return button;
        }

        private void Update()
        {
            if (InputHandler.GetKeyDown(KeyCode.Escape))
                Hide();
            else if (InputHandler.GetKeyDown(KeyCode.Tab))
                MoveFocus(InputHandler.GetKey(KeyCode.LeftShift) || InputHandler.GetKey(KeyCode.RightShift));
        }

        private void WireNavigation()
        {
            for (var index = 0; index < focusOrder.Count; index++)
            {
                var previous = focusOrder[(index + focusOrder.Count - 1) % focusOrder.Count];
                var next = focusOrder[(index + 1) % focusOrder.Count];
                focusOrder[index].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = previous,
                    selectOnDown = next
                };
            }
        }

        private void MoveFocus(bool backwards)
        {
            var events = EventSystem.current;
            if (events == null)
                return;

            var selected = events.currentSelectedGameObject;
            int index = focusOrder.FindIndex(control => control.gameObject == selected);
            if (index < 0)
                index = backwards ? 0 : -1;
            focusOrder[(index + (backwards ? -1 : 1) + focusOrder.Count) % focusOrder.Count].Select();
        }
    }
}
