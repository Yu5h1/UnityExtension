using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib.Serialization;
using static Yu5h1Lib.UI.SettingsLayout;

namespace Yu5h1Lib.UI
{
    /// <summary>Hosts the chat performance fields in the common Config and applies the accepted values to the chat.</summary>
    public sealed class ChatTextPerformanceSettings : MonoBehaviour
    {
        private SettingsPanel config;
        private ConfigStore<ChatTextPerformance> store;
        private Button retryLoad;
        private ChatTextPerformance draft;
        private bool autoSave = true;
        private string fieldError;
        private event Action RefreshFields;

        public static SettingsPanel Mount(RectTransform host, ChatPanel chat, string storageKey,
            UguiControlPalette palette = null)
        {
            if (host == null || chat == null || string.IsNullOrWhiteSpace(storageKey))
                throw new ArgumentException("A host, chat panel and settings key are required.");

            palette ??= UguiControlPalette.Light;
            var config = SettingsPanel.Create(host, SettingsPanel.Presentation.Config, palette);
            var settings = config.gameObject.AddComponent<ChatTextPerformanceSettings>();
            settings.Initialize(config, chat, storageKey, palette);
            return config;
        }

        private void Initialize(SettingsPanel surface, ChatPanel chat, string key, UguiControlPalette palette)
        {
            config = surface;
            config.Opener.name = "聊天設定";
            var storage = Preferences.DefaultStorage;
            store = new ConfigStore<ChatTextPerformance>(chat.TextPerformance, value => value, value => value.Validate(),
                _ =>
                {
                    var loaded = storage.Load(key);
                    if (loaded.Status == PreferencesLoadStatus.Error)
                        throw new InvalidOperationException(loaded.Error);
                    return Task.FromResult(loaded.Status == PreferencesLoadStatus.Missing
                        ? (false, default(ChatTextPerformance))
                        : (true, JsonUtility.FromJson<ChatTextPerformance>(loaded.Data["textPerformance"])));
                },
                (value, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    storage.Save(key, new DataView { ["textPerformance"] = JsonUtility.ToJson(value) });
                    return Task.CompletedTask;
                },
                (value, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    chat.TextPerformance = value;
                    return Task.CompletedTask;
                });
            draft = store.Defaults;

            var page = Rect(config.transform, "Text performance");
            var scroll = Scroll(page, out var content);
            ConfigureScroll(scroll, horizontal: false);
            BuildFields(content, palette);
            config.SetGroups(new[] { new SettingsPanel.Group("chat-panel", "聊天",
                new[] { new Tabs.Item("text-performance", "文字表演", page) }) });
            var retry = MakeButton(config.SaveBar.Actions, "Retry load", "重新讀取");
            palette.ApplyButton(retry.ui, retry.labelText);
            retryLoad = retry.ui;
            retryLoad.onClick.AddListener(() =>
            {
                if (store.CurrentStatus == ConfigStore<ChatTextPerformance>.Status.LoadError)
                    _ = store.LoadAsync();
            });

            config.Closed += () =>
            {
                fieldError = null;
                if (store.CurrentStatus != ConfigStore<ChatTextPerformance>.Status.SaveError)
                    draft = store.Accepted;
                Render();
            };
            config.ResetRequested += () => Edit(store.Defaults);
            config.SaveBar.AutoSaveRequested += enabled =>
            {
                autoSave = enabled;
                Edit(draft);
            };
            config.SaveBar.SaveRequested += () => _ = store.SaveAsync(draft);
            config.SaveBar.RetryApplyRequested += () => _ = store.RetryApplyAsync();
            store.Changed += Render;
            _ = store.LoadAsync();
        }

