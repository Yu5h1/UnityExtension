using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit.Tests
{
    /// <summary>
    /// Scroll-follow and distance fade against a live panel. PlayMode on purpose, same reason as
    /// <c>WorldPanelTests</c>: a runtime <see cref="UIDocument"/> only resolves layout while the Game view is
    /// actually rendering, so in an unfocused Editor every <c>worldBound</c> stays invalid.
    /// <para>
    /// The wheel/drag interaction that disengages following is not covered here, for the same reason
    /// <c>GestureLongPressTests</c> does not simulate a hold's timing: it is a live-panel pointer interaction,
    /// verified through the demo rather than an event dispatched by a test.
    /// </para>
    /// </summary>
    public class ChatPresenterScrollTests
    {
        private const float PanelWidth = 400, PanelHeight = 300;

        private GameObject host;
        private PanelSettings settings;
        private ChatModuleConfig config;
        private ChatPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            host = new GameObject("ChatPresenterScrollTests");
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = settings;

            var root = document.rootVisualElement;
            root.style.width = PanelWidth;
            root.style.height = PanelHeight;

            config = new ChatModuleConfig { ShowAvatars = false };
            var alice = new ChatParticipant("alice", "Alice");
            presenter = new ChatPresenter(config, "self", id => id == "alice" ? alice : null);
            presenter.Root.style.height = PanelHeight;
            root.Add(presenter.Root);
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
        }

        private static IEnumerator Settle()
        {
            // The nested row/bubble/wrapped-text layout here takes more passes to converge than a flat
            // fixture like WorldPanelTests' - especially in an unfocused Editor, where each yielded frame
            // advances the panel less completely.
            for (int i = 0; i < 10; i++) yield return null;
        }

        private static List<ChatMessage> ManyMessages(int count)
        {
            var messages = new List<ChatMessage>();
            for (int i = 0; i < count; i++)
                messages.Add(new ChatMessage(i.ToString(), "alice", "message number " + i, DateTimeOffset.Now));
            return messages;
        }

        [UnityTest]
        public IEnumerator TickScrollsToTheBottomWhileFollowing()
        {
            presenter.SetMessages(ManyMessages(40)); // enough rows to overflow a 300px-tall panel
            yield return Settle();

            var scrollView = (ScrollView)presenter.Root;
            Assert.Greater(scrollView.verticalScroller.highValue, 0,
                "guard: the fixture must actually overflow the panel or scrolling is meaningless here");

            presenter.Tick();
            yield return Settle();

            Assert.Greater(scrollView.scrollOffset.y, 0, "should have scrolled down, not sat at the top");
        }

        [UnityTest]
        public IEnumerator FollowBottomJumpsToTheBottomImmediately()
        {
            presenter.SetMessages(ManyMessages(40));
            yield return Settle();

            presenter.FollowBottom();
            yield return null;

            var scrollView = (ScrollView)presenter.Root;
            Assert.Greater(scrollView.scrollOffset.y, 0);
        }

        private List<VisualElement> Rows() =>
            ((ScrollView)presenter.Root).contentContainer.Children().First().Children().ToList();

        private IEnumerator SettleAtBottom()
        {
            presenter.SetMessages(ManyMessages(40));
            yield return Settle();
            presenter.Tick();
            yield return Settle();
            presenter.Tick(); // opacity reflects the scroll position Tick left behind last frame
            yield return Settle();
        }

        [UnityTest]
        public IEnumerator NewestRowIsOpaqueAndRowsFadeTowardTheOldestEdge()
        {
            yield return SettleAtBottom();

            var rows = Rows();
            Assert.Greater(rows.Count, 2);
            // Following the bottom by default, so the newest row sits on the viewport's bottom edge.
            float newest = rows[rows.Count - 1].resolvedStyle.opacity;
            float older = rows[rows.Count - 4].resolvedStyle.opacity;
            float oldest = rows[0].resolvedStyle.opacity;
            Assert.AreEqual(1f, newest, .01f);
            Assert.Greater(newest, older);
            Assert.Greater(older, oldest);
        }

        [UnityTest]
        public IEnumerator TurningFadeOffLeavesEveryRowOpaque()
        {
            yield return SettleAtBottom();
            Assert.Less(Rows()[0].resolvedStyle.opacity, 1f, "guard: fading must be in effect before turning it off");

            config.FadeMessages = false;
            presenter.Tick();
            yield return Settle();

            foreach (var row in Rows()) Assert.AreEqual(1f, row.resolvedStyle.opacity, .001f);
        }
    }
}
