using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>
    /// Applies the shared VisualFoundation palette roles to opt-in uGUI controls.
    /// </summary>
    public sealed class UguiControlPalette
    {
        private const float FadeDuration = 0.1f;

        // VisualFoundation visual-design.json 1.3.0, SHA-256 ffcfb60703205089157835e6c15348798c43eb4b59b2df4a488b20a4b7e73250.
        public static UguiControlPalette Light { get; } = new UguiControlPalette(
            page: new Color32(0xf5, 0xf3, 0xec, 255),
            panel: new Color32(0xff, 0xfd, 0xf8, 255),
            itemText: new Color32(0x27, 0x34, 0x2e, 255),
            secondaryText: new Color32(0x4f, 0x5e, 0x57, 255),
            controlBorder: new Color32(0x80, 0x89, 0x81, 255),
            hover: new Color32(0xef, 0xee, 0xe8, 255),
            accent: new Color32(0x1f, 0x68, 0x4f, 255),
            onAccent: Color.white);

        public static UguiControlPalette Dark { get; } = new UguiControlPalette(
            page: new Color32(0x17, 0x1a, 0x21, 255),
            panel: new Color32(0x20, 0x25, 0x30, 255),
            itemText: new Color32(0xcb, 0xd4, 0xdf, 255),
            secondaryText: new Color32(0x9d, 0xa9, 0xb8, 255),
            controlBorder: new Color32(0x73, 0x7d, 0x8c, 255),
            hover: new Color32(0x2a, 0x31, 0x3c, 255),
            accent: new Color32(0x7f, 0x9f, 0xd1, 255),
            onAccent: Color.black);

        public Color PageBackground { get; }
        public Color PanelBackground { get; }
        public Color ItemText { get; }
        public Color SecondaryText { get; }
        public Color ControlBorder { get; }
        public Color Hover { get; }
        public Color Accent { get; }
        public Color OnAccent { get; }
        public Color Focus => Accent;
        public Color Disabled => SecondaryText;

        private UguiControlPalette(Color page, Color panel, Color itemText, Color secondaryText,
            Color controlBorder, Color hover, Color accent, Color onAccent)
        {
            PageBackground = page;
            PanelBackground = panel;
            ItemText = itemText;
            SecondaryText = secondaryText;
            ControlBorder = controlBorder;
            Hover = hover;
            Accent = accent;
            OnAccent = onAccent;
        }

        /// <summary>
        /// Applies the palette to a native button and its label, icon or subtitle.
        /// Apply after configuring any ButtonAddon appearance properties.
        /// </summary>
        public void ApplyButton(Button button, Graphic foreground = null, bool accent = false, bool border = true,
            Graphic secondary = null)
        {
            Color normal = accent ? Accent : PanelBackground;
            Color highlighted = accent ? Color.Lerp(Accent, PanelBackground, 0.08f) : Hover;
            Color pressed = accent ? Color.Lerp(Accent, PanelBackground, 0.15f) : Color.Lerp(Hover, ControlBorder, 0.16f);
            ApplyTint(button, normal, highlighted, pressed, PanelBackground);

            Color primaryText = accent ? OnAccent : ItemText;
            Color secondaryText = accent ? OnAccent : SecondaryText;
            var decoration = UguiControlDecoration.GetOrCreate(button);
            decoration.Configure(button, this, foreground, primaryText, secondary, secondaryText, border, true, accent);
        }

        /// <summary>
        /// Styles a native text input and its caret. A borderless field keeps the container surface visible
        /// and relies on the native caret while focused.
        /// </summary>
        public void ApplyField(InputField field, bool border = true)
        {
            Color normal = border ? PanelBackground : Color.clear;
            Color highlighted = border ? Hover : Color.clear;
            ApplyTint(field, normal, highlighted, highlighted, normal);
            field.customCaretColor = true;
            field.caretColor = ItemText;

            Color selection = Focus;
            selection.a = 0.35f;
            field.selectionColor = selection;

            var decoration = UguiControlDecoration.GetOrCreate(field);
            decoration.Configure(field, this, field.textComponent, ItemText, field.placeholder, SecondaryText, border, border, false);
        }

        private static void ApplyTint(Selectable control, Color normal, Color highlighted, Color pressed, Color disabled)
        {
            if (control.targetGraphic != null)
                control.targetGraphic.color = Color.white;

            control.transition = Selectable.Transition.ColorTint;
            var tints = new ColorBlock
            {
                normalColor = normal,
                highlightedColor = highlighted,
                pressedColor = pressed,
                selectedColor = normal,
                disabledColor = disabled,
                colorMultiplier = 1f
            };
            control.colors = tints;
            tints.fadeDuration = FadeDuration;
            control.colors = tints;
        }
    }
}
