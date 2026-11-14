using System;
using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>
    /// Applies the shared VisualFoundation palette roles to opt-in uGUI controls.
    /// </summary>
    public sealed class UguiControlPalette
    {
        private const string VisualFoundationResource = "Yu5h1Lib.UI/VisualFoundation";
        private const float FadeDuration = 0.1f;
        private readonly string defaultScheme;
        private Theme sourceTheme;
        private PaletteColors colors;

        /// <summary>The built-in light palette. Read its colors on the Unity main thread.</summary>
        public static UguiControlPalette Light { get; } = new UguiControlPalette("Light");
        /// <summary>The built-in dark palette. Read its colors on the Unity main thread.</summary>
        public static UguiControlPalette Dark { get; } = new UguiControlPalette("Dark");

        /// <summary>The imported or explicitly supplied Theme used by this palette.</summary>
        public Theme SourceTheme => sourceTheme ??= LoadDefaultTheme(defaultScheme);

        private PaletteColors Colors => colors ??= new PaletteColors(SourceTheme);

        public Color PageBackground => Colors.PageBackground;
        public Color PanelBackground => Colors.PanelBackground;
        public Color Heading => Colors.Heading;
        public Color PanelHeading => Colors.PanelHeading;
        public Color ItemText => Colors.ItemText;
        public Color SecondaryText => Colors.SecondaryText;
        public Color ControlBorder => Colors.ControlBorder;
        public Color Hover => Colors.Hover;
        public Color Accent => Colors.Accent;
        public Color OnAccent => Colors.OnAccent;
        public Color Focus => Colors.Focus;
        public Color Disabled => Colors.Disabled;
        public Color Overlay => Colors.Overlay;

        /// <summary>Reads the VisualFoundation roles from a Theme and its schema for use by uGUI controls.</summary>
        public static UguiControlPalette FromTheme(Theme theme)
        {
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            return new UguiControlPalette(theme);
        }

        private UguiControlPalette(string defaultScheme) => this.defaultScheme = defaultScheme;

        private UguiControlPalette(Theme theme)
        {
            sourceTheme = theme;
            colors = new PaletteColors(theme);
        }

        private static Theme LoadDefaultTheme(string scheme)
        {
            var themes = Resources.LoadAll<Theme>(VisualFoundationResource);
            var theme = Array.Find(themes, candidate => candidate.name == scheme);
            if (theme == null)
                throw new InvalidOperationException(
                    $"The imported VisualFoundation Theme '{scheme}' is missing from Resources/{VisualFoundationResource}.");

            return theme;
        }

        private sealed class PaletteColors
        {
            public readonly Color PageBackground;
            public readonly Color PanelBackground;
            public readonly Color Heading;
            public readonly Color PanelHeading;
            public readonly Color ItemText;
            public readonly Color SecondaryText;
            public readonly Color ControlBorder;
            public readonly Color Hover;
            public readonly Color Accent;
            public readonly Color OnAccent;
            public readonly Color Focus;
            public readonly Color Disabled;
            public readonly Color Overlay;

            internal PaletteColors(Theme theme)
            {
                PageBackground = ReadColor(theme, "pageBackground");
                PanelBackground = ReadColor(theme, "panelBackground");
                Heading = ReadColor(theme, "heading");
                PanelHeading = ReadColor(theme, "panelHeading");
                ItemText = ReadColor(theme, "itemText");
                SecondaryText = ReadColor(theme, "secondaryText");
                ControlBorder = ReadColor(theme, "controlBorder");
                Hover = ReadColor(theme, "hover");
                Accent = ReadColor(theme, "accent");
                OnAccent = ReadColor(theme, "onAccent");
                Focus = ReadColor(theme, "focus");
                Disabled = ReadColor(theme, "disabled");
                Overlay = ReadColor(theme, "overlay");
            }

            private static Color ReadColor(Theme theme, string role)
            {
                if (theme.TryGet<Color>(role, out var color))
                    return color;

                throw new ArgumentException($"Theme '{theme.name}' is missing VisualFoundation color role '{role}'.", nameof(theme));
            }
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
