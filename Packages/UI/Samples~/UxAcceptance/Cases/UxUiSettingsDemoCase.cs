using System;
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
        private readonly Func<string, DataView, CancellationToken, Task> write;
        private readonly Func<bool, bool, CancellationToken, Task> apply;
        private RectTransform root;
        private SettingsPanel config;
        private SettingsPanel setting;
        private Text preview;
        private ConfigStore<Values> store;
        private Values draft;
        private readonly System.Collections.Generic.List<(Toggle Toggle, bool First)> fields = new();
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

        /// <summary>Uses an alternate writer for the demo scope and an optional application target for the two settings.</summary>
        public UxUiSettingsDemoCase(string key, Func<string, DataView, CancellationToken, Task> write,
            Func<bool, bool, CancellationToken, Task> apply = null)
        {
            this.key = key;
            this.write = write ?? throw new ArgumentNullException(nameof(write));
            this.apply = apply;
        }

        public void Mount(RectTransform container)
        {
            Unmount();
            autoSave = true;
            root = CreateRect(container, "SettingsDemo");
            Stretch(root, 12);
            var actions = CreateRect(root, "Settings entries");
            Stretch(actions);
            preview = CreateText(actions, "Applied", "", 18, Palette.ItemText, TextAnchor.MiddleCenter);
            Stretch(preview.rectTransform);
            var direction = ButtonAddon.Create(actions, label: "橫列／直欄", showIcon: false).ui;
            var directionRect = (RectTransform)direction.transform;
            directionRect.anchorMin = directionRect.anchorMax = new Vector2(0, 1);
            directionRect.pivot = new Vector2(0, 1);
            directionRect.sizeDelta = new Vector2(138, 36);
            direction.onClick.AddListener(() =>
            {
                var next = config.Tabs.Orientation == Tabs.Direction.Row ? Tabs.Direction.Column : Tabs.Direction.Row;
                config.Tabs.Orientation = setting.Tabs.Orientation = next;
            });

            config = SettingsPanel.Create(root, SettingsPanel.Presentation.Config, Palette);
            setting = SettingsPanel.Create(root, SettingsPanel.Presentation.Setting, Palette);
            ((RectTransform)setting.Opener.transform).anchoredPosition = new Vector2(-48, 0);
            ((RectTransform)setting.Opener.transform).sizeDelta = new Vector2(46, 36);
            setting.Opener.name = "提示設定";
            config.SetGroups(new[] { CreateGroup(config, Choice.Hints), CreateGroup(config, Choice.Sound) });
            setting.SetGroups(new[] { CreateGroup(setting, Choice.Hints) });
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
                16, Palette.ItemText, TextAnchor.UpperLeft);
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
            background.color = Palette.Hover;
            var check = CreateRect(box, "Check");
            Stretch(check, 3);
            var mark = check.gameObject.AddComponent<Image>();
            mark.color = Palette.Accent;
            mark.raycastTarget = false;
            var caption = CreateText(row, "Label", label, 16, Palette.ItemText, TextAnchor.MiddleLeft);
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
            if (store != null)
            {
                store.Changed -= Render;
                store.Dispose();
                store = null;
            }
            fields.Clear();
            if (root == null)
                return;
            root.gameObject.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(root.gameObject);
            else UnityEngine.Object.DestroyImmediate(root.gameObject);
            root = null;
        }
    }
}
