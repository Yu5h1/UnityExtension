using System;
using UnityEngine;
using UnityEngine.UI;
using static Yu5h1Lib.UI.SettingsLayout;

namespace Yu5h1Lib.UI
{
    /// <summary>Displays host-owned save state and requests changes through native controls.</summary>
    public sealed class SaveBar : MonoBehaviour
    {
        public readonly struct State
        {
            public readonly bool AutoSave, Busy, Enabled, CanSave, RetrySave, RetryApply, LockMode;
            public readonly string Message;

            public State(bool autoSave = true, bool busy = false, bool enabled = true, bool canSave = true,
                bool retrySave = false, bool retryApply = false, string message = null, bool lockMode = false)
            {
                AutoSave = autoSave;
                Busy = busy;
                Enabled = enabled;
                CanSave = canSave;
                RetrySave = retrySave;
                RetryApply = retryApply;
                Message = message;
                LockMode = lockMode;
            }
        }

        private State state;
        private ButtonAddon save;
        private Text message;
        public RectTransform Actions { get; private set; }
        public Button SaveButton => save.ui;
        public Toggle ModeToggle { get; private set; }
        public event Action<bool> AutoSaveRequested;
        public event Action SaveRequested;
        public event Action RetryApplyRequested;

        public static SaveBar Create(Transform parent, UguiControlPalette palette = null)
        {
            palette ??= UguiControlPalette.Light;
            var root = Rect(parent, "Save bar");
            Layout<VerticalLayoutGroup>(root, 4).childForceExpandWidth = true;
            var bar = root.gameObject.AddComponent<SaveBar>();
            bar.message = MakeText(root, "Status", "", palette);
            bar.message.color = palette.SecondaryText;
            Size(bar.message, height: 26);
            var row = Rect(root, "Save actions");
            var toolbar = Layout<HorizontalLayoutGroup>(row, 6);
            toolbar.childAlignment = TextAnchor.MiddleRight;
            bar.Actions = Rect(row, "Host actions");
            Layout<HorizontalLayoutGroup>(bar.Actions, 6);
            Size(bar.Actions, flexibleWidth: 1);
            bar.ModeToggle = CreateToggle(row, palette);
            bar.save = MakeButton(row, "Save", "套用並保存", palette);
            Size(bar.save, 120, 36);
            bar.ModeToggle.onValueChanged.AddListener(bar.RequestMode);
            bar.SaveButton.onClick.AddListener(bar.RequestSave);
            bar.SetState(new State(autoSave: true));
            return bar;
        }

        public void SetState(State value)
        {
            state = value;
            ModeToggle.SetIsOnWithoutNotify(state.AutoSave);
            ModeToggle.interactable = state.Enabled && !state.Busy && !state.RetryApply && !state.LockMode;
            SaveButton.interactable = state.Enabled && !state.Busy
                && (state.RetryApply || state.CanSave) && (!state.AutoSave || state.RetrySave || state.RetryApply);
            save.labelText.text = state.Busy ? "處理中…" : state.RetryApply ? "重試套用"
                : state.RetrySave ? "重試儲存" : "套用並保存";
            message.text = state.Message ?? "";
            message.gameObject.SetActive(!string.IsNullOrEmpty(state.Message));
        }

        private void RequestMode(bool next)
        {
            ModeToggle.SetIsOnWithoutNotify(state.AutoSave);
            if (ModeToggle.IsInteractable() && next != state.AutoSave)
                AutoSaveRequested?.Invoke(next);
        }

        private void RequestSave()
        {
            if (!SaveButton.IsInteractable())
                return;
            if (state.RetryApply)
                RetryApplyRequested?.Invoke();
            else
                SaveRequested?.Invoke();
        }

        private static Toggle CreateToggle(Transform parent, UguiControlPalette palette)
        {
            var root = Rect(parent, "自動儲存");
            Size(root, 90, 36);
            var toggle = root.gameObject.AddComponent<Toggle>();
            var box = Rect(root, "Checkbox");
            box.anchorMin = box.anchorMax = new Vector2(0, .5f);
            box.sizeDelta = new Vector2(18, 18);
            box.anchoredPosition = new Vector2(9, 0);
            var background = box.gameObject.AddComponent<Image>();
            background.color = palette.Hover;
            var check = Rect(box, "Checked");
            Stretch(check);
            check.offsetMin = new Vector2(3, 3);
            check.offsetMax = new Vector2(-3, -3);
            var mark = check.gameObject.AddComponent<Image>();
            mark.color = palette.Accent;
            mark.raycastTarget = false;
            var label = MakeText(root, "Label", "自動儲存", palette);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(23, 0);
            label.raycastTarget = true;
            toggle.targetGraphic = background;
            toggle.graphic = mark;
            return toggle;
        }
    }
}