        private void BuildFields(RectTransform content, UguiControlPalette palette)
        {
            Layout<VerticalLayoutGroup>(content, 6).childForceExpandWidth = true;
            var typewriter = AddGroup(content, palette, "Typewriter", "逐字", v => !v.Instant,
                (v, enabled) => { v.Instant = !enabled; return v; });
            AddNumber(typewriter, palette, "字速（秒／字素）", v => v.SecondsPerGrapheme,
                (v, n) => { v.SecondsPerGrapheme = n; return v; });

            var fade = AddGroup(content, palette, "Fade in", "淡入", v => v.Fade,
                (v, enabled) => { v.Fade = enabled; return v; });
            AddToggle(fade, palette, "淡入依序觸發", "依序觸發", v => v.FadeSequential,
                (v, enabled) => { v.FadeSequential = enabled; return v; }, v => !v.Instant);

            var move = AddGroup(content, palette, "Move in", "位移歸位", v => v.Move,
                (v, enabled) => { v.Move = enabled; return v; });
            AddToggle(move, palette, "位移依序觸發", "依序觸發", v => v.MoveSequential,
                (v, enabled) => { v.MoveSequential = enabled; return v; }, v => !v.Instant);
            AddToggle(move, palette, "指定方向", "指定方向", v => v.Directed,
                (v, enabled) => { v.Directed = enabled; return v; });
            var omnidirectional = AddCaption(move, palette, "Direction mode", "全方位");
            omnidirectional.color = palette.SecondaryText;
            var direction = AddNumber(move, palette, "方向（度，向右為 0°）", v => v.DirectionDegrees,
                (v, n) => { v.DirectionDegrees = n; return v; });
            RefreshFields += () =>
            {
                omnidirectional.gameObject.SetActive(!draft.Directed);
                direction.gameObject.SetActive(draft.Directed);
            };
            AddRange(move, palette, "距離（px）", "最小", "最大", v => v.DistanceMin, v => v.DistanceMax,
                (v, n) => { v.DistanceMin = n; return v; }, (v, n) => { v.DistanceMax = n; return v; });

            var shared = Rect(fade, "Shared performance");
            Layout<VerticalLayoutGroup>(shared, 4).childForceExpandWidth = true;
            AddNumber(shared, palette, "表演比例（%）", v => v.Percentage,
                (v, n) => { v.Percentage = n; return v; });
            AddRange(shared, palette, "時間（秒）", "最短", "最長", v => v.DurationMin, v => v.DurationMax,
                (v, n) => { v.DurationMin = n; return v; }, (v, n) => { v.DurationMax = n; return v; });
            var sharedHint = AddCaption(shared, palette, "Shared values", "比例與時間為淡入及位移共用。");
            sharedHint.color = palette.SecondaryText;
            RefreshFields += () =>
            {
                var parent = draft.Fade || !draft.Move ? fade : move;
                if (shared.parent != parent)
                    shared.SetParent(parent, false);
                sharedHint.gameObject.SetActive(draft.Fade && draft.Move);
            };
        }

        private RectTransform AddGroup(Transform parent, UguiControlPalette palette, string name, string label,
            Func<ChatTextPerformance, bool> read, Func<ChatTextPerformance, bool, ChatTextPerformance> write)
        {
            var group = Rect(parent, name);
            Layout<VerticalLayoutGroup>(group, 3).childForceExpandWidth = true;
            AddToggle(group, palette, label, label, read, write, heading: true);
            var body = Rect(group, "Fields");
            var layout = Layout<VerticalLayoutGroup>(body, 4);
            layout.padding = new RectOffset(8, 4, 0, 4);
            layout.childForceExpandWidth = true;
            RefreshFields += () => body.gameObject.SetActive(read(draft));
            return body;
        }

        private void Edit(ChatTextPerformance next)
        {
            fieldError = null;
            try { next.Validate(); }
            catch (ArgumentOutOfRangeException)
            {
                RejectNumber();
                return;
            }
            draft = next;
            Render();
            if (autoSave && !draft.Equals(store.Accepted))
                _ = store.SaveAsync(draft);
        }

        private void RejectNumber()
        {
            fieldError = "請輸入有效數值，最短或最小值須小於等於最大值。";
            Render();
        }

        private void Render()
        {
            var status = store.CurrentStatus;
            if (status == ConfigStore<ChatTextPerformance>.Status.Disposed)
                return;
            if (status == ConfigStore<ChatTextPerformance>.Status.Applying)
                draft = store.Accepted;
            bool loadError = status == ConfigStore<ChatTextPerformance>.Status.LoadError;
            bool saveError = status == ConfigStore<ChatTextPerformance>.Status.SaveError;
            bool retryApply = status == ConfigStore<ChatTextPerformance>.Status.ApplyError;
            string message = loadError ? "讀取失敗，原設定已保留。" :
                saveError ? "保存失敗。" : retryApply ? "已保存，尚未套用。" : fieldError;
            config.SetState(new SaveBar.State(autoSave, store.IsBusy, !loadError,
                saveError || !draft.Equals(store.Accepted), saveError, retryApply, message));
            retryLoad.gameObject.SetActive(loadError);
            config.SaveBar.ModeToggle.gameObject.SetActive(!loadError);
            config.SaveBar.SaveButton.gameObject.SetActive(!loadError);
            RefreshFields?.Invoke();
        }

