// Holds the eight accepted color values for the settings case's UX Theme v1 custom mode.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Stores a complete custom palette with a light or dark base.</summary>
    internal sealed class SettingsCustomColors
    {
        internal static readonly string[] Roles =
        {
            "pageBackground", "panelBackground", "heading", "panelHeading",
            "itemText", "secondaryText", "border", "accent"
        };

        private static readonly Regex HexColor = new Regex(@"\A#[0-9a-fA-F]{6}\z");
        private readonly Dictionary<string, string> colors = new Dictionary<string, string>(Roles.Length);

        internal SettingsCustomColors(bool isDark, Func<string, string> getColor)
        {
            IsDark = isDark;
            foreach (string role in Roles)
                colors.Add(role, NormalizeHexColor(getColor(role)));
        }

        internal bool IsDark { get; set; }

        internal string this[string role]
        {
            get => colors[role];
            set
            {
                if (!colors.ContainsKey(role))
                    throw new ArgumentException("Unknown UX Theme color role.", nameof(role));

                colors[role] = NormalizeHexColor(value);
            }
        }

        internal static bool IsHexColor(string value) => value != null && HexColor.IsMatch(value);

        private static string NormalizeHexColor(string value)
        {
            if (!IsHexColor(value))
                throw new ArgumentException("A UX Theme color must be in #RRGGBB format.", nameof(value));

            return value.ToLowerInvariant();
        }
    }
}
