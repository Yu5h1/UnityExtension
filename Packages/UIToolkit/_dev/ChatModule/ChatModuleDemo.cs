using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Yu5h1Lib.UIToolkit;

namespace Yu5h1LibTest
{
    /// <summary>
    /// Hands-on rig for the chat module, built step by step alongside it - see
    /// <c>Documentation/聊天面板模組.md</c> in UnityExtension for the decided config surface and progress.
    /// <para>Drop it on an empty GameObject and press Play. No scene setup is needed.</para>
    /// </summary>
    [AddComponentMenu("Yu5h1LibTest/Chat Module Demo")]
    public sealed class ChatModuleDemo : MonoBehaviour
    {
        private const string SelfId = "me";
        private const float AppMaxWidth = 430; // matches the phone-ish column every mainstream chat app settles on

        // Painted explicitly - a transparent root shows whatever the host clears to, which differs between
        // the Game View, the Device Simulator and a real device.
        private static readonly Color PageBackground = new Color(.11f, .12f, .14f);

        [Header("Test data (edit to try your own)")]
        [Tooltip("Pool of simulated reply texts - mix of short/medium/long so you can check the bubble's " +
                 "shape, wrapping and corner radius at each length. A new one is picked at random per reply.")]
        public string[] simulatedReplies =
        {
            "好的!",
            "收到,我會盡快處理,謝謝你的訊息。",
            "這是一則比較長的測試訊息,用來確認泡泡在內容比較多的時候,換行、最大寬度跟圓角是不是還維持良好的視覺效果——畢竟真實的聊天室裡,短中長訊息本來就會混著出現。",
        };

        [Tooltip("Pool of \"thinking\" durations (seconds) before a reply starts streaming. 0 exercises the " +
                 "instant-display path (thinking skipped entirely); a longer value exercises the dot-cycling " +
                 "and, past 5s, the elapsed-seconds suffix. A value is picked at random per reply.")]
        public float[] thinkingTimePool = { 0f, 6f };

        [Tooltip("Seconds between each revealed character of a simulated reply.")]
        public float streamInterval = .06f;

        private UIDocument document;
        private PanelSettings settings;
        private ChatPresenter presenter;
        private ChatComposer composer;
        private readonly Dictionary<string, ChatParticipant> participants = new Dictionary<string, ChatParticipant>();
        private readonly List<ChatMessage> messages = new List<ChatMessage>();

        private string[] replyWords;
        private float thinkingSeconds;
        private float replyTimer;
        private int replyStage = -1; // -1: thinking, 0..replyWords.Length: streaming/complete

        private void OnEnable()
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "ChatModuleDemo Panel";
            DevicePanel.ApplyScale(settings);

            var theme = Resources.Load<ThemeStyleSheet>("ChatModuleTheme");
            if (theme != null) settings.themeStyleSheet = theme;
            else Debug.LogError("ChatModuleTheme.tss not found under a Resources folder.");

            var host = new GameObject("ChatModuleDemo UI");
            host.transform.SetParent(transform, false);
            document = host.AddComponent<UIDocument>();
            document.panelSettings = settings;

            var root = document.rootVisualElement;
            // UIDocument's own rootVisualElement ships position:absolute with left/right pinned to 0 (it
            // fills the panel by default) - width/max-width/align-self set directly on it are overridden by
            // that pin and never actually centre anything. A normal flex child doesn't have that problem,
            // so the phone-width column below lives on its own wrapper instead.
            root.style.flexGrow = 1;
            root.style.flexDirection = FlexDirection.Row;
            root.style.justifyContent = Justify.Center;
            root.style.backgroundColor = PageBackground;
            // On root, not the 430 column - the background bleeds under the notch, the content doesn't.
            DevicePanel.PadToSafeArea(root);

            // Keeps the whole demo phone-width and centred even in a wide window - the same thing HealthAI's
            // own `.app { max-width: 430px; align-self: center; }` does.
            var frame = new VisualElement { name = "chat-module-demo-frame" };
            frame.style.flexDirection = FlexDirection.Column;
            frame.style.width = new Length(100, LengthUnit.Percent);
            frame.style.maxWidth = AppMaxWidth;
            frame.style.paddingLeft = frame.style.paddingRight =
                frame.style.paddingTop = frame.style.paddingBottom = 16;
            root.Add(frame);

