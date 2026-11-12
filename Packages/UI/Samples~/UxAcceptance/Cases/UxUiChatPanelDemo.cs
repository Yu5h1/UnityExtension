
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
        private const int EarlierHistoryBatchSize = 4;
        private const float ControlGap = 6f;
        private const float ToolbarButtonWidth = 60f;
        private const float ToolbarButtonHeight = 36f;
        private const float ComposerMinHeight = 44f;
        private const float SendSize = 38f;
        private const int BodyFontSize = 14;
        private const float BodyLineSpacing = 1.3125f;
        private const string ArtPath = "UxAcceptance/ChatPanel/";

        private static readonly UguiControlPalette Palette = UguiControlPalette.Dark;

        [SerializeField] private ChatPanel chatPanelPrefab;

        private ChatPanel panel;
        private GridLayoutGroup toolbar;
        private InputFieldAddon input;
        private InputFieldState draftState;
        private Text hostStatus;
        private Font font;
        private Sprite image;
        private Button send;
        private CanvasGroup sendAppearance;
        private Button quickQuestion;
        private int earlierHistoryCount;
        private bool answering;

        public ChatPanel Panel => panel;

        public void Build()
        {
            if (panel != null)
                return;
            if (chatPanelPrefab == null)
                throw new InvalidOperationException("請在聊天展示 prefab 指定 Yu5h1Lib.UI 的 ChatPanel prefab。");

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
            AddButton(controls, "加入更早歷史", ToolbarButtonHeight, PrependEarlierHistory);
            AddButton(controls, "回到最新", ToolbarButtonHeight, () => panel.ScrollView.RequestFollow());
            var motion = AddButton(controls, "減少動態", ToolbarButtonHeight, () => panel.ReduceMotion = !panel.ReduceMotion);
            quickQuestion = AddButton(controls, "角色貼文", ToolbarButtonHeight, () => StartCoroutine(AskForPost()));
            ReflowToolbar();
            motion.onClick.AddListener(() =>
                motion.GetComponentInChildren<Text>().text = panel.ReduceMotion ? "減少動態 ✓" : "減少動態");

            hostStatus = AddLabel(transform, "選擇一個操作以開始", 12, TextAnchor.MiddleLeft);
            hostStatus.color = Palette.SecondaryText;
            hostStatus.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            var conversation = AddRect("Conversation", transform);
            conversation.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var conversationLayout = conversation.gameObject.AddComponent<VerticalLayoutGroup>();
            conversationLayout.spacing = ControlGap;
            conversationLayout.childControlHeight = conversationLayout.childControlWidth = true;
            conversationLayout.childForceExpandHeight = false;
            conversationLayout.childForceExpandWidth = true;

            panel = Instantiate(chatPanelPrefab, conversation, false);
            panel.name = chatPanelPrefab.name;
            panel.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            panel.Configure(new ChatPanel.Options
            {
                Font = font,
                RowMaxWidthPercent = 70f,
                GraphemesPerSecond = 20f,
                ShowThinkingSeconds = true,
                ShowTimestamps = true,
                MessageFontSize = BodyFontSize,
                MessageLineSpacing = BodyLineSpacing,
                MessageMinLineHeight = 21f,
                MessageBottomInset = 8,
                TimestampWidth = 24f,
                MetadataColor = new Color(Palette.ItemText.r, Palette.ItemText.g, Palette.ItemText.b, .65f),
                Self = new ChatPanel.BubbleStyle { Background = Palette.Accent, Text = Palette.OnAccent },
                Other = new ChatPanel.BubbleStyle { Background = Palette.PanelBackground, Text = Palette.ItemText },
                ViewerSafeArea = conversation,
                ViewerBackdropOpacity = .84f,
                ViewerCloseSprite = Resources.Load<Sprite>(ArtPath + "Disc"),
                ViewerPalette = Palette
            });

            var composer = AddComposerRow(conversation);
            var viewport = AddRect("Draft viewport", composer);
            viewport.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var field = AddRect("Sample input", viewport);
            field.anchorMin = Vector2.zero;
            field.anchorMax = Vector2.one;
            field.offsetMin = field.offsetMax = Vector2.zero;
            var hitArea = field.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            var draft = AddLabel(field, "", BodyFontSize, TextAnchor.MiddleLeft);
            var hint = AddLabel(field, "想說什麼?", BodyFontSize, TextAnchor.MiddleLeft);
            draft.lineSpacing = BodyLineSpacing;
            PlaceLabel(draft.rectTransform);
            PlaceLabel(hint.rectTransform);
            draft.horizontalOverflow = HorizontalWrapMode.Wrap;
            draft.verticalOverflow = VerticalWrapMode.Truncate;
            draft.supportRichText = false;
            input = InputFieldAddon.Create(field, draft, hint, hitArea, ComposerMinHeight - 6f, 5, 8f);
            input.Multiline = true;
            Palette.ApplyField((InputField)input.Raw, border: false);
            draftState = field.gameObject.AddComponent<InputFieldState>();

            send = AddSendButton(composer);
            input.Submitted += _ => Send();
            draftState.Changed += _ => UpdateActions();
            ResetHistory(false);
        }

        private void ResetHistory(bool longHistory)
        {
            StopAllCoroutines();
            answering = false;
            earlierHistoryCount = 0;
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
                hostStatus.text = "往上捲後按「加入更早歷史」，可檢查閱讀位置。";
            }
            else
            {
                panel.ClearMessages();
                AddIntro(panel, start);
                hostStatus.text = "短對話貼底。送出訊息可試逐字回覆。";
            }
            UpdateActions();
        }

        private void PrependEarlierHistory()
        {
            var content = panel.MessageContent;
            int firstNewRow = content.childCount;
            for (int i = 0; i < EarlierHistoryBatchSize; i++)
            {
                int number = ++earlierHistoryCount;
                panel.AddMessage($"更早的聊天紀錄 · {number}", self: number % 2 == 0,
                    id: $"earlier-history-{number}", fromHistory: true);
            }
            for (int i = 0; i < EarlierHistoryBatchSize; i++)
                content.GetChild(firstNewRow + i).SetAsFirstSibling();

            panel.ScrollView.RefreshContent();
            hostStatus.text = $"已在前方加入 {EarlierHistoryBatchSize} 則歷史訊息。";
        }

        private static void AddIntro(ChatPanel chat, DateTime start)
        {
            chat.AddMessage("晚安。這是昨日的聊天紀錄。", timestamp: start, id: "intro-yesterday", fromHistory: true);
            chat.AddMessage("來看看今天的對話。", true, DateTime.Today.AddHours(9), "intro-today", true);
        }

        private void UpdateActions()
        {
            send.interactable = !answering && draftState.IsInteractable && draftState.HasNonWhitespace;
            sendAppearance.alpha = send.interactable ? 1f : .5f;
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
            var picture = GetPicture();
            var post = panel.AddImage(picture != null ? picture.rect.size : default, "角色貼文圖片", DateTime.Now);
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
            var picture = fail ? null : GetPicture();
            if (picture != null)
                message.SetLoaded(picture);
            else
                message.SetFailed();
        }

        private Sprite GetPicture()
        {
            if (image == null)
                image = Resources.Load<Sprite>(ArtPath + "Scene");
            return image;
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

        private RectTransform AddComposerRow(Transform conversation)
        {
            var slot = AddRect("Composer slot", conversation);
            var inset = slot.gameObject.AddComponent<HorizontalLayoutGroup>();
            inset.padding = new RectOffset(6, 6, 0, 0);
            inset.childControlWidth = inset.childControlHeight = true;
            inset.childForceExpandWidth = true;
            inset.childForceExpandHeight = false;

            var rect = AddRect("Sample host composer", slot);
            rect.gameObject.AddComponent<Image>().color = Palette.PanelBackground;
            var size = rect.gameObject.AddComponent<LayoutElement>();
            size.minHeight = ComposerMinHeight;
            size.flexibleHeight = 0f;
            var row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(14, 4, 3, 3);
            row.spacing = 4f;
            row.childAlignment = TextAnchor.LowerLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            return rect;
        }

        private Button AddSendButton(Transform composer)
        {
            var rect = AddRect("送出", composer);
            var size = rect.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = SendSize;
            size.minHeight = size.preferredHeight = SendSize;
            var disc = rect.gameObject.AddComponent<Image>();
            disc.sprite = Resources.Load<Sprite>(ArtPath + "SendDisc");
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = disc;
            button.onClick.AddListener(Send);
            Palette.ApplyButton(button, accent: true, border: false);
            var colors = button.colors;
            colors.disabledColor = colors.normalColor;
            button.colors = colors;

            var glyph = AddRect("Envelope", rect);
            glyph.sizeDelta = Vector2.one * 26f;
            var icon = glyph.gameObject.AddComponent<Image>();
            icon.sprite = Resources.Load<Sprite>(ArtPath + "Envelope");
            icon.raycastTarget = false;
            sendAppearance = rect.gameObject.AddComponent<CanvasGroup>();
            return button;
        }

        private Button AddButton(Transform parent, string text, float height, Action click)
        {
            var addon = ButtonAddon.Create(parent, label: text, showIcon: false, size: height);
            addon.name = text;
            addon.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(4, 4, 0, 0);
            addon.labelText.fontSize = 13;
            addon.labelText.alignment = TextAnchor.MiddleCenter;
            addon.labelText.resizeTextForBestFit = true;
            addon.labelText.resizeTextMinSize = 11;
            addon.labelText.resizeTextMaxSize = 13;
            Palette.ApplyButton(addon.ui, addon.labelText);
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

        private static void PlaceLabel(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(0f, 2f);
            rect.offsetMax = new Vector2(0f, -3f);
        }
    }
}
