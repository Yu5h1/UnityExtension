
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>
    /// A uGUI message area for one-to-one conversations. The host owns input, history data,
    /// asynchronous requests and assets. Call Configure before adding messages.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChatPanel : MonoBehaviour
    {
        [Serializable]
        public sealed class BubbleStyle
        {
            public Color Background;
            public Color Text = Color.white;
            public Sprite BackgroundImage;
        }

        [Serializable]
        public sealed class Options
        {
            public Font Font;
            [Range(1, 100)] public float RowMaxWidthPercent = 85f;
            public bool SelfAlignRight = true;
            public bool ShowTimestamps;
            public bool ShowThinkingSeconds = true;
            [Min(.01f)] public float GraphemesPerSecond = ChatReplyReveal.DefaultRate;
            [Min(1)] public float ImageMaxHeight = 240f;
            [Min(1)] public float ImagePlaceholderSize = 160f;
            public RectTransform ViewerSafeArea;
            [Min(0)] public float ViewerPreviewInset = 16f;
            [Min(0)] public float ViewerZoomGestureThreshold = .1f;
            [Min(0)] public float FollowResumeTolerance = FollowingScrollView.DefaultResumeTolerance;
            public BubbleStyle Self = new() { Background = new Color32(79, 140, 255, 255) };
            public BubbleStyle Other = new()
            {
                Background = new Color32(38, 42, 52, 255),
                Text = new Color32(235, 239, 245, 255)
            };
        }

        public enum ViewerCloseReason { Cancel, Programmatic }
        public enum ViewerDismissSource { Escape, CloseButton, Backdrop, ZoomOut }

        public sealed class Reply
        {
            private readonly ChatPanel panel;
            private readonly ChatReplyReveal reveal = new();
            internal readonly Text Label;
            private bool active = true;

            internal Reply(ChatPanel panel, Text label)
            {
                this.panel = panel;
                Label = label;
            }

            public string Text => Label != null ? Label.text : "";
            public bool IsComplete { get; private set; }
            public event Action RevealCompleted;
            public float Pace { get; set; } = 1f;

            public void Push(string snapshot)
            {
                if (!active || IsComplete)
                    return;
                reveal.Push(snapshot);
                panel.MarkReplyChanged(this);
            }

            public void Complete(string finalSnapshot = null)
            {
                if (!active || IsComplete)
                    return;
                reveal.Complete(finalSnapshot);
                panel.MarkReplyChanged(this);
            }

            internal bool Advance(float dt, out bool completed)
            {
                var options = panel.options;
                string text = reveal.Advance(dt, Mathf.Max(.01f, options.GraphemesPerSecond),
                    Pace, panel.ReduceMotion, options.ShowThinkingSeconds);
                bool changed = Label.text != text;
                if (changed)
                    Label.text = text;
                completed = reveal.Done;
                return changed;
            }

            internal void NotifyCompleted()
            {
                if (!active || IsComplete)
                    return;
                IsComplete = true;
                RevealCompleted?.Invoke();
            }

            internal void Cancel()
            {
                active = false;
                RevealCompleted = null;
            }
        }

        public sealed class ImageMessage
        {
            private readonly ChatPanel panel;
            internal readonly Image Thumbnail;
            internal readonly Button Button;
            internal readonly Text Status;
            internal readonly LayoutElement Size;
            internal readonly string Description;
            internal Vector2 SourceSize;
            private bool pending = true;

            internal ImageMessage(ChatPanel panel, Image thumbnail, Button button, Text status,
                LayoutElement size, Vector2 sourceSize, string description)
            {
                this.panel = panel;
                Thumbnail = thumbnail;
                Button = button;
                Status = status;
                Size = size;
                SourceSize = sourceSize;
                Description = string.IsNullOrWhiteSpace(description) ? "圖片" : description;
            }

            public bool Ready => Button != null && Button.interactable;
            public Vector2 DisplaySize => Size != null ? new Vector2(Size.preferredWidth, Size.preferredHeight) : Vector2.zero;

            public void SetLoaded(Sprite sprite)
            {
                if (!pending || !sprite || !Thumbnail)
                    return;
                pending = false;
                SourceSize = sprite.rect.size;
                Thumbnail.sprite = sprite;
                Thumbnail.color = Color.white;
                Status.gameObject.SetActive(false);
                Button.interactable = true;
                panel.RefreshLayout();
            }

            public void SetFailed()
            {
                if (!pending || !Thumbnail)
                    return;
                pending = false;
                Status.text = "×";
                Button.name = Description + "（無法載入）";
            }

            internal void Cancel() => pending = false;
        }

        internal const float RowGap = 8f;
        internal const float BubblePadding = 8f;
        private const float TimestampWidth = 38f;
        private const float TimestampGap = 4f;

        private readonly List<(LayoutElement Bubble, bool HasTime)> textRows = new();
        private readonly List<(ImageMessage Message, bool HasTime)> imageRows = new();
        private readonly List<Reply> replies = new();
        private readonly List<Reply> completedReplies = new();
        private Options options = new();
        private DateTime? lastDate;
        private FollowingScrollView scroll;
        private RectTransform viewport;
        private RectTransform content;
        private CanvasGroup scrollbarVisibility;
        private ChatImageViewer viewer;
        private float lastWidth = -1f;
        private float lastScrollOffset;
        private float scrollbarIdle;
        private bool replacingHistory;

        public FollowingScrollView ScrollView { get { EnsureView(); return scroll; } }
        public bool ReduceMotion { get; set; }
        public bool ViewerOpen => viewer != null && viewer.gameObject.activeSelf;
        public bool ViewerFull => ViewerOpen && viewer.Full;
        public RectTransform MessageContent { get { EnsureView(); return content; } }
        public Func<ViewerDismissSource, bool> CanDismissImage { get; set; }
        public event Action ImageViewerOpened;
        public event Action<ViewerCloseReason> ImageViewerClosed;

        public void Configure(Options value)
        {
            if (textRows.Count != 0 || imageRows.Count != 0)
                throw new InvalidOperationException("Configure the ChatPanel before adding messages.");
            options = value ?? throw new ArgumentNullException(nameof(value));
            EnsureView();
            scroll.ResumeTolerance = Mathf.Max(0f, options.FollowResumeTolerance);
        }

        public void AddMessage(string text, bool self = false, DateTime? timestamp = null, string id = null,
            bool fromHistory = false)
        {
            EnsureView();
            CreateTextRow(self, timestamp, id).text = text ?? "";
            RefreshLayout();
            if (self && !fromHistory && !replacingHistory)
                scroll.RequestFollow();
        }

        public Reply BeginReply(DateTime? timestamp = null, string id = null)
        {
            if (replacingHistory)
                throw new InvalidOperationException("Begin replies after replacing history.");
            EnsureView();
            var reply = new Reply(this, CreateTextRow(false, timestamp, id));
            replies.Add(reply);
            reply.Advance(0f, out _);
            RefreshLayout();
            scroll.RequestFollow();
            return reply;
        }

        public ImageMessage AddImage(Vector2 originalSize = default, string description = null,
            DateTime? timestamp = null, string id = null)
        {
            EnsureView();
            var row = CreateRow(false, timestamp, id);
            var imageRect = AddRect("Image", row.Rect);
            var thumbnail = imageRect.gameObject.AddComponent<Image>();
            thumbnail.color = options.Other.Background.a > .001f ? options.Other.Background : Color.gray;
            thumbnail.preserveAspect = true;
            var button = imageRect.gameObject.AddComponent<Button>();
            button.targetGraphic = thumbnail;
            button.interactable = false;
            var status = AddLabel("Loading", imageRect, 18, options.Other.Text, TextAnchor.MiddleCenter);
            Stretch(status.rectTransform);
            status.text = "…";
            var message = new ImageMessage(this, thumbnail, button, status,
                imageRect.gameObject.AddComponent<LayoutElement>(), originalSize, description);
            button.name = message.Description;
            button.onClick.AddListener(() => OpenImage(message));
            imageRows.Add((message, row.HasTime));
            AddTimestamp(row, timestamp);
            RefreshLayout();
            return message;
        }

        public void ClearMessages()
        {
            if (replacingHistory)
                throw new InvalidOperationException("History replacement already clears the old rows.");
            EnsureView();
            RemoveRows();
            RefreshLayout();
            scroll.RequestFollow();
            CloseImage();
        }

        /// <summary>
        /// Replaces history in one layout pass. Reuse message IDs to keep the current reader's anchor.
        /// The callback adds only archived messages and their images.
        /// </summary>
        public void ReplaceHistory(Action<ChatPanel> populate)
        {
            if (populate == null)
                throw new ArgumentNullException(nameof(populate));
            if (replacingHistory)
                throw new InvalidOperationException("History replacement cannot be nested.");
            EnsureView();
            replacingHistory = true;
            try
            {
                RemoveRows();
                populate(this);
            }
            finally
            {
                replacingHistory = false;
                RefreshLayout();
                CloseImage();
            }
        }

        private void RemoveRows()
        {
            foreach (var reply in replies)
                reply.Cancel();
            foreach (var image in imageRows)
                image.Message.Cancel();
            replies.Clear();
            textRows.Clear();
            imageRows.Clear();
            lastDate = null;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                child.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    child.SetParent(null, false);
                    Destroy(child.gameObject);
                }
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        public bool OpenImage(ImageMessage message)
        {
            if (!isActiveAndEnabled || message == null || !message.Ready || ViewerOpen ||
                !imageRows.Exists(row => row.Message == message))
                return false;
            if (viewer == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas == null)
                    return false;
                viewer = ChatImageViewer.Create(canvas.rootCanvas.transform, ResolveFont());
            }
            viewer.Open(message.Thumbnail.sprite, message.Button, message.Description,
                options.ViewerSafeArea, options.ViewerPreviewInset, options.ViewerZoomGestureThreshold,
                source => CanDismissImage?.Invoke(source) ?? true, NotifyViewerClosed);
            ImageViewerOpened?.Invoke();
            return true;
        }

        public void CloseImage() => viewer?.Close(ViewerCloseReason.Programmatic);

        private void NotifyViewerClosed(ViewerCloseReason reason) => ImageViewerClosed?.Invoke(reason);

        private void OnDisable() => viewer?.DisposeView();

        private void OnDestroy()
        {
            foreach (var reply in replies)
                reply.Cancel();
            if (viewer == null)
                return;
            if (Application.isPlaying)
                Destroy(viewer.gameObject);
            else
                DestroyImmediate(viewer.gameObject);
        }

        private void Update() => Advance(Time.unscaledDeltaTime);

        /// <summary>Advances every pending reply. The component calls this once per frame in Play mode.</summary>
        public void Advance(float deltaTime)
        {
            if (scroll == null || replies.Count == 0)
                return;
            bool layoutChanged = false;
            completedReplies.Clear();
            foreach (var reply in replies)
            {
                layoutChanged |= reply.Advance(deltaTime, out bool completed);
                if (completed)
                    completedReplies.Add(reply);
            }
            if (layoutChanged)
                RefreshLayout();
            foreach (var reply in completedReplies.ToArray())
            {
                if (replies.Remove(reply))
                    reply.NotifyCompleted();
            }
        }

        private void MarkReplyChanged(Reply reply)
        {
            if (!replies.Contains(reply))
                return;
            if (reply.Advance(0f, out bool completed))
                RefreshLayout();
            if (completed && replies.Remove(reply))
                reply.NotifyCompleted();
        }

        private void LateUpdate()
        {
            if (viewport == null)
                return;
            if (!Mathf.Approximately(lastWidth, viewport.rect.width))
                RefreshLayout();
            bool scrolled = !Mathf.Approximately(lastScrollOffset, scroll.Offset);
            lastScrollOffset = scroll.Offset;
            scrollbarIdle = scrolled || scroll.UserScrolling ? .65f : Mathf.Max(0f, scrollbarIdle - Time.unscaledDeltaTime);
            scrollbarVisibility.alpha = Mathf.MoveTowards(scrollbarVisibility.alpha,
                scrollbarIdle > 0f ? 1f : 0f, Time.unscaledDeltaTime / .18f);
            scrollbarVisibility.blocksRaycasts = scrollbarVisibility.alpha > .1f;
        }

        private void EnsureView()
        {
            if (scroll != null)
                return;
            var scrollRoot = AddRect("Message Scroll View", transform);
            Stretch(scrollRoot);
            scroll = scrollRoot.gameObject.AddComponent<FollowingScrollView>();
            scroll.scrollSensitivity = 6f;
            viewport = AddRect("Viewport", scrollRoot);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            content = AddRect("Messages", viewport);
            content.anchorMin = Vector2.up;
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1f);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;

            var stack = content.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = RowGap;
            stack.childControlHeight = stack.childControlWidth = true;
            stack.childForceExpandHeight = false;
            stack.childForceExpandWidth = true;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.ResumeTolerance = Mathf.Max(0f, options.FollowResumeTolerance);
            scroll.verticalScrollbar = CreateScrollbar(scrollRoot);
        }

        private Scrollbar CreateScrollbar(RectTransform parent)
        {
            var track = AddRect("Scrollbar", parent);
            track.anchorMin = Vector2.right;
            track.anchorMax = Vector2.one;
            track.pivot = Vector2.right;
            track.sizeDelta = new Vector2(12f, 0f);
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = Color.clear;
            scrollbarVisibility = track.gameObject.AddComponent<CanvasGroup>();
            scrollbarVisibility.alpha = 0f;
            scrollbarVisibility.blocksRaycasts = false;

            var slidingArea = AddRect("Sliding Area", track);
            Stretch(slidingArea);
            var handle = AddRect("Handle", slidingArea);
            handle.sizeDelta = Vector2.zero;
            var graphic = AddRect("Thumb", handle);
            graphic.anchorMin = new Vector2(.33f, 0f);
            graphic.anchorMax = new Vector2(.67f, 1f);
            graphic.offsetMin = graphic.offsetMax = Vector2.zero;
            var image = graphic.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, .65f);
            image.raycastTarget = false;

            var bar = track.gameObject.AddComponent<FollowingScrollViewScrollbar>();
            bar.handleRect = handle;
            bar.targetGraphic = image;
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.ScrollView = scroll;
            return bar;
        }

        private (RectTransform Rect, bool Self, bool HasTime) CreateRow(bool self, DateTime? timestamp, string id)
        {
            bool hasTime = options.ShowTimestamps && timestamp.HasValue;
            if (hasTime && lastDate != timestamp.Value.Date)
            {
                lastDate = timestamp.Value.Date;
                string date = lastDate.Value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
                var divider = AddLabel("Date " + date, content, 11, new Color(1f, 1f, 1f, .65f), TextAnchor.MiddleCenter);
                divider.text = date;
                divider.gameObject.AddComponent<LayoutElement>().preferredHeight = 27f;
            }
            var row = AddRect(id ?? $"Message {content.childCount}", content);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = self && options.SelfAlignRight ? TextAnchor.LowerRight : TextAnchor.LowerLeft;
            layout.spacing = TimestampGap;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = layout.childForceExpandWidth = false;
            return (row, self, hasTime);
        }

        private Text CreateTextRow(bool self, DateTime? timestamp, string id)
        {
            var row = CreateRow(self, timestamp, id);
            var style = self ? options.Self : options.Other;
            var bubble = AddRect("Bubble", row.Rect);
            var image = bubble.gameObject.AddComponent<Image>();
            image.sprite = style.BackgroundImage;
            image.type = image.sprite != null && image.sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.color = style.Background;
            image.raycastTarget = false;
            int padding = style.Background.a > .001f || style.BackgroundImage != null ? (int)BubblePadding : 0;
            var layout = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var label = AddLabel("Text", bubble, 16, style.Text, TextAnchor.UpperLeft);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            var size = bubble.gameObject.AddComponent<LayoutElement>();
            size.flexibleWidth = 0f;
            textRows.Add((size, row.HasTime));
            AddTimestamp(row, timestamp);
            return label;
        }

        private void AddTimestamp((RectTransform Rect, bool Self, bool HasTime) row, DateTime? timestamp)
        {
            if (!row.HasTime)
                return;
            bool right = row.Self && options.SelfAlignRight;
            var label = AddLabel("Timestamp", row.Rect, 10, new Color(1f, 1f, 1f, .65f),
                right ? TextAnchor.LowerRight : TextAnchor.LowerLeft);
            label.text = timestamp.Value.ToString("HH:mm", CultureInfo.InvariantCulture);
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = TimestampWidth;
            if (right)
                label.transform.SetAsFirstSibling();
        }

        /// <summary>Applies the current viewport size before rendering. Play mode also detects resizes automatically.</summary>
        public void RefreshLayout()
        {
            if (replacingHistory)
                return;
            EnsureView();
            lastWidth = viewport.rect.width;
            float rowWidth = Mathf.Max(0f, lastWidth) * Mathf.Clamp(options.RowMaxWidthPercent, 1f, 100f) / 100f;
            foreach (var row in textRows)
                row.Bubble.minWidth = row.Bubble.preferredWidth = BubbleWidth(rowWidth, row.HasTime);
            foreach (var row in imageRows)
            {
                var message = row.Message;
                float fallback = Mathf.Max(1f, options.ImagePlaceholderSize);
                Vector2 size = message.SourceSize.x > 0f && message.SourceSize.y > 0f
                    ? message.SourceSize : new Vector2(fallback, fallback);
                float scale = Mathf.Min(1f, Mathf.Min(BubbleWidth(rowWidth, row.HasTime) / size.x,
                    Mathf.Max(1f, options.ImageMaxHeight) / size.y));
                message.Size.preferredWidth = size.x * scale;
                message.Size.preferredHeight = size.y * scale;
            }
            scroll.RefreshContent();
        }

        private static float BubbleWidth(float width, bool hasTime) =>
            Mathf.Max(0f, width - (hasTime ? TimestampWidth + TimestampGap : 0f));

        private Font ResolveFont() => options.Font != null
            ? options.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private Text AddLabel(string name, Transform parent, int size, Color color, TextAnchor alignment)
        {
            var label = AddRect(name, parent).gameObject.AddComponent<Text>();
            label.font = ResolveFont();
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.supportRichText = false;
            label.raycastTarget = false;
            return label;
        }

        internal static RectTransform AddRect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