            BuildParticipants();

            var config = new ChatModuleConfig
            {
                SelfAlignRight = true,
                ShowAvatars = true,
                NameDisplay = ChatNameDisplay.HideSelf,
                ShowTimestamps = false,
                UnifiedStyle = new BubbleStyle { BackgroundColor = new Color(.23f, .35f, .45f), CornerRadius = 14 },
            };

            presenter = new ChatPresenter(config, SelfId, id =>
                participants.TryGetValue(id, out var participant) ? participant : null);
            presenter.ParticipantCount = 2;

            SeedMessages();
            presenter.SetMessages(messages);
            frame.Add(presenter.Root);

            composer = new ChatComposer();
            composer.Sent += OnSent;
            composer.MicClicked += () => composer.Recording = !composer.Recording; // no real speech backend here
            frame.Add(composer.Root);
        }

        private void OnDisable()
        {
            if (document != null) Destroy(document.gameObject);
            if (settings != null) Destroy(settings);
        }

        /// <summary>Short/medium/long, right from the start, so the bubble shape at every length is visible
        /// without having to type anything first.</summary>
        private void SeedMessages()
        {
            var now = DateTimeOffset.Now;
            messages.Add(new ChatMessage("seed-short", "assistant", "嗨!", now));
            messages.Add(new ChatMessage("seed-medium", "assistant", "這是中等長度的訊息,大概一兩行。", now));
            messages.Add(new ChatMessage("seed-long", "assistant",
                "這是一則長訊息,打字、按 Enter 或送出鈕都能試試看——輸入框現在是膠囊形狀,麥克風跟送出鈕都在裡面靠右。", now));
        }

        private void OnSent(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            messages.Add(new ChatMessage(Guid.NewGuid().ToString(), SelfId, text, DateTimeOffset.Now));
            presenter.SetMessages(messages);
            composer.ClearDraft();
            presenter.FollowBottom();
            BeginSimulatedReply();
        }

        private void BeginSimulatedReply()
        {
            string reply = simulatedReplies != null && simulatedReplies.Length > 0
                ? simulatedReplies[UnityEngine.Random.Range(0, simulatedReplies.Length)]
                : "...";
            var chars = new List<string>(reply.Length);
            foreach (var rune in reply) chars.Add(rune.ToString());
            replyWords = chars.ToArray();

            thinkingSeconds = thinkingTimePool != null && thinkingTimePool.Length > 0
                ? thinkingTimePool[UnityEngine.Random.Range(0, thinkingTimePool.Length)]
                : 0f;

            presenter.BeginReply("assistant");
            replyTimer = 0;
            replyStage = -1;
        }

        private void Update()
        {
            if (presenter == null) return;
            float dt = Time.deltaTime;
            presenter.TickReply(dt, reduceMotion: false);
            presenter.Tick(); // bottom-anchor, scroll-follow and distance fade

            if (!presenter.IsReplying || replyWords == null) return;
            replyTimer += dt;

            if (replyStage == -1)
            {
                if (replyTimer < thinkingSeconds) return;
                replyTimer = 0;
                replyStage = 0;
            }

            if (replyTimer < streamInterval || replyStage >= replyWords.Length) return;
            replyTimer = 0;
            replyStage++;
            string snapshot = string.Join("", replyWords, 0, replyStage);
            if (replyStage >= replyWords.Length)
            {
                presenter.CompleteReply(snapshot);
                messages.Add(new ChatMessage(Guid.NewGuid().ToString(), "assistant", snapshot, DateTimeOffset.Now));
                presenter.EndReply();
                presenter.SetMessages(messages);
                replyWords = null;
            }
            else presenter.PushReply(snapshot);
        }

        private void BuildParticipants()
        {
            participants[SelfId] = new ChatParticipant(SelfId, "我") { Avatar = ProceduralAvatarGenerator.CreateUserAvatar() };
            participants["assistant"] = new ChatParticipant("assistant", "AI") { Avatar = ProceduralAvatarGenerator.CreateAiAvatar() };
        }
    }
}
