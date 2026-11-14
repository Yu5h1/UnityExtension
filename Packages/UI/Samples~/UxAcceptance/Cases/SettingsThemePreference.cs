using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Yu5h1Lib.UI.Acceptance
{
    internal enum SettingsThemeMode
    {
        Light,
        Dark,
        System,
        Custom
    }

    /// <summary>Stores the settings case's theme as a separate UX Theme v1 preference.</summary>
    internal sealed class SettingsThemePreference
    {
        private const double Version = 1;
        private const string Light = "light";
        private const string Dark = "dark";
        private const string SystemMode = "system";
        private const string CustomMode = "custom";

        private readonly PlayerPrefValue<string> storage;

        public SettingsThemePreference(string settingsKey)
        {
            storage = new PlayerPrefValue<string> { key = settingsKey + ".Theme" };
        }

        public UguiControlPalette Load(UguiControlPalette fallback, out SettingsThemeMode? mode, out SettingsCustomColors custom)
        {
            if (!TryRead(out mode, out custom))
            {
                mode = null;
                custom = null;
                Debug.LogWarning("Unsupported or corrupt UX Theme preference. The stored data was preserved.");
                return fallback;
            }

            return mode switch
            {
                SettingsThemeMode.Light => UguiControlPalette.Light,
                SettingsThemeMode.Dark => UguiControlPalette.Dark,
                SettingsThemeMode.Custom => custom.IsDark ? UguiControlPalette.Dark : UguiControlPalette.Light,
                _ => fallback
            };
        }

        public void Save(SettingsThemeMode mode, SettingsCustomColors custom = null)
        {
            if (!TryRead(out _, out var storedCustom))
                return;

            custom ??= storedCustom;
            if (mode == SettingsThemeMode.Custom && custom == null)
                throw new ArgumentNullException(nameof(custom));

            string storedMode = mode switch
            {
                SettingsThemeMode.Light => Light,
                SettingsThemeMode.Dark => Dark,
                SettingsThemeMode.System => SystemMode,
                SettingsThemeMode.Custom => CustomMode,
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            };
            try
            {
                storage.Value = Encode(storedMode, custom);
                PlayerPrefs.Save();
            }
            catch (PlayerPrefsException)
            {
                Debug.LogWarning("The UX Theme preference could not be saved.");
            }
        }

        private bool TryRead(out SettingsThemeMode? mode, out SettingsCustomColors custom)
        {
            mode = null;
            custom = null;
            if (!storage.TryGetValue(out string raw))
                return true;

            if (!PreferencesFormat.TryReadJsonObject(raw, out var fields)
                || !fields.TryGetValue("version", out var version) || !(version is double number) || number != Version
                || !fields.TryGetValue("mode", out var modeValue) || !(modeValue is string value))
                return false;

            mode = value switch
            {
                Light => SettingsThemeMode.Light,
                Dark => SettingsThemeMode.Dark,
                SystemMode => SettingsThemeMode.System,
                CustomMode => SettingsThemeMode.Custom,
                _ => null
            };
            if (mode == null)
                return false;

            if (!fields.TryGetValue("custom", out var customValue))
                return fields.Count == 2 && mode != SettingsThemeMode.Custom;

            return fields.Count == 3 && customValue is Dictionary<string, object> colors
                && TryDecodeCustom(colors, out custom);
        }

        private static bool TryDecodeCustom(Dictionary<string, object> colors, out SettingsCustomColors custom)
        {
            custom = null;
            if (colors.Count != SettingsCustomColors.Roles.Length + 1
                || !colors.TryGetValue("base", out var baseValue) || !(baseValue is string scheme)
                || (scheme != Light && scheme != Dark))
                return false;

            foreach (string role in SettingsCustomColors.Roles)
            {
                if (!colors.TryGetValue(role, out var value) || !(value is string color) || !SettingsCustomColors.IsHexColor(color))
                    return false;
            }

            custom = new SettingsCustomColors(scheme == Dark, role => (string)colors[role]);
            return true;
        }

        private static string Encode(string mode, SettingsCustomColors custom)
        {
            var json = new StringBuilder("{\"version\":1,\"mode\":\"");
            json.Append(mode).Append('"');
            if (custom != null)
            {
                json.Append(",\"custom\":{\"base\":\"").Append(custom.IsDark ? Dark : Light).Append('"');
                foreach (string role in SettingsCustomColors.Roles)
                    json.Append(",\"").Append(role).Append("\":\"").Append(custom[role]).Append('"');
                json.Append('}');
            }

            return json.Append('}').ToString();
        }
    }
}
