// Projects the settings case's custom colors through the existing Theme and ColorObjects.
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Owns the transient Theme used to apply a custom VisualFoundation palette.</summary>
    internal sealed class SettingsCustomPalette : IDisposable
    {
        private const string Focus = "focus";
        private const string Disabled = "disabled";
        private const string OnAccent = "onAccent";

        private readonly Dictionary<string, ColorObject> overrides = new();
        private Theme baseTheme;
        private Theme customTheme;

        /// <summary>Reads the eight editable color defaults from the imported scheme.</summary>
        internal static SettingsCustomColors FromBase(bool isDark)
        {
            var theme = GetBaseTheme(isDark);
            return new SettingsCustomColors(isDark, role =>
            {
                if (!theme.TryGet<Color>(role, out var color))
                    throw new InvalidOperationException($"VisualFoundation theme '{theme.name}' is missing '{role}'.");

                return "#" + ColorUtility.ToHtmlStringRGB(color).ToLowerInvariant();
            });
        }

        /// <summary>Updates the transient Theme and returns a fresh palette for its current values.</summary>
        internal UguiControlPalette Build(SettingsCustomColors colors)
        {
            var theme = GetBaseTheme(colors.IsDark);
            if (customTheme == null)
                CreateTheme(theme);
            if (baseTheme != theme)
                UseBaseTheme(theme);

            foreach (string role in SettingsCustomColors.Roles)
                overrides[role].value = ReadHex(colors[role]);

            var accent = overrides["accent"].value;
            overrides[Focus].value = accent;
            overrides[Disabled].value = overrides["secondaryText"].value;
            overrides[OnAccent].value = Contrast(Color.white, accent) >= Contrast(Color.black, accent)
                ? Color.white
                : Color.black;

            return UguiControlPalette.FromTheme(customTheme);
        }

        /// <summary>Parses a color already validated by the preference model.</summary>
        internal static Color ReadHex(string color)
        {
            if (!ColorUtility.TryParseHtmlString(color, out var value))
                throw new ArgumentException("A valid RGB hex color is required.", nameof(color));

            return value;
        }

        /// <summary>Calculates the WCAG 2 contrast ratio of two opaque sRGB colors.</summary>
        internal static float Contrast(Color foreground, Color background)
        {
            double first = Luminance(foreground);
            double second = Luminance(background);
            return (float)((Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05));
        }

        private static double Luminance(Color color) =>
            0.2126 * LinearChannel(color.r) + 0.7152 * LinearChannel(color.g) + 0.0722 * LinearChannel(color.b);

        private static double LinearChannel(double channel) =>
            channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

        private static Theme GetBaseTheme(bool isDark) =>
            (isDark ? UguiControlPalette.Dark : UguiControlPalette.Light).SourceTheme;

        private void CreateTheme(Theme theme)
        {
            customTheme = Object.Instantiate(theme);
            customTheme.name = "Custom Settings Theme";
            customTheme.hideFlags = HideFlags.DontSave;

            foreach (string role in SettingsCustomColors.Roles)
                AddOverride(role);
            AddOverride(Focus);
            AddOverride(Disabled);
            AddOverride(OnAccent);
        }

        private void AddOverride(string role)
        {
            var color = ScriptableObject.CreateInstance<ColorObject>();
            color.name = role;
            color.hideFlags = HideFlags.DontSave;
            overrides.Add(role, color);
        }

        /// <summary>Retains the selected scheme's fixed roles in the clone's serialized parameter list.</summary>
        private void UseBaseTheme(Theme theme)
        {
            customTheme.items.Clear();
            foreach (var parameter in theme.items)
                customTheme.items.Add(overrides.TryGetValue(parameter.name, out var color) ? color : parameter);
            baseTheme = theme;
        }

        public void Dispose()
        {
            Destroy(customTheme);
            foreach (var color in overrides.Values)
                Destroy(color);
            overrides.Clear();
            customTheme = null;
            baseTheme = null;
        }

        private static void Destroy(Object ownedObject)
        {
            if (ownedObject == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(ownedObject);
            else
                Object.DestroyImmediate(ownedObject);
        }
    }
}
