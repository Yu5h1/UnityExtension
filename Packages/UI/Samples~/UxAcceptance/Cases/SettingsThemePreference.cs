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

            try
            {
                storage.Value = Encode(mode, custom ?? storedCustom);
                PlayerPrefs.Save();
            }
            catch (PlayerPrefsException)
            {
                Debug.LogWarning("The UX Theme preference could not be saved.");
            }
        }

        internal bool TrySaveImported(SettingsThemeMode mode, SettingsCustomColors custom)
        {
            if (!Enum.IsDefined(typeof(SettingsThemeMode), mode) || (mode == SettingsThemeMode.Custom && custom == null))
                return false;

            string json = Encode(mode, custom);
            bool hadPreviousValue = storage.TryGetValue(out var previous);
            bool assigned = false;
            try
            {
                storage.Value = json;
                assigned = true;
                PlayerPrefs.Save();
                return true;
            }
            catch (PlayerPrefsException)
            {
                if (assigned)
                {
                    try
                    {
                        if (hadPreviousValue)
                            storage.Value = previous;
                        else
                            storage.DeleteKey();
                    }
                    catch (PlayerPrefsException)
                    {
                        Debug.LogWarning("The previous UX Theme preference could not be restored.");
                    }
                }
                return false;
            }
        }

        private bool TryRead(out SettingsThemeMode? mode, out SettingsCustomColors custom)
        {
            if (storage.TryGetValue(out string raw))
                return TryDecode(raw, out mode, out custom);

            mode = null;
            custom = null;
            return true;
        }

        internal static bool TryDecode(string raw, out SettingsThemeMode? mode, out SettingsCustomColors custom)
        {
            mode = null;
            custom = null;

            if (!PreferencesFormat.TryReadJsonObject(raw, out var fields)
                || !fields.TryGetValue("version", out var version) || !(version is double number) || number != Version
                || !fields.TryGetValue("mode", out var modeValue) || !(modeValue is string value))
                return false;

            SettingsThemeMode? decodedMode = value switch
            {
                Light => SettingsThemeMode.Light,
                Dark => SettingsThemeMode.Dark,
                SystemMode => SettingsThemeMode.System,
                CustomMode => SettingsThemeMode.Custom,
                _ => null
            };
            if (decodedMode == null)
                return false;

            if (fields.TryGetValue("custom", out var customValue))
            {
                if (fields.Count != 3 || !(customValue is Dictionary<string, object> colors)
                    || !TryDecodeCustom(colors, out custom))
                    return false;
            }
            else if (fields.Count != 2 || decodedMode == SettingsThemeMode.Custom)
                return false;

            mode = decodedMode;
            return true;
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

        internal static string Encode(SettingsThemeMode mode, SettingsCustomColors custom)
        {
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
            var json = new StringBuilder("{\"version\":1,\"mode\":\"");
            json.Append(storedMode).Append('"');
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
