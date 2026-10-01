using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Lays out a list of <see cref="ChatMessage"/> as rows: self's messages on whichever side
    /// <see cref="ChatModuleConfig.SelfAlignRight"/> picks, everyone else always on the left, styled from
    /// data rather than computed - never a per-frame contrast algorithm.
    /// <para>
    /// Geometry and data only where it can be - the actual <see cref="VisualElement"/> tree is unavoidable
    /// here (a chat has to build real bubbles), but colour, radius and background all come from
    /// <see cref="BubbleStyle"/>, never from this class deciding what looks good.
    /// </para>
    /// </summary>
    public sealed class ChatPresenter
    {
        private const float MaxWidthPercent = 85;
        private const float AvatarSize = 32;
        private const float AvatarGap = 8;
        private static readonly Color FallbackAvatarColor = new Color(.4f, .45f, .5f);

        private const string ActiveReplyId = "chat-active-reply";

        // Opacity of a row at the viewport's oldest edge, and the pointer travel that counts as "the user is
        // dragging the list", not a tap on a message.
        private const float MinRowOpacity = .35f;
        private const float ScrollDragThreshold = 4f;

        private readonly ChatModuleConfig config;
        private readonly string selfParticipantId;
        private readonly Func<string, ChatParticipant> resolveParticipant;
        private readonly Dictionary<string, VisualElement> rows = new Dictionary<string, VisualElement>();
        private readonly ScrollView scrollView = new ScrollView { name = "chat-messages" };
        // Holds the rows; its own marginTop is set each Tick to push it down when it is shorter than the
        // viewport, so a short conversation hugs the bottom edge. Tried justify-content: flex-end first
        // (on this element and directly on the content container) - both left Yoga reporting this
        // container's own height as capped at its min-height rather than the overflowing sum of its
        // children, so every row beyond that cap landed at a negative, out-of-view position and scrolling's
        // own math (driven by that same wrong height) broke with it. A computed margin does not touch
        // whatever that measurement path is and does not have the problem.
        private readonly VisualElement stack = new VisualElement { name = "chat-messages-stack" };

        private ChatReplyPerformance activeReply;
        private VisualElement activeReplyRow;
        private Label activeReplyText;

        private bool followBottom = true;
        private bool scrollPressed;
        private Vector2 scrollPressPosition;

        /// <summary>The element to add into the caller's own hierarchy. A <see cref="ScrollView"/>; content
        /// goes through the usual <c>Add</c>/<c>Clear</c>, which UI Toolkit already routes to its content
        /// container.</summary>
        public VisualElement Root => scrollView;

        /// <summary>
        /// How many participants the room actually has - a fact about the room, not derivable from
        /// whichever messages happen to be loaded (a 3-person room where only 2 have spoken yet is still a
        /// 3-person room). The caller sets it; <see cref="ChatNameDisplay.Adaptive"/> reads it to decide
        /// whether names are worth showing at all. Defaults to 2 (adaptive hides names) until told otherwise.
        /// </summary>
        public int ParticipantCount { get; set; } = 2;

        /// <param name="selfParticipantId">Which participant is "self" for alignment - a viewpoint, not a
        /// fact stored on <see cref="ChatParticipant"/> itself.</param>
        /// <param name="resolveParticipant">Looks up a participant by <see cref="ChatMessage.ParticipantId"/>.
        /// May return null; an unknown participant renders with the unified/default style and no name.</param>
        public ChatPresenter(ChatModuleConfig config, string selfParticipantId,
            Func<string, ChatParticipant> resolveParticipant)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.selfParticipantId = selfParticipantId;
            this.resolveParticipant = resolveParticipant ?? throw new ArgumentNullException(nameof(resolveParticipant));

            // Fills whatever the caller gives it by default - without this a ScrollView sizes to its
            // content and never actually clips or scrolls, which defeats the point silently.
            scrollView.style.flexGrow = 1;
            scrollView.contentContainer.style.flexDirection = FlexDirection.Column;
            StyleOverlayScrollbar();

            stack.style.flexDirection = FlexDirection.Column;
            scrollView.Add(stack);

            // Any of these count as "the reader took the wheel" - stop chasing the bottom until the next
            // reply re-engages it. A plain tap on a message must not count, hence the drag threshold below
            // rather than reacting to PointerDown alone.
            scrollView.RegisterCallback<WheelEvent>(_ => followBottom = false);
            scrollView.contentContainer.RegisterCallback<PointerDownEvent>(e =>
            {
                scrollPressed = true;
                scrollPressPosition = e.position;
            });
            scrollView.contentContainer.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (scrollPressed && Vector2.Distance(e.position, scrollPressPosition) >= ScrollDragThreshold)
                    followBottom = false;
            });
            scrollView.contentContainer.RegisterCallback<PointerUpEvent>(_ => scrollPressed = false);
        }

        /// <summary>
        /// A thin overlay scrollbar that only appears on hover - invisible, out of the content's layout flow,
        /// otherwise. The world's chat apps (and most scrollable mobile/desktop UI generally) treat a
        /// permanently-visible scroll track as visual noise; this makes the default match that instead of
        /// Unity's own boxy one.
        /// </summary>
        private void StyleOverlayScrollbar()
        {
            scrollView.contentViewport.style.marginRight = 0;

            var scroller = scrollView.verticalScroller;
            scroller.style.position = Position.Absolute;
            scroller.style.right = 0;
            scroller.style.top = 2;
            scroller.style.bottom = 2;
            scroller.style.width = scroller.style.minWidth = 12;
            scroller.style.marginLeft = scroller.style.marginRight = 0;
            scroller.style.paddingLeft = 4;
            scroller.style.paddingRight = 0;
            scroller.style.borderLeftWidth = scroller.style.borderRightWidth =
                scroller.style.borderTopWidth = scroller.style.borderBottomWidth = 0;
            scroller.style.backgroundColor = Color.clear;
            // The theme sizes the slider wider than this 12px track; clip it so nothing it draws (the
            // tracker's border, in particular) lands outside the scrollbar.
            scroller.style.overflow = Overflow.Hidden;
            scroller.style.opacity = 0;
            scroller.style.transitionProperty = new List<StylePropertyName> { "opacity" };
            scroller.style.transitionDuration = new List<TimeValue> { new TimeValue(180, TimeUnit.Millisecond) };

            scroller.lowButton.style.display = DisplayStyle.None;
            scroller.highButton.style.display = DisplayStyle.None;

            scroller.slider.style.flexGrow = 1;
            scroller.slider.style.minWidth = 0;
            scroller.slider.style.width = new Length(100, LengthUnit.Percent);
            scroller.slider.style.marginLeft = scroller.slider.style.marginRight = 0;
            scroller.slider.style.backgroundColor = Color.clear;

            var tracker = scroller.slider.Q(className: Slider.trackerUssClassName);
            var draggerBorder = scroller.slider.Q(className: Slider.draggerBorderUssClassName);
            var dragger = scroller.slider.Q(className: Slider.draggerUssClassName);
            if (tracker != null)
            {
                tracker.style.backgroundColor = Color.clear;
                tracker.style.borderLeftWidth = tracker.style.borderRightWidth =
                    tracker.style.borderTopWidth = tracker.style.borderBottomWidth = 0;
            }
            if (draggerBorder != null)
            {
                draggerBorder.style.backgroundColor = Color.clear;
                draggerBorder.style.borderLeftWidth = draggerBorder.style.borderRightWidth =
                    draggerBorder.style.borderTopWidth = draggerBorder.style.borderBottomWidth = 0;
            }
            if (dragger != null)
            {
                dragger.style.width = dragger.style.minWidth = 4;
                dragger.style.minHeight = 24;
                dragger.style.marginLeft = dragger.style.marginRight = 0;
                dragger.style.borderLeftWidth = dragger.style.borderRightWidth =
                    dragger.style.borderTopWidth = dragger.style.borderBottomWidth = 0;
                dragger.style.borderTopLeftRadius = dragger.style.borderTopRightRadius =
                    dragger.style.borderBottomLeftRadius = dragger.style.borderBottomRightRadius = 2;
                dragger.style.backgroundColor = new Color(.4f, .44f, .43f, .45f);
            }

            // Mouse Enter/Leave (not Pointer Enter/Leave) are what bubble across an entire subtree in UI
            // Toolkit, matching CSS :hover on a container - Pointer versions only fire on the exact element
            // the pointer crossed into.
            scrollView.RegisterCallback<MouseEnterEvent>(_ => scroller.style.opacity = 1);
            scrollView.RegisterCallback<MouseLeaveEvent>(_ => scroller.style.opacity = 0);
        }

        /// <summary>Per-frame upkeep unrelated to any single reply: keeps a short conversation hugging the
        /// bottom edge, keeps the view pinned to the bottom while nothing has pulled it away (see
        /// <see cref="FollowBottom"/>), and applies <see cref="ChatModuleConfig.FadeMessages"/>.
        /// Call every frame regardless of whether a reply is active.</summary>
        public void Tick()
        {
            UpdateBottomAnchor();
            if (followBottom) ScrollToBottom();
            UpdateFade();
        }

        /// <summary>
        /// Pushes <see cref="stack"/> down by whatever gap is left under it when it is shorter than the
        /// viewport, so a short conversation sits at the bottom instead of floating at the top with empty
        /// space under it - the same thing every mainstream chat app does. A computed margin, not
        /// <c>justify-content</c>; see the field comment on <see cref="stack"/> for why.
        /// </summary>
        private void UpdateBottomAnchor()
        {
            float viewportHeight = scrollView.contentViewport.resolvedStyle.height;
            float contentHeight = stack.resolvedStyle.height;
            if (float.IsNaN(viewportHeight) || float.IsNaN(contentHeight)) return;
            stack.style.marginTop = Mathf.Max(0, viewportHeight - contentHeight);
        }

        /// <summary>Jumps to the bottom immediately and resumes auto-following - for a caller's own action
        /// (sending a message) that should always land there, independent of whatever the reader was doing.</summary>
        public void FollowBottom()
        {
            followBottom = true;
            ScrollToBottom();
        }

        private void ScrollToBottom() =>
            scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, float.MaxValue);

        private void UpdateFade()
        {
            Rect viewport = scrollView.contentViewport.worldBound;
            if (!viewport.IsValid()) return;
            bool fade = config.FadeMessages; // read every frame so the caller can flip it live
            foreach (var row in rows.Values) FadeRow(row, viewport, fade);
            if (activeReplyRow != null) FadeRow(activeReplyRow, viewport, fade);
        }

        /// <summary>One-directional, following the stacking direction: messages pile up from the bottom, so
        /// the bottom edge (newest) is fully opaque and opacity falls off toward the top (oldest). Measured
        /// from the row's bottom margin edge, so the newest row sitting on the viewport's bottom is exactly 1.</summary>
        private static void FadeRow(VisualElement row, Rect viewport, bool fade)
        {
            if (!fade)
            {
                row.style.opacity = StyleKeyword.Null;
                return;
            }
            Rect bounds = row.worldBound;
            if (!bounds.IsValid()) return;
            float bottom = bounds.yMax + row.resolvedStyle.marginBottom;
            float towardNewest = Mathf.Clamp01((bottom - viewport.y) / viewport.height);
            row.style.opacity = Mathf.Lerp(MinRowOpacity, 1f, towardNewest);
        }

        /// <summary>Rebuilds every row from scratch. Cheap enough for now; only worth diffing once a real
        /// project's message counts say otherwise.</summary>
        public void SetMessages(IReadOnlyList<ChatMessage> messages)
        {
            stack.Clear();
            rows.Clear();
            if (messages == null) return;

            DateTime? previousDay = null;
            foreach (var message in messages)
            {
                if (config.ShowTimestamps)
                {
                    var day = message.CreatedAt.ToLocalTime().Date;
                    if (previousDay != day)
                    {
                        previousDay = day;
                        stack.Add(BuildDateDivider(message.CreatedAt));
                    }
                }

                var row = BuildRow(message);
                rows[message.Id] = row;
                stack.Add(row);
            }
        }

        /// <summary>Adds a new, empty reply row from <paramref name="participantId"/> and starts its reveal
        /// clock at "thinking". One active reply at a time - a second call replaces the first without
        /// finishing it.</summary>
        public void BeginReply(string participantId)
        {
            activeReplyRow?.RemoveFromHierarchy();
            activeReply = new ChatReplyPerformance();
            activeReply.Begin();
            activeReplyRow = BuildRowShell(
                new ChatMessage(ActiveReplyId, participantId, "", DateTimeOffset.Now), out activeReplyText);
            stack.Add(activeReplyRow);
            // A fresh reply is worth seeing even if the reader had scrolled away from an earlier one.
            FollowBottom();
        }

        /// <summary>Feeds a new cumulative snapshot - the reply's full text so far, not a delta - into the
        /// active reply. No-op with no active reply.</summary>
        public void PushReply(string snapshot) => activeReply?.Push(snapshot);

        /// <summary>Marks the active reply's final text. Still reveals at the normal pace from wherever it
        /// had reached - see <see cref="ChatReplyPerformance.Complete"/>.</summary>
        public void CompleteReply(string final) => activeReply?.Complete(final);

        /// <summary>Advances the active reply's reveal by <paramref name="dt"/> and applies the result to its
        /// row. Call every frame while <see cref="IsReplying"/> is true; a no-op otherwise.</summary>
        public void TickReply(float dt, bool reduceMotion, float pace = 1f)
        {
            if (activeReply == null) return;
            activeReplyText.text = activeReply.Tick(dt, reduceMotion, pace);
        }

        /// <summary>True from <see cref="BeginReply"/> until <see cref="EndReply"/>.</summary>
        public bool IsReplying => activeReply != null;

        /// <summary>True once the active reply has finished revealing and been completed. Always true with
        /// no active reply, so a caller can gate on it without checking <see cref="IsReplying"/> first.</summary>
        public bool ReplyDone => activeReply?.Done ?? true;

        /// <summary>
        /// Drops the live reply row. Call after appending the finished reply to the caller's own message
        /// list and re-calling <see cref="SetMessages"/>, so the same content is not shown twice - this
        /// class never adds the reply to <paramref name="messages"/> itself.
        /// </summary>
        public void EndReply()
        {
            activeReplyRow?.RemoveFromHierarchy();
            activeReply = null;
            activeReplyRow = null;
            activeReplyText = null;
        }

        private VisualElement BuildRow(ChatMessage message) => BuildRowShell(message, out _);

        private VisualElement BuildRowShell(ChatMessage message, out Label text)
        {
            var participant = resolveParticipant(message.ParticipantId);
            bool isSelf = message.ParticipantId == selfParticipantId;
            bool selfRight = isSelf && config.SelfAlignRight;

            var row = new VisualElement { name = message.Id };
            row.AddToClassList("chat-row");
            // Reversed for a right-aligned self row so the avatar stays on the outer edge (right), the
            // same place it would sit for anyone reading their own message back.
            row.style.flexDirection = selfRight ? FlexDirection.RowReverse : FlexDirection.Row;
            // Top-aligned: the avatar sits at the bubble's top corner regardless of how many lines the
            // message wraps to, the same place every mainstream chat app anchors it.
            row.style.alignItems = Align.FlexStart;
            row.style.alignSelf = selfRight ? Align.FlexEnd : Align.FlexStart;
            // On the bubble instead, this and the row's own shrink-to-fit (from alignSelf above) would
            // size each other in a circle - the row can only be a percentage of a Root that has a definite
            // width, so the constraint has to live here.
            row.style.maxWidth = new Length(MaxWidthPercent, LengthUnit.Percent);
            row.style.flexShrink = 0;
            row.style.marginBottom = 8;

            if (config.ShowAvatars)
            {
                var avatar = BuildAvatar(participant);
                if (selfRight) avatar.style.marginLeft = AvatarGap; else avatar.style.marginRight = AvatarGap;
                row.Add(avatar);
            }

            var bubble = new VisualElement { name = message.Id + "-bubble" };
            bubble.AddToClassList("chat-message");
            bubble.AddToClassList(isSelf ? "chat-message-self" : "chat-message-other");
            ApplyStyle(bubble, ResolveStyle(participant));

            if (participant != null && ShouldShowName(isSelf))
            {
                var name = new Label(participant.Name);
                name.AddToClassList("chat-message-name");
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                name.style.fontSize = 11;
                // The runtime theme's own Label defaults add margin and padding on every side - left as-is
                // here it was stacking up to 31px before the first line of text even started. Zeroed out and
                // replaced with one deliberate gap below the name instead.
                ZeroSpacing(name);
                name.style.marginBottom = 2;
                bubble.Add(name);
            }

            text = new Label(message.Text) { enableRichText = false };
            text.AddToClassList("chat-message-text");
            text.style.whiteSpace = WhiteSpace.Normal;
            ZeroSpacing(text);
            bubble.Add(text);

            if (config.ShowTimestamps)
            {
                var time = new Label(message.CreatedAt.ToLocalTime().ToString("HH:mm"));
                time.AddToClassList("chat-message-time");
                time.style.alignSelf = Align.FlexEnd;
                time.style.fontSize = 10;
                time.style.opacity = .65f;
                ZeroSpacing(time);
                time.style.marginTop = 2;
                bubble.Add(time);
            }

            row.Add(bubble);
            return row;
        }

        /// <summary>Whether <paramref name="isSelf"/>'s message gets a name label. See
        /// <see cref="ChatNameDisplay"/> for what each mode means; only <see cref="ParticipantCount"/> and
        /// this flag decide it - never anything read off the message itself.</summary>
        private bool ShouldShowName(bool isSelf)
        {
            switch (config.NameDisplay)
            {
                case ChatNameDisplay.AlwaysShow: return true;
                case ChatNameDisplay.HideSelf: return !isSelf;
                default: return !isSelf && ParticipantCount > 2; // Adaptive
            }
        }

        /// <summary>Which <see cref="BubbleStyle"/> applies, per <see cref="ChatModuleConfig.StyleMode"/>.
        /// Only <see cref="ChatStyleMode.Unified"/> is implemented; the others fall back to it until they
        /// are built, rather than silently reading a participant field nothing has populated yet.</summary>
        private BubbleStyle ResolveStyle(ChatParticipant participant)
        {
            switch (config.StyleMode)
            {
                case ChatStyleMode.PerParticipant when participant?.Style != null:
                    return participant.Style;
                default:
                    return config.UnifiedStyle;
            }
        }

        /// <summary>Clears margin and padding the runtime theme's base Label style otherwise contributes -
        /// spacing inside a bubble is this class's call via <see cref="BubbleStyle"/> and a handful of
        /// explicit constants, not whatever a generic theme rule happens to add.</summary>
        private static void ZeroSpacing(VisualElement element)
        {
            element.style.marginLeft = element.style.marginRight =
                element.style.marginTop = element.style.marginBottom = 0;
            element.style.paddingLeft = element.style.paddingRight =
                element.style.paddingTop = element.style.paddingBottom = 0;
        }

        private static void ApplyStyle(VisualElement element, BubbleStyle style)
        {
            if (style == null) return;
            element.style.backgroundColor = style.BackgroundColor;
            // Set on the bubble, not each label - color is inherited, so the name, text and timestamp
            // labels all pick it up without each needing to know about BubbleStyle themselves.
            element.style.color = style.TextColor;
            element.style.borderTopLeftRadius = style.CornerRadius;
            element.style.borderTopRightRadius = style.CornerRadius;
            element.style.borderBottomLeftRadius = style.CornerRadius;
            element.style.borderBottomRightRadius = style.CornerRadius;
            if (style.BackgroundImage != null)
                element.style.backgroundImage = new StyleBackground(style.BackgroundImage);
            if (style.BackgroundColor.a > 0 || style.BackgroundImage != null)
                element.style.paddingLeft = element.style.paddingRight =
                    element.style.paddingTop = element.style.paddingBottom = 8;
        }

        /// <summary>One centred label marking a day boundary. Inserted once per day, not once per message -
        /// see <see cref="SetMessages"/>.</summary>
        private static VisualElement BuildDateDivider(DateTimeOffset timestamp)
        {
            var label = new Label(timestamp.ToLocalTime().ToString("yyyy/MM/dd"));
            label.AddToClassList("chat-date-divider");
            label.style.alignSelf = Align.Center;
            label.style.fontSize = 11;
            label.style.opacity = .65f;
            label.style.marginTop = label.style.marginBottom = 8;
            return label;
        }

        /// <summary>
        /// A circular avatar. Uses <see cref="ChatParticipant.Avatar"/> when one is set; otherwise falls
        /// back to a plain initial-on-a-circle - not the vector icon the design calls for eventually
        /// (Documentation/聊天面板模組.md step 8, deferred), but it needs no image asset at all, so the
        /// module works before that subsystem exists.
        /// </summary>
        private static VisualElement BuildAvatar(ChatParticipant participant)
        {
            var avatar = new VisualElement { name = "avatar" };
            avatar.AddToClassList("chat-avatar");
            avatar.style.width = avatar.style.height = AvatarSize;
            avatar.style.borderTopLeftRadius = avatar.style.borderTopRightRadius =
                avatar.style.borderBottomLeftRadius = avatar.style.borderBottomRightRadius = AvatarSize / 2;
            avatar.style.alignItems = Align.Center;
            avatar.style.justifyContent = Justify.Center;
            avatar.style.flexShrink = 0;

            if (participant?.Avatar != null)
            {
                avatar.style.backgroundImage = new StyleBackground(participant.Avatar);
                return avatar;
            }

            avatar.style.backgroundColor = FallbackAvatarColor;
            string initial = string.IsNullOrEmpty(participant?.Name)
                ? "?" : participant.Name.Substring(0, 1).ToUpperInvariant();
            var label = new Label(initial) { pickingMode = PickingMode.Ignore };
            label.style.color = Color.white;
            label.style.fontSize = AvatarSize * .45f;
            avatar.Add(label);
            return avatar;
        }
    }
}
