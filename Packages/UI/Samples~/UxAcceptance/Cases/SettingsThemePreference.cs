using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Stores the settings case's theme as a separate UX Theme v1 preference.</summary>
    internal sealed class SettingsThemePreference
    {
        private const double Version = 1;
        private const string Light = "light";
        private const string Dark = "dark";
        private const string SystemMode = "system";
        private const string CustomMode = "custom";

        private static readonly string[] CustomColors =
        {
            "pageBackground", "panelBackground", "heading", "panelHeading",
            "itemText", "secondaryText", "border", "accent"
        };
        private static readonly Regex HexColor = new Regex(@"\A#[0-9a-fA-F]{6}\z");

        private readonly PlayerPrefValue<string> storage;

        public SettingsThemePreference(string settingsKey)
        {
            storage = new PlayerPrefValue<string> { key = settingsKey + ".Theme" };
        }

        public UguiControlPalette Load(UguiControlPalette fallback)
        {
            if (!TryRead(out string mode, out var custom))
            {
                Debug.LogWarning("Unsupported or corrupt UX Theme preference. The stored data was preserved.");
                return fallback;
            }

            string scheme = mode == CustomMode ? (string)custom["base"] : mode;
            return scheme switch
            {
                Light => UguiControlPalette.Light,
                Dark => UguiControlPalette.Dark,
                _ => fallback
            };
        }

        public void Save(UguiControlPalette palette)
        {
            if (!TryRead(out _, out var custom))
                return;

            string mode = ReferenceEquals(palette, UguiControlPalette.Light) ? Light : Dark;
            try
            {
                storage.Value = Encode(mode, custom);
                PlayerPrefs.Save();
            }
            catch (PlayerPrefsException)
            {
                Debug.LogWarning("The UX Theme preference could not be saved.");
            }
        }

        private bool TryRead(out string mode, out Dictionary<string, object> custom)
        {
            mode = null;
            custom = null;
            if (!storage.TryGetValue(out string raw))
                return true;

            if (!PreferencesFormat.TryReadJsonObject(raw, out var fields)
                || !fields.TryGetValue("version", out var version) || !(version is double number) || number != Version
                || !fields.TryGetValue("mode", out var modeValue) || !(modeValue is string value))
                return false;

            if (value != Light && value != Dark && value != SystemMode && value != CustomMode)
                return false;

            mode = value;
            if (!fields.TryGetValue("custom", out var customValue))
                return fields.Count == 2 && mode != CustomMode;

            custom = customValue as Dictionary<string, object>;
            return fields.Count == 3 && custom != null && IsValidCustom(custom);
        }

        private static bool IsValidCustom(Dictionary<string, object> custom)
        {
            if (custom.Count != CustomColors.Length + 1
                || !custom.TryGetValue("base", out var baseValue) || !(baseValue is string scheme)
                || (scheme != Light && scheme != Dark))
                return false;

            foreach (string key in CustomColors)
            {
                if (!custom.TryGetValue(key, out var value) || !(value is string color) || !HexColor.IsMatch(color))
                    return false;
            }

            return true;
        }

        private static string Encode(string mode, Dictionary<string, object> custom)
        {
            var json = new StringBuilder("{\"version\":1,\"mode\":\"");
            json.Append(mode).Append('"');
            if (custom != null)
            {
                json.Append(",\"custom\":{\"base\":\"").Append(custom["base"]).Append('"');
                foreach (string key in CustomColors)
                    json.Append(",\"").Append(key).Append("\":\"").Append(((string)custom[key]).ToLowerInvariant()).Append('"');
                json.Append('}');
            }

            return json.Append('}').ToString();
        }
    }
}
