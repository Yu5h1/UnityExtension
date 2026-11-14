// Edits the acceptance sample's live custom palette through native uGUI fields.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Yu5h1Lib.UI.Acceptance.UxUiAcceptanceHost;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Shows the eight UX Theme colors and applies valid edits without leaving the settings panel.</summary>
    [DefaultExecutionOrder(100)]
    internal sealed class SettingsCustomThemeEditor : MonoBehaviour
    {
        private const float MinimumTextContrast = 4.5f;
        private static readonly string[] RoleLabels =
        {
            "頁面背景", "面板背景", "頁面標題", "面板標題",
            "項目文字", "次要文字", "邊框", "強調色"
        };

        private readonly List<(string Role, Text Label, Image Swatch, InputField Field)> rows = new();
        private Selectable[] focusOrder;
        private GameObject previousSelection;
        private Action<string, string> setColor;
        private Action<bool> setBase;
        private Action onHide;
        private SettingsCustomColors colors;
        private Image backdrop;
        private Image surface;
        private Image divider;
        private Text heading;
        private Text inputWarning;
        private Text contrastWarning;
        private ButtonAddon baseButton;
        private ButtonAddon close;

        internal bool IsOpen => gameObject.activeSelf;

        internal static SettingsCustomThemeEditor Create(RectTransform parent, Action<string, string> setColor,
            Action<bool> setBase, Action onHide)
        {
            var root = CreateRect(parent, "Custom theme editor");
            root.gameObject.SetActive(false);
            Stretch(root);
            var editor = root.gameObject.AddComponent<SettingsCustomThemeEditor>();
            editor.setColor = setColor;
            editor.setBase = setBase;
            editor.onHide = onHide;
            editor.Build(root);
            return editor;
        }

        internal void Show(SettingsCustomColors colors, UguiControlPalette palette)
        {
            if (!IsOpen)
                previousSelection = EventSystem.current?.currentSelectedGameObject;
            this.colors = colors;
            foreach (var row in rows)
                row.Field.SetTextWithoutNotify(colors[row.Role]);
            inputWarning.text = "";
            ApplyPalette(palette);
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            close.ui.Select();
        }

        internal void ApplyPalette(UguiControlPalette palette)
        {
            backdrop.color = palette.Overlay;
            surface.color = palette.PanelBackground;
            if (palette.SourceTheme.TryGet<Color>("border", out var border))
                divider.color = border;
            heading.color = palette.Heading;
            Color warningColor = palette.SecondaryText;
            if (SettingsCustomPalette.Contrast(warningColor, surface.color) < MinimumTextContrast)
                warningColor = SettingsCustomPalette.Contrast(Color.white, surface.color)
                    >= SettingsCustomPalette.Contrast(Color.black, surface.color) ? Color.white : Color.black;
            contrastWarning.color = warningColor;
            inputWarning.color = warningColor;
            if (colors != null)
            {
                baseButton.labelText.text = colors.IsDark ? "基底　暗色" : "基底　亮色";
                foreach (var row in rows)
                    row.Swatch.color = SettingsCustomPalette.ReadHex(colors[row.Role]);
                RefreshContrastWarning(palette);
            }
            palette.ApplyButton(baseButton.ui, baseButton.labelText);
            palette.ApplyButton(close.ui, close.labelText);
            foreach (var row in rows)
            {
                row.Label.color = palette.ItemText;
                palette.ApplyField(row.Field);
            }
        }

        internal void Hide()
        {
            if (!IsOpen)
                return;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
                && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
                EventSystem.current.SetSelectedGameObject(null);
            gameObject.SetActive(false);
            onHide?.Invoke();
            if (previousSelection != null && previousSelection.TryGetComponent<Selectable>(out var target)
                && target.IsActive() && target.IsInteractable())
                target.Select();
            previousSelection = null;
        }

        private void Build(RectTransform root)
        {
            backdrop = root.gameObject.AddComponent<Image>();
            var dialog = CreateRect(root, "Custom theme dialog");
            dialog.anchorMin = new Vector2(0f, 0.5f);
            dialog.anchorMax = new Vector2(1f, 0.5f);
            dialog.sizeDelta = new Vector2(-48f, 438f);
            surface = dialog.gameObject.AddComponent<Image>();
            var layout = dialog.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var header = CreateRow(dialog, "Custom theme header", 34f);
            heading = CreateText(header, "Custom theme title", "自訂主題", 19, Color.white, TextAnchor.MiddleLeft);
            heading.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            baseButton = CreateButton(header, "Custom theme base", "基底　亮色", 102f, () => setBase(!colors.IsDark));
            close = CreateButton(header, "Close custom theme editor", "完成", 58f, Hide);
            var line = CreateRect(dialog, "Custom theme separator");
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 1f;
            divider = line.gameObject.AddComponent<Image>();
            divider.raycastTarget = false;

            for (var index = 0; index < SettingsCustomColors.Roles.Length; index++)
                AddColorRow(dialog, SettingsCustomColors.Roles[index], RoleLabels[index]);

            inputWarning = CreateMessage(dialog, "Custom theme input warning", 19f);
            contrastWarning = CreateMessage(dialog, "Custom theme contrast warning", 40f);
            WireNavigation();
        }

        private void AddColorRow(RectTransform dialog, string role, string caption)
        {
            var row = CreateRow(dialog, caption, 31f);
            var label = CreateText(row, "Color role", caption, 15, Color.white, TextAnchor.MiddleLeft);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var chip = CreateRect(row, "Swatch");
            chip.gameObject.AddComponent<LayoutElement>().preferredWidth = 26f;
            var swatch = chip.gameObject.AddComponent<Image>();
            swatch.raycastTarget = false;

            var input = CreateRect(row, role);
            input.gameObject.AddComponent<LayoutElement>().preferredWidth = 122f;
            var field = input.gameObject.AddComponent<InputField>();
            field.targetGraphic = input.gameObject.AddComponent<Image>();
            field.textComponent = CreateText(input, "Value", "", 17, Color.white, TextAnchor.MiddleLeft);
            Stretch(field.textComponent.rectTransform, 6f);
            field.lineType = InputField.LineType.SingleLine;
            field.onValueChanged.AddListener(value => ChangeColor(role, value, field.wasCanceled));
            field.onEndEdit.AddListener(_ =>
            {
                field.SetTextWithoutNotify(colors[role]);
                inputWarning.text = "";
            });
            rows.Add((role, label, swatch, field));
        }

        private void ChangeColor(string role, string value, bool wasCanceled)
        {
            if (!IsOpen || wasCanceled)
                return;
            if (!SettingsCustomColors.IsHexColor(value))
            {
                inputWarning.text = "請輸入完整的 #RRGGBB 色值";
                return;
            }
            inputWarning.text = "";
            setColor(role, value);
        }

        private void RefreshContrastWarning(UguiControlPalette palette)
        {
            var belowMinimum = new List<string>(4);
            if (SettingsCustomPalette.Contrast(palette.Heading, palette.PageBackground) < MinimumTextContrast)
                belowMinimum.Add(RoleLabels[2]);
            if (SettingsCustomPalette.Contrast(palette.PanelHeading, palette.PanelBackground) < MinimumTextContrast)
                belowMinimum.Add(RoleLabels[3]);
            if (SettingsCustomPalette.Contrast(palette.ItemText, palette.PanelBackground) < MinimumTextContrast)
                belowMinimum.Add(RoleLabels[4]);
            if (SettingsCustomPalette.Contrast(palette.SecondaryText, palette.PanelBackground) < MinimumTextContrast)
                belowMinimum.Add(RoleLabels[5]);
            contrastWarning.text = belowMinimum.Count == 0 ? "" : "對比偏低（低於 4.5:1）\n" + string.Join("、", belowMinimum);
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
            focusOrder = new Selectable[rows.Count + 2];
            focusOrder[0] = baseButton.ui;
            for (var index = 0; index < rows.Count; index++)
                focusOrder[index + 1] = rows[index].Field;
            focusOrder[focusOrder.Length - 1] = close.ui;
            for (var index = 0; index < focusOrder.Length; index++)
            {
                focusOrder[index].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = focusOrder[(index + focusOrder.Length - 1) % focusOrder.Length],
                    selectOnDown = focusOrder[(index + 1) % focusOrder.Length]
                };
            }
        }

        private void MoveFocus(bool backwards)
        {
            var events = EventSystem.current;
            if (events == null)
                return;
            var selected = events.currentSelectedGameObject;
            var index = Array.FindIndex(focusOrder, control => control.gameObject == selected);
            if (index < 0)
                index = backwards ? 0 : -1;
            focusOrder[(index + (backwards ? -1 : 1) + focusOrder.Length) % focusOrder.Length].Select();
        }

        private static RectTransform CreateRow(RectTransform parent, string name, float height)
        {
            var row = CreateRect(parent, name);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        private static Text CreateMessage(RectTransform parent, string name, float height)
        {
            var text = CreateText(parent, name, "", 14, Color.white, TextAnchor.UpperLeft);
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return text;
        }

        private static ButtonAddon CreateButton(RectTransform parent, string name, string label, float width,
            UnityEngine.Events.UnityAction action)
        {
            var addon = ButtonAddon.Create(parent, label: label, showIcon: false, size: 32f);
            addon.gameObject.name = name;
            addon.labelText.fontSize = 15;
            addon.GetComponent<LayoutElement>().preferredWidth = width;
            addon.ui.onClick.AddListener(action);
            return addon;
        }
    }
}
