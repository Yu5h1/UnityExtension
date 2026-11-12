
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Exercises the public ChatPanel from a mock host using local replies and a local picture.</summary>
    public sealed class UxUiChatPanelDemo : MonoBehaviour
    {
        private const int HorizontalInset = 16;
        private const float ControlGap = 6f;
        private const float ToolbarButtonWidth = 72f;
        private const float ToolbarButtonHeight = 36f;
        private const float ComposerMinHeight = 44f;

        private static readonly UguiControlPalette Palette = UguiControlPalette.Dark;

        private ChatPanel panel;
        private GridLayoutGroup toolbar;
        private InputField input;
        private InputFieldState draftState;
        private Text hostStatus;
        private Font font;
        private Texture2D texture;
        private Sprite image;
        private Button send;
        private Button quickQuestion;
        private bool answering;

        public ChatPanel Panel => panel;

        public void Build()
        {
            if (panel != null)
                return;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var background = gameObject.AddComponent<Image>();
            background.color = Palette.PageBackground;
            var stack = gameObject.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(HorizontalInset, HorizontalInset, 12, 12);
            stack.spacing = 8f;
            stack.childControlHeight = stack.childControlWidth = true;
            stack.childForceExpandHeight = false;
            stack.childForceExpandWidth = true;

            var controls = AddRect("Demo controls", transform);
            controls.gameObject.AddComponent<LayoutElement>().flexibleHeight = 0f;
            toolbar = controls.gameObject.AddComponent<GridLayoutGroup>();
            toolbar.spacing = Vector2.one * ControlGap;
            toolbar.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            AddButton(controls, "短對話", ToolbarButtonHeight, () => ResetHistory(false));
            AddButton(controls, "載入歷史", ToolbarButtonHeight, () => ResetHistory(true));
            AddButton(controls, "回到最新", ToolbarButtonHeight, () => panel.ScrollView.RequestFollow());
            var motion = AddButton(controls, "減少動態", ToolbarButtonHeight, () => panel.ReduceMotion = !panel.ReduceMotion);
            ReflowToolbar();
            motion.onClick.AddListener(() =>
                motion.GetComponentInChildren<Text>().text = panel.ReduceMotion ? "減少動態 ✓" : "減少動態");

            var scrollArea = AddRect("ChatPanel", transform);
            scrollArea.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            panel = scrollArea.gameObject.AddComponent<ChatPanel>();
            panel.Configure(new ChatPanel.Options
            {
                Font = font,
                RowMaxWidthPercent = 70f,
                GraphemesPerSecond = 20f,
                ShowThinkingSeconds = true,
                ShowTimestamps = true
            });

            hostStatus = AddLabel(transform, "選擇一個操作以開始", 12, TextAnchor.MiddleLeft);
            hostStatus.color = Palette.SecondaryText;
            hostStatus.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            var composer = AddComposerRow();
            quickQuestion = AddButton(composer, "角色貼文", ComposerMinHeight, () => StartCoroutine(AskForPost()));
            var viewport = AddRect("Draft viewport", composer);
            viewport.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            viewport.gameObject.AddComponent<Image>().color = Palette.PanelBackground;

            var field = AddRect("Sample input", viewport);
            field.anchorMin = Vector2.zero;
            field.anchorMax = Vector2.one;
            field.offsetMin = field.offsetMax = Vector2.zero;
            var hitArea = field.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            var draft = AddLabel(field, "", 15, TextAnchor.UpperLeft);
            var hint = AddLabel(field, "輸入訊息", 15, TextAnchor.UpperLeft);
            PlaceLabel(draft.rectTransform, 8f);
            PlaceLabel(hint.rectTransform, 8f);
            draft.horizontalOverflow = HorizontalWrapMode.Wrap;
            draft.verticalOverflow = VerticalWrapMode.Truncate;
            draft.supportRichText = false;
            input = field.gameObject.AddComponent<InputField>();
            input.targetGraphic = hitArea;
            input.textComponent = draft;
            input.placeholder = hint;
            input.lineType = InputField.LineType.MultiLineNewline;
            Palette.ApplyField(input, border: false);
            draftState = field.gameObject.AddComponent<InputFieldState>();
            viewport.gameObject.AddComponent<MultilineInputField>().Configure(input, minHeight: ComposerMinHeight, maxVisibleLines: 5);

            send = AddButton(composer, "送出", ComposerMinHeight, Send, accent: true);
            draftState.Changed += _ => UpdateActions();
            ResetHistory(false);
        }

        private void ResetHistory(bool longHistory)
        {
            StopAllCoroutines();
            answering = false;
            DateTime start = DateTime.Today.AddDays(-1).AddHours(21);
            if (longHistory)
            {
                ChatPanel.ImageMessage delayed = null;
                panel.ReplaceHistory(history =>
                {
                    AddIntro(history, start);
                    for (int i = 0; i < 20; i++)
                        history.AddMessage($"第 {i + 1} 則歷史訊息。可以向上捲動，觀察畫面是否保留閱讀位置。",
                            i % 3 == 0, DateTime.Today.AddHours(9).AddMinutes(i + 1), $"history-{i}", true);
                    delayed = history.AddImage(description: "長對話中的圖片", id: "history-image");
                    for (int i = 0; i < 8; i++)
                        history.AddMessage($"載入中的圖片下方 · {i + 1}", id: $"after-image-{i}", fromHistory: true);
                });
                StartCoroutine(FinishImage(delayed, 2f, false));
                hostStatus.text = "載入歷史後可往上捲，再操作「回到最新」。";
            }
            else
            {
                panel.ClearMessages();
                AddIntro(panel, start);
                hostStatus.text = "短對話貼底。送出訊息可試逐字回覆。";
            }
            UpdateActions();
        }

        private static void AddIntro(ChatPanel chat, DateTime start)
        {
            chat.AddMessage("晚安。這是昨日的聊天紀錄。", timestamp: start, id: "intro-yesterday", fromHistory: true);
            chat.AddMessage("來看看今天的對話。", true, DateTime.Today.AddHours(9), "intro-today", true);
        }

        private void UpdateActions()
        {
            send.interactable = !answering && draftState.IsInteractable && draftState.HasNonWhitespace;
            quickQuestion.interactable = !answering;
        }

        private void Send()
        {
            if (answering || !draftState.IsInteractable || !draftState.HasNonWhitespace)
                return;
            panel.AddMessage(draftState.Value, true, DateTime.Now);
            input.text = "";
            StartCoroutine(Answer());
        }

        private IEnumerator Answer()
        {
            answering = true;
            UpdateActions();
            var reply = panel.BeginReply(DateTime.Now);
            reply.RevealCompleted += () =>
            {
                hostStatus.text = "收到一次揭露完成通知。";
                answering = false;
                UpdateActions();
            };
            hostStatus.text = "模擬回覆等待 6 秒";
            yield return new WaitForSecondsRealtime(6f);
            reply.Push("收到你的訊息。");
            yield return new WaitForSecondsRealtime(.65f);
            reply.Complete("收到你的訊息。這則回覆可以依照字速逐字顯示，並在換行時增加泡泡高度。");
        }

        private IEnumerator AskForPost()
        {
            answering = true;
            UpdateActions();
            panel.AddMessage("看看角色貼文", true, DateTime.Now);
            panel.AddMessage("這裡有一張照片。", timestamp: DateTime.Now);
            var post = panel.AddImage(new Vector2(480f, 300f), "角色貼文圖片", DateTime.Now);
            hostStatus.text = "載入兩張圖片，第二張模擬失敗。";
            yield return FinishImage(post, 1.25f, false);
            yield return FinishImage(panel.AddImage(description: "無法載入的圖片"), 1.25f, true);
            hostStatus.text = "點開已載入的圖片可試預覽與滿版。";
            answering = false;
            UpdateActions();
        }

        private IEnumerator FinishImage(ChatPanel.ImageMessage message, float delay, bool fail)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (fail)
                message.SetFailed();
            else
                message.SetLoaded(GetPicture());
        }

        private Sprite GetPicture()
        {
            if (image != null)
                return image;
            const int width = 480;
            const int height = 300;
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var colors = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float ridge = 75f + 50f * Mathf.Sin(x * .011f) + x * .06f;
                    colors[y * width + x] = y < ridge ? new Color32(31, 74, 82, 255)
                        : Color32.Lerp(new Color32(250, 186, 117, 255), new Color32(76, 103, 160, 255), y / (float)height);
                }
            }
            texture.SetPixels32(colors);
            texture.Apply(false, true);
            image = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f));
            return image;
        }

        private void OnDestroy()
        {
            if (image != null)
                Destroy(image);
            if (texture != null)
                Destroy(texture);
        }

        private void OnRectTransformDimensionsChange() => ReflowToolbar();

        private void ReflowToolbar()
        {
            if (toolbar == null)
                return;
            float width = Mathf.Max(0f, ((RectTransform)transform).rect.width - 2f * HorizontalInset);
            int count = toolbar.transform.childCount;
            int columns = Mathf.Clamp(Mathf.FloorToInt((width + ControlGap) / (ToolbarButtonWidth + ControlGap)),
                1, Mathf.Max(1, count));
            toolbar.constraintCount = columns;
            toolbar.cellSize = new Vector2(Mathf.Max(0f, (width - (columns - 1) * ControlGap) / columns),
                ToolbarButtonHeight);
        }

        private RectTransform AddComposerRow()
        {
            var rect = AddRect("Sample host composer", transform);
            var size = rect.gameObject.AddComponent<LayoutElement>();
            size.minHeight = ComposerMinHeight;
            size.flexibleHeight = 0f;
            var row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = ControlGap;
            row.childAlignment = TextAnchor.LowerLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            return rect;
        }

        private Button AddButton(Transform parent, string text, float height, Action click, bool accent = false)
        {
            var addon = ButtonAddon.Create(parent, label: text, showIcon: false, size: height);
            addon.name = text;
            addon.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(4, 4, 0, 0);
            addon.labelText.fontSize = 13;
            addon.labelText.alignment = TextAnchor.MiddleCenter;
            addon.labelText.resizeTextForBestFit = true;
            addon.labelText.resizeTextMinSize = 11;
            addon.labelText.resizeTextMaxSize = 13;
            Palette.ApplyButton(addon.ui, addon.labelText, accent: accent);
            addon.ui.onClick.AddListener(() => click());
            return addon.ui;
        }

        private Text AddLabel(Transform parent, string value, int size, TextAnchor alignment)
        {
            var label = AddRect("Label", parent).gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.text = value;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform AddRect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void PlaceLabel(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, 1f);
            rect.offsetMax = new Vector2(-inset, -1f);
        }
    }
}
