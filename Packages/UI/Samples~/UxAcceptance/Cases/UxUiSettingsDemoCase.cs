using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib.Serialization;
using static Yu5h1Lib.UI.Acceptance.UxUiAcceptanceHost;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Runs the shared settings panel against a dedicated PlayerPrefs scope.</summary>
    public sealed class UxUiSettingsDemoCase : IUxUiModuleDemoCase
    {
        public string Id => "settingspanel";
        public string DisplayName => "Config 與 Setting";
        public string ModuleSpec => "Yu5h1Lib.UX/SettingsPanel";

        private readonly string key;
        private readonly UguiControlPalette initialPalette;
        private readonly SettingsThemePreference themePreference;
        private readonly Func<bool?> readSystemTheme;
        private SettingsThemeMode? themeMode;
        private SettingsCustomColors customColors;
        private SettingsCustomPalette customPalette;
        private SettingsCustomThemeEditor customEditor;
        private UguiControlPalette palette;
        private Image background;
        private Coroutine systemThemeWatch;
        private readonly Func<string, DataView, CancellationToken, Task> write;
        private readonly Func<bool, bool, CancellationToken, Task> apply;
        private RectTransform root;
        private SettingsPanel config;
        private SettingsPanel setting;
        private Text preview;
        private ButtonAddon directionAddon;
        private readonly List<(Button Button, Text Label, SettingsThemeMode? Mode)> themeButtons = new();
        private readonly List<GraphicColorBinding> colorBindings = new();
        private ConfigStore<Values> store;
        private Values draft;
        private readonly List<(Toggle Toggle, bool First)> fields = new();
        private bool autoSave = true;
        private enum Choice { Hints, Sound }
        private readonly struct Values
        {
            public readonly bool Hints, Sound;
            public Values(bool hints, bool sound) { Hints = hints; Sound = sound; }
        }

        public UxUiSettingsDemoCase(string key = "Yu5h1Lib.UI.Acceptance.Settings")
            : this(key, WritePlayerPrefs)
        {
        }

        /// <summary>Uses the supplied palette for this settings case.</summary>
        public UxUiSettingsDemoCase(UguiControlPalette palette, string key)
            : this(key, WritePlayerPrefs, null, palette, SettingsSystemTheme.Read)
        {
        }

        /// <summary>Uses an alternate writer for the demo scope and an optional application target for the two settings.</summary>
        public UxUiSettingsDemoCase(string key, Func<string, DataView, CancellationToken, Task> write,
            Func<bool, bool, CancellationToken, Task> apply = null)
            : this(key, write, apply, null, SettingsSystemTheme.Read)
        {
        }

        /// <summary>Uses an alternate system theme source for the demo scope. True means dark, false means light.</summary>
        public UxUiSettingsDemoCase(string key, Func<string, DataView, CancellationToken, Task> write,
            Func<bool, bool, CancellationToken, Task> apply, Func<bool?> readSystemTheme)
            : this(key, write, apply, null, readSystemTheme)
        {
        }

        private UxUiSettingsDemoCase(string key, Func<string, DataView, CancellationToken, Task> write,
            Func<bool, bool, CancellationToken, Task> apply, UguiControlPalette palette, Func<bool?> readSystemTheme)
        {
            this.key = key;
            initialPalette = palette ?? Palette;
            themePreference = new SettingsThemePreference(key);
            this.readSystemTheme = readSystemTheme ?? throw new ArgumentNullException(nameof(readSystemTheme));
            this.write = write ?? throw new ArgumentNullException(nameof(write));
            this.apply = apply;
        }

        public void Mount(RectTransform container)
        {
            Unmount();
            autoSave = true;
            palette = themePreference.Load(initialPalette, out themeMode, out customColors);
            bool followsSystem = themeMode == SettingsThemeMode.System;
            bool? systemIsDark = followsSystem ? readSystemTheme() : null;
            if (followsSystem)
                palette = ResolveSystemPalette(systemIsDark);
            else if (themeMode == SettingsThemeMode.Custom)
                BuildCustomPalette();
            root = CreateRect(container, "SettingsDemo");
            Stretch(root);
            background = root.gameObject.AddComponent<Image>();
            BindColor("pageBackground", background);
            background.raycastTarget = false;
            var page = CreateRect(root, "Settings demo content");
            Stretch(page, 12);
            var actions = CreateRect(page, "Settings entries");
            Stretch(actions);
            preview = CreateText(actions, "Applied", "", 18, palette.Heading, TextAnchor.MiddleCenter);
            BindColor("heading", preview);
            Stretch(preview.rectTransform);
            directionAddon = ButtonAddon.Create(actions, label: "橫列／直欄", showIcon: false);
            var direction = directionAddon.ui;
            direction.name = "Tab direction";
            var directionRect = (RectTransform)direction.transform;
            directionRect.anchorMin = directionRect.anchorMax = new Vector2(0, 1);
            directionRect.pivot = new Vector2(0, 1);
            directionRect.sizeDelta = new Vector2(138, 36);
            direction.onClick.AddListener(() =>
            {
                var next = config.Tabs.Orientation == Tabs.Direction.Row ? Tabs.Direction.Column : Tabs.Direction.Row;
                config.Tabs.Orientation = setting.Tabs.Orientation = next;
            });

            config = SettingsPanel.Create(page, SettingsPanel.Presentation.Config, palette);
            setting = SettingsPanel.Create(page, SettingsPanel.Presentation.Setting, palette);
            ((RectTransform)setting.Opener.transform).anchoredPosition = new Vector2(-48, 0);
            ((RectTransform)setting.Opener.transform).sizeDelta = new Vector2(46, 36);
            setting.Opener.name = "提示設定";
            config.SetGroups(new[] { CreateGroup(config, Choice.Hints), CreateGroup(config, Choice.Sound) });
            setting.SetGroups(new[] { CreateGroup(setting, Choice.Hints) });
            AddThemeSwitch(config);
            AddThemeSwitch(setting);
            ApplyTheme();
            if (followsSystem)
                StartSystemThemeWatch(systemIsDark);
            store = new ConfigStore<Values>(new Values(true, true), value => value, _ => { },
                _ =>
                {
                    var read = Preferences.DefaultStorage.Load(key);
                    if (read.Status == PreferencesLoadStatus.Error)
                        throw new InvalidOperationException(read.Error);
                    return Task.FromResult(read.Status == PreferencesLoadStatus.Missing
                        ? (false, default(Values)) : (true, new Values(bool.Parse(read.Data["hints"]), bool.Parse(read.Data["sound"]))));
                },
                (value, token) =>
                {
                    var data = new DataView { ["hints"] = value.Hints.ToString(), ["sound"] = value.Sound.ToString() };
                    return write(key, data, token);
                },
                async (value, token) =>
                {
                    if (apply != null)
                        await apply(value.Hints, value.Sound, token);
                    token.ThrowIfCancellationRequested();
                    preview.text = $"目前套用　提示 {(value.Hints ? "開" : "關")}　音效 {(value.Sound ? "開" : "關")}";
                });
            draft = store.Defaults;
            Bind(config);
            Bind(setting);
            store.Changed += Render;
            _ = store.LoadAsync();
        }

        private void BindColor(string role, Graphic graphic) =>
            colorBindings.Add(GraphicColorBinding.Create(role, graphic));

        private void AddThemeSwitch(SettingsPanel panel)
        {
            AddThemeButton(panel, "Theme switch", "", 62, SwitchTheme, null);
            AddThemeButton(panel, "Follow system theme", "跟隨系統", 82, FollowSystemTheme, SettingsThemeMode.System);
            AddThemeButton(panel, "Custom theme", "自訂", 56, OpenCustomTheme, SettingsThemeMode.Custom);
            var divider = CreateRect(panel.HeaderActions, "Theme header divider");
            divider.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            divider.anchorMax = Vector2.right;
            divider.anchoredPosition = new Vector2(0, -3);
            divider.sizeDelta = new Vector2(0, 1);
            var line = divider.gameObject.AddComponent<Image>();
            line.raycastTarget = false;
            BindColor("border", line);
        }

        private void AddThemeButton(SettingsPanel panel, string name, string caption, float width, Action action,
            SettingsThemeMode? mode)
        {
            var header = panel.HeaderActions;
            var rect = CreateRect(header, name);
            rect.SetSiblingIndex(header.childCount - 2);
            var size = rect.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = width;
            size.preferredHeight = 36;

            var background = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<SettingsThemeButton>();
            button.Initialize(panel);
            button.targetGraphic = background;
            button.onClick.AddListener(() => action());

            var label = CreateText(rect, "Label", caption, 15, palette.ItemText, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            themeButtons.Add((button, label, mode));
        }

        private void SwitchTheme()
        {
            customEditor?.Hide();
            StopSystemThemeWatch();
            bool useDark = UsesLightTheme;
            var mode = useDark ? SettingsThemeMode.Dark : SettingsThemeMode.Light;
            themeMode = mode;
            palette = useDark ? UguiControlPalette.Dark : UguiControlPalette.Light;
            ApplyTheme();
            themePreference.Save(mode, customColors);
        }

        private void FollowSystemTheme()
        {
            if (themeMode == SettingsThemeMode.System)
                return;

            customEditor?.Hide();
            bool? systemIsDark = readSystemTheme();
            themeMode = SettingsThemeMode.System;
            palette = ResolveSystemPalette(systemIsDark);
            ApplyTheme();
            StartSystemThemeWatch(systemIsDark);
            themePreference.Save(SettingsThemeMode.System, customColors);
        }

        private void OpenCustomTheme()
        {
            if (themeMode != SettingsThemeMode.Custom)
            {
                StopSystemThemeWatch();
                customColors ??= SettingsCustomPalette.FromBase(!UsesLightTheme);
                themeMode = SettingsThemeMode.Custom;
                ApplyCustomTheme();
            }

            customEditor ??= SettingsCustomThemeEditor.Create(root, SetCustomColor, SetCustomBase,
                () => SetPanelKeyboardEnabled(true));
            SetPanelKeyboardEnabled(false);
            customEditor.Show(customColors, palette);
        }

        private void SetPanelKeyboardEnabled(bool enabled)
        {
            config.enabled = enabled;
            setting.enabled = enabled;
        }

        private void SetCustomColor(string role, string color)
        {
            if (themeMode != SettingsThemeMode.Custom ||
                string.Equals(customColors[role], color, StringComparison.OrdinalIgnoreCase))
                return;

            customColors[role] = color;
            ApplyCustomTheme();
        }

        private void SetCustomBase(bool isDark)
        {
            if (themeMode != SettingsThemeMode.Custom || customColors.IsDark == isDark)
                return;

            customColors.IsDark = isDark;
            ApplyCustomTheme();
        }

        private void BuildCustomPalette()
        {
            customPalette ??= new SettingsCustomPalette();
            palette = customPalette.Build(customColors);
        }

        private void ApplyCustomTheme()
        {
            BuildCustomPalette();
            ApplyTheme();
            themePreference.Save(SettingsThemeMode.Custom, customColors);
        }

        private UguiControlPalette ResolveSystemPalette(bool? isDark) => isDark switch
        {
            true => UguiControlPalette.Dark,
            false => UguiControlPalette.Light,
            null => initialPalette
        };

        private void StartSystemThemeWatch(bool? initial)
        {
            if (Application.isPlaying && background.gameObject.activeInHierarchy)
                background.StartCoroutine(ref systemThemeWatch,
                    SettingsSystemTheme.Watch(readSystemTheme, ApplySystemTheme, initial));
        }

        private void StopSystemThemeWatch()
        {
            if (systemThemeWatch == null)
                return;

            if (background != null)
                background.StopCoroutine(systemThemeWatch);
            systemThemeWatch = null;
        }

        private void ApplySystemTheme(bool? isDark)
        {
            var nextPalette = ResolveSystemPalette(isDark);
            if (themeMode != SettingsThemeMode.System || ReferenceEquals(palette, nextPalette))
                return;

            palette = nextPalette;
            ApplyTheme();
        }

        private bool UsesLightTheme => themeMode == SettingsThemeMode.Custom
            ? !customColors.IsDark
            : ReferenceEquals(palette.SourceTheme, UguiControlPalette.Light.SourceTheme);

        private void ApplyTheme()
        {
            var theme = palette.SourceTheme;
            foreach (var binding in colorBindings)
                binding.Apply(theme);

            config.ApplyPalette(palette);
            setting.ApplyPalette(palette);
            palette.ApplyButton(directionAddon.ui, directionAddon.labelText);
            bool light = UsesLightTheme;
            foreach (var (button, label, mode) in themeButtons)
            {
                if (!mode.HasValue)
                    label.text = light ? "暗色" : "亮色";
                palette.ApplyButton(button, label, accent: mode.HasValue && mode == themeMode);
            }
            customEditor?.ApplyPalette(palette);
        }

        private static Task WritePlayerPrefs(string key, DataView data, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Preferences.DefaultStorage.Save(key, data);
            return Task.CompletedTask;
        }

        private SettingsPanel.Group CreateGroup(SettingsPanel panel, Choice choice)
        {
            var hints = choice == Choice.Hints;
            var page = CreateRect(panel.transform, "Field");
            var field = CreateToggle(page, hints ? "顯示提示" : "播放音效");
            fields.Add((field, hints));
            field.onValueChanged.AddListener(value =>
            {
                draft = hints ? new Values(value, draft.Sound) : new Values(draft.Hints, value);
                Edit();
            });
            var details = CreateRect(panel.transform, "Details");
            var text = CreateText(details, "Details", hints ? "提示會顯示在操作區。" : "音效會在操作時播放。",
                16, palette.ItemText, TextAnchor.UpperLeft);
            BindColor("itemText", text);
            Stretch(text.rectTransform, 6);
            return new SettingsPanel.Group(hints ? "hints" : "sound", hints ? "提示" : "音效",
                new[] { new Tabs.Item("general", "一般", page), new Tabs.Item("details", "說明", details) });
        }

        private Toggle CreateToggle(RectTransform parent, string label)
        {
            var row = CreateRect(parent, label);
            row.anchorMin = new Vector2(0, 1);
            row.anchorMax = Vector2.one;
            row.pivot = new Vector2(.5f, 1);
            row.sizeDelta = new Vector2(0, 36);
            var toggle = row.gameObject.AddComponent<Toggle>();
            var box = CreateRect(row, "Box");
            box.anchorMin = box.anchorMax = new Vector2(0, .5f);
            box.anchoredPosition = new Vector2(12, 0);
            box.sizeDelta = new Vector2(18, 18);
            var background = box.gameObject.AddComponent<Image>();
            BindColor("hover", background);
            var check = CreateRect(box, "Check");
            Stretch(check, 3);
            var mark = check.gameObject.AddComponent<Image>();
            BindColor("accent", mark);
            mark.raycastTarget = false;
            var caption = CreateText(row, "Label", label, 16, palette.ItemText, TextAnchor.MiddleLeft);
            BindColor("itemText", caption);
            Stretch(caption.rectTransform);
            caption.rectTransform.offsetMin = new Vector2(28, 0);
            caption.raycastTarget = true;
            toggle.targetGraphic = background;
            toggle.graphic = mark;
            return toggle;
        }

        private void Bind(SettingsPanel panel)
        {
            panel.Closed += () =>
            {
                if (store.CurrentStatus != ConfigStore<Values>.Status.SaveError)
                    draft = store.Accepted;
                Render();
            };
            panel.ResetRequested += () =>
            {
                var defaults = store.Defaults;
                draft = panel.ActiveGroup == "hints"
                    ? new Values(defaults.Hints, draft.Sound) : new Values(draft.Hints, defaults.Sound);
                Edit();
            };
            panel.SaveBar.AutoSaveRequested += enabled =>
            {
                autoSave = enabled;
                Edit();
            };
            panel.SaveBar.SaveRequested += () => _ = store.SaveAsync(draft);
            panel.SaveBar.RetryApplyRequested += () => _ = store.RetryApplyAsync();
        }

        private void Edit()
        {
            Render();
            if (autoSave)
                _ = store.SaveAsync(draft);
        }

        private void Render()
        {
            var status = store.CurrentStatus;
            var enabled = status != ConfigStore<Values>.Status.Idle && status != ConfigStore<Values>.Status.LoadError;
            var retry = status == ConfigStore<Values>.Status.ApplyError;
            var saveError = status == ConfigStore<Values>.Status.SaveError;
            var message = retry ? "已保存，尚未套用。" : saveError ? "保存失敗。"
                : status == ConfigStore<Values>.Status.LoadError ? "讀取失敗，原設定已保留。" : null;
            if (status == ConfigStore<Values>.Status.Applying || retry)
                draft = store.Accepted;
            var state = new SaveBar.State(autoSave, store.IsBusy, enabled, true, saveError, retry, message);
            config.SetState(state);
            setting.SetState(state);
            foreach (var field in fields)
                field.Toggle.SetIsOnWithoutNotify(field.First ? draft.Hints : draft.Sound);
        }

        public void Unmount()
        {
            StopSystemThemeWatch();
            customEditor?.Hide();
            customEditor = null;
            customPalette?.Dispose();
            customPalette = null;
            customColors = null;
            background = null;
            if (store != null)
            {
                store.Changed -= Render;
                store.Dispose();
                store = null;
            }
            fields.Clear();
            themeButtons.Clear();
            foreach (var binding in colorBindings)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(binding);
                else UnityEngine.Object.DestroyImmediate(binding);
            }
            colorBindings.Clear();
            if (root == null)
                return;
            root.gameObject.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(root.gameObject);
            else UnityEngine.Object.DestroyImmediate(root.gameObject);
            root = null;
        }
    }
}