        private void AddToggle(Transform parent, UguiControlPalette palette, string name, string label,
            Func<ChatTextPerformance, bool> read, Func<ChatTextPerformance, bool, ChatTextPerformance> write,
            Func<ChatTextPerformance, bool> enabled = null, bool heading = false)
        {
            var row = Rect(parent, name);
            Size(row, height: 36);
            var control = Rect(row, "Toggle");
            Stretch(control);
            control.gameObject.AddComponent<Image>().color = heading ? palette.Hover : Color.clear;
            var toggle = control.gameObject.AddComponent<Toggle>();
            var box = Rect(control, "Checkbox");
            box.anchorMin = box.anchorMax = new Vector2(0, .5f);
            box.anchoredPosition = new Vector2(17, 0);
            box.sizeDelta = new Vector2(18, 18);
            var background = box.gameObject.AddComponent<Image>();
            background.color = palette.ControlBorder;
            var check = Rect(box, "Check");
            Stretch(check);
            check.offsetMin = new Vector2(3, 3);
            check.offsetMax = new Vector2(-3, -3);
            var mark = check.gameObject.AddComponent<Image>();
            mark.color = palette.Accent;
            mark.raycastTarget = false;

            var caption = MakeText(control, "Label", label);
            Stretch(caption.rectTransform);
            caption.rectTransform.offsetMin = new Vector2(30, 0);
            caption.rectTransform.offsetMax = new Vector2(-4, 0);
            toggle.targetGraphic = background;
            toggle.graphic = mark;
            UguiControlDecoration.GetOrCreate(toggle).Configure(toggle, palette, caption, palette.ItemText,
                mark, palette.Accent, heading, true, false);
            toggle.onValueChanged.AddListener(value =>
            {
                if (config.CanEdit && toggle.IsActive() && toggle.IsInteractable())
                    Edit(write(draft, value));
            });
            RefreshFields += () =>
            {
                toggle.interactable = enabled?.Invoke(draft) ?? true;
                toggle.SetIsOnWithoutNotify(read(draft));
            };
        }

        private RectTransform AddNumber(Transform parent, UguiControlPalette palette, string label,
            Func<ChatTextPerformance, float> read, Func<ChatTextPerformance, float, ChatTextPerformance> write)
        {
            var row = Rect(parent, label);
            Layout<VerticalLayoutGroup>(row, 2).childForceExpandWidth = true;
            AddCaption(row, palette, "Label", label);
            Number(row, palette, label, read, write);
            return row;
        }

        private void AddRange(Transform parent, UguiControlPalette palette, string label, string minLabel, string maxLabel,
            Func<ChatTextPerformance, float> min, Func<ChatTextPerformance, float> max,
            Func<ChatTextPerformance, float, ChatTextPerformance> writeMin,
            Func<ChatTextPerformance, float, ChatTextPerformance> writeMax)
        {
            var group = Rect(parent, label);
            Layout<VerticalLayoutGroup>(group, 2).childForceExpandWidth = true;
            AddCaption(group, palette, "Label", label);
            var row = Rect(group, "Range");
            var layout = Layout<HorizontalLayoutGroup>(row, 6);
            layout.childForceExpandWidth = true;
            var lower = AddNumber(row, palette, minLabel, min, writeMin);
            var upper = AddNumber(row, palette, maxLabel, max, writeMax);
            Size(lower, width: 0).minWidth = 0;
            Size(upper, width: 0).minWidth = 0;
        }

        private static Text AddCaption(Transform parent, UguiControlPalette palette, string name, string label)
        {
            var caption = MakeText(parent, name, label);
            caption.color = palette.ItemText;
            Size(caption, width: 0).minHeight = 20;
            return caption;
        }

        private void Number(RectTransform row, UguiControlPalette palette, string name,
            Func<ChatTextPerformance, float> read, Func<ChatTextPerformance, float, ChatTextPerformance> write)
        {
            var field = Rect(row, name);
            Size(field, width: 0, height: 32);
            var input = field.gameObject.AddComponent<InputField>();
            input.targetGraphic = field.gameObject.AddComponent<Image>();
            var text = MakeText(field, "Value", "");
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(4, 0);
            text.rectTransform.offsetMax = new Vector2(-4, 0);
            input.textComponent = text;
            palette.ApplyField(input);
            input.onEndEdit.AddListener(value =>
            {
                if (!config.CanEdit || !input.IsActive() || !input.IsInteractable())
                    return;
                if (input.wasCanceled)
                    Render();
                else if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
                    Edit(write(draft, number));
                else
                    RejectNumber();
            });
            RefreshFields += () => input.SetTextWithoutNotify(read(draft).ToString("R", CultureInfo.InvariantCulture));
        }

        private void OnDestroy()
        {
            if (store != null)
            {
                store.Changed -= Render;
                store.Dispose();
            }
        }
    }
}
