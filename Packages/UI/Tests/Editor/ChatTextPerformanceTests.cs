using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib.UI.Acceptance;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.UI.Tests
{
    /// <summary>Verifies chat effects through reply timing, rendered vertices, and the common Config.</summary>
    public sealed class ChatTextPerformanceTests
    {
        private GameObject root;
        private ChatPanel chat;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Chat performance", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(500, 600);
            var child = new GameObject("Chat", typeof(RectTransform));
            child.transform.SetParent(rect, false);
            var view = (RectTransform)child.transform;
            view.anchorMin = Vector2.zero;
            view.anchorMax = Vector2.one;
            view.offsetMin = view.offsetMax = Vector2.zero;
            chat = child.AddComponent<ChatPanel>();
            chat.Configure(new ChatPanel.Options());
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void StandaloneTextPerformanceRevealsAndAnimatesEachGrapheme()
        {
            var banner = new GameObject("Banner", typeof(RectTransform));
            banner.transform.SetParent(root.transform, false);
            var label = banner.AddComponent<TextPerformance>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.AutoAdvance = false;
            var options = ChatTextPerformance.Default;
            options.Fade = options.Move = options.FadeSequential = options.MoveSequential = true;
            options.Directed = true;
            options.SecondsPerGrapheme = .5f;
            options.DurationMin = options.DurationMax = 1f;
            options.DistanceMin = options.DistanceMax = 16f;
            label.Settings = options;
            int completed = 0;
            label.Completed += () => completed++;

            label.Play("GO");
            Assert.That(label.text, Is.Empty);
            label.Advance(.5f);
            Assert.That(label.text, Is.EqualTo("G"));
            label.Advance(.5f);
            Assert.That(label.text, Is.EqualTo("GO"));
            var active = Capture(label);
            var preferredSize = new Vector2(label.preferredWidth, label.preferredHeight);
            Assert.That(label.IsComplete, Is.False);

            label.Advance(1f);
            var settled = Capture(label);
            Assert.That(label.IsComplete, Is.True);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(active.Count, Is.EqualTo(settled.Count));
            Assert.That(active[0].color.a, Is.InRange(222, 225));
            Assert.That(active[6].color.a, Is.Zero);
            Assert.That(active[0].position.x - settled[0].position.x, Is.EqualTo(2f).Within(.01f));
            Assert.That(active[6].position.x - settled[6].position.x, Is.EqualTo(16f).Within(.01f));
            Assert.That(new Vector2(label.preferredWidth, label.preferredHeight), Is.EqualTo(preferredSize));

            label.Play("OK");
            label.Advance(.5f);
            label.Stop();
            label.Advance(1f);
            Assert.That(label.text, Is.EqualTo("O"));
            Assert.That(Capture(label)[0].color.a, Is.EqualTo(255));
            Assert.That(completed, Is.EqualTo(1));
        }

        [Test]
        public void ChatComponentCompletionCanStartAnotherReply()
        {
            var reply = chat.BeginReply();
            reply.Complete("A");
            var label = chat.MessageContent.GetComponentInChildren<TextPerformance>();
            ChatPanel.Reply next = null;
            label.Completed += () => next = chat.BeginReply();

            chat.Advance(1f);

            Assert.That(reply.IsComplete, Is.True);
            Assert.That(next, Is.Not.Null);
            Assert.That(next.IsComplete, Is.False);
        }

        [TestCase("Hello world")]
        [TestCase("a\u0301 中文\n🇹🇼 👩‍💻 B")]
        public void CombinedEffectsAnimateGlyphsWithoutChangingLayout(string message)
        {
            var options = ChatTextPerformance.Default;
            options.Instant = options.Fade = options.Move = options.Directed = true;
            options.FadeSequential = options.MoveSequential = true;
            options.DurationMin = options.DurationMax = 1;
            options.DistanceMin = options.DistanceMax = 16;
            chat.TextPerformance = options;

            var reply = chat.BeginReply();
            int completed = 0;
            reply.RevealCompleted += () => completed++;
            reply.Complete(message);
            Assert.That(reply.Text, Is.EqualTo(message));
            var label = chat.MessageContent.GetComponentInChildren<TextPerformance>();
            Assert.That(label, Is.Not.Null);
            var early = Capture(label);
            var preferredHeight = label.preferredHeight;
            var scrollHeight = chat.MessageContent.rect.height;

            chat.Advance(.5f);
            var middle = Capture(label);
            Assert.That(reply.IsComplete, Is.False);
            chat.Advance(.5f);
            var settled = Capture(label);
            Assert.That(reply.IsComplete, Is.True);
            chat.Advance(1);

            Assert.That(completed, Is.EqualTo(1));
            Assert.That(early.Count, Is.GreaterThan(0));
            Assert.That(middle.Count, Is.EqualTo(early.Count));
            Assert.That(settled.Count, Is.EqualTo(early.Count));
            Assert.That(early[0].color.a, Is.Zero,
                $"stream={early.Count}, text={label.text.Length}, chars={label.cachedTextGenerator.characterCount}, visible={label.cachedTextGenerator.characterCountVisible}, label={label.GetType()}");
            Assert.That(middle[0].color.a, Is.InRange(222, 225));
            Assert.That(settled[0].color.a, Is.EqualTo(255));
            Assert.That(early[0].position.x - settled[0].position.x, Is.EqualTo(16f).Within(.01f));
            Assert.That(middle[0].position.x - settled[0].position.x, Is.EqualTo(2f).Within(.01f));
            int lastGlyph = settled.FindLastIndex(vertex => vertex.color.a != 0);
            Assert.That(lastGlyph, Is.GreaterThanOrEqualTo(6));
            Assert.That(early[lastGlyph].color.a, Is.Zero);
            Assert.That(middle[lastGlyph].color.a, Is.InRange(222, 225));
            Assert.That(early[lastGlyph].position.x - settled[lastGlyph].position.x, Is.EqualTo(16f).Within(.01f));
            Assert.That(label.preferredHeight, Is.EqualTo(preferredHeight));
            Assert.That(chat.MessageContent.rect.height, Is.EqualTo(scrollHeight));
        }

        [Test]
        public void InstantRetainsSafePrefixAndReducedMotionSettlesEffects()
        {
            var options = ChatTextPerformance.Default;
            options.Instant = options.Fade = true;
            chat.TextPerformance = options;
            var reply = chat.BeginReply();
            reply.Push("AB");
            Assert.That(reply.Text, Is.EqualTo("A"));
            reply.Complete();
            Assert.That(reply.Text, Is.EqualTo("AB"));
            Assert.That(reply.IsComplete, Is.False);
            chat.ReduceMotion = true;
            chat.Advance(0f);
            Assert.That(reply.IsComplete, Is.True);
        }

        [Test]
        public void RewrittenGraphemeStartsItsOwnEffect()
        {
            var options = ChatTextPerformance.Default;
            options.Instant = options.Fade = true;
            options.DurationMin = options.DurationMax = 1;
            chat.TextPerformance = options;
            var reply = chat.BeginReply();
            reply.Push("a");
            chat.Advance(.75f);
            reply.Complete("a\u0301");
            chat.Advance(.25f);
            Assert.That(reply.IsComplete, Is.False);
            chat.Advance(.75f);
            Assert.That(reply.IsComplete, Is.True);
        }

        [TestCase(false, 223)]
        [TestCase(true, 0)]
        public void SequentialFadeStartsAtEachRevealedGrapheme(bool sequential, int secondOpacity)
        {
            var options = ChatTextPerformance.Default;
            options.Fade = true;
            options.FadeSequential = sequential;
            options.SecondsPerGrapheme = .5f;
            options.DurationMin = options.DurationMax = 1f;
            chat.TextPerformance = options;

            var reply = chat.BeginReply();
            reply.Complete("AB");
            chat.Advance(.5f);
            Assert.That(reply.Text, Is.EqualTo("A"));
            chat.Advance(.5f);
            var vertices = Capture(chat.MessageContent.GetComponentInChildren<Text>());
            Assert.That(reply.Text, Is.EqualTo("AB"));
            Assert.That(vertices[0].color.a, Is.EqualTo(223));
            Assert.That(vertices[6].color.a, Is.EqualTo(secondOpacity));
        }

        [Test]
        public void EditingAnInactiveTriggerOrRevealSpeedKeepsTheCurrentFade()
        {
            var value = ChatTextPerformance.Default;
            value.Instant = value.Fade = true;
            value.DurationMin = value.DurationMax = 1f;
            chat.TextPerformance = value;
            var reply = chat.BeginReply();
            reply.Complete("AB");
            chat.Advance(.5f);

            value.FadeSequential = true;
            value.SecondsPerGrapheme *= 2;
            chat.TextPerformance = value;
            chat.Advance(0f);
            var middle = Capture(chat.MessageContent.GetComponentInChildren<Text>());
            Assert.That(middle[0].color.a, Is.InRange(222, 225));
            chat.Advance(.5f);
            Assert.That(reply.IsComplete, Is.True);
        }

        [Test]
        public void ChangingDisplayAndMoveWhileReplyingKeepsAnUnchangedFade()
        {
            var value = ChatTextPerformance.Default;
            value.Fade = true;
            value.SecondsPerGrapheme = .5f;
            value.DurationMin = value.DurationMax = 1f;
            chat.TextPerformance = value;
            var reply = chat.BeginReply();
            reply.Complete("AB");
            chat.Advance(.5f);
            chat.Advance(.25f);
            Assert.That(reply.Text, Is.EqualTo("A"));

            value.Instant = value.Move = value.Directed = true;
            value.DistanceMin = value.DistanceMax = 16f;
            chat.TextPerformance = value;
            chat.Advance(0f);
            Assert.That(reply.Text, Is.EqualTo("AB"));
            var label = chat.MessageContent.GetComponentInChildren<Text>();
            var moving = Capture(label);
            Assert.That(moving[0].color.a, Is.InRange(146, 149));

            value.Move = false;
            chat.TextPerformance = value;
            chat.Advance(0f);
            var noMovement = Capture(label);
            Assert.That(moving[0].position.x - noMovement[0].position.x, Is.EqualTo(16f).Within(.01f));
            Assert.That(noMovement[0].color.a, Is.EqualTo(moving[0].color.a));
            Assert.That(reply.IsComplete, Is.False);
        }

        [Test]
        public void ZeroRatioCompletesImmediatelyAndInvalidRangeKeepsLastValue()
        {
            var options = ChatTextPerformance.Default;
            options.Instant = options.Fade = options.Move = true;
            options.Percentage = 0f;
            chat.TextPerformance = options;

            var invalid = options;
            invalid.DurationMin = invalid.DurationMax + 1f;
            Assert.Throws<System.ArgumentOutOfRangeException>(() => chat.TextPerformance = invalid);
            Assert.That(chat.TextPerformance.DurationMin, Is.EqualTo(options.DurationMin));

            var reply = chat.BeginReply();
            reply.Complete("Hello");
            Assert.That(reply.Text, Is.EqualTo("Hello"));
            Assert.That(reply.IsComplete, Is.True);
        }

        [Test]
        public void AcceptanceCaseMountsConfigWithoutCoveringTheToolbar()
        {
            Object.DestroyImmediate(chat.gameObject);
            ((RectTransform)root.transform).sizeDelta = new Vector2(320, 600);
            var demoCase = new UxUiChatPanelDemoCase();
            try
            {
                demoCase.Mount((RectTransform)root.transform);
                Canvas.ForceUpdateCanvases();
                var demo = root.GetComponentInChildren<UxUiChatPanelDemo>();
                var config = demo.GetComponentInChildren<SettingsPanel>(true);
                Assert.That(config.ActiveGroup, Is.EqualTo("chat-panel"));
                var toolbar = (RectTransform)demo.transform.Find("Demo controls");
                var opener = (RectTransform)config.Opener.transform;
                Assert.That(WorldY(opener, opener.rect.yMin), Is.GreaterThanOrEqualTo(WorldY(toolbar, toolbar.rect.yMax)));
                config.Opener.onClick.Invoke();
                Assert.That(config.IsOpen, Is.True);
                Assert.That(config.Close(), Is.True);
                Assert.That(demo.Panel, Is.Not.Null);
            }
            finally
            {
                demoCase.Unmount();
            }
        }

        [Test]
        public void SharedConfigPersistsAndAppliesPerformanceToNewReplies()
        {
            string key = "yu5h1lib.ui.test.chat-performance." + System.Guid.NewGuid().ToString("N");
            try
            {
                var config = ChatTextPerformanceSettings.Mount((RectTransform)root.transform, chat, key);
                Assert.That(config.ActiveGroup, Is.EqualTo("chat-panel"));
                Assert.That(config.Tabs.SelectedId, Is.EqualTo("text-performance"));
                Assert.That(config.Show(), Is.True);
                FindToggle(config, "逐字").isOn = false;
                Assert.That(chat.TextPerformance.Instant, Is.True);
                Object.DestroyImmediate(config.gameObject);
                chat.TextPerformance = ChatTextPerformance.Default;

                var restored = ChatTextPerformanceSettings.Mount((RectTransform)root.transform, chat, key);
                Assert.That(restored.Show(), Is.True);
                Assert.That(chat.TextPerformance.Instant, Is.True);
                var reply = chat.BeginReply();
                reply.Complete("Hello");
                Assert.That(reply.Text, Is.EqualTo("Hello"));
                Assert.That(reply.IsComplete, Is.True);
            }
            finally
            {
                Preferences.DefaultStorage.Delete(key);
            }
        }

        [Test]
        public void ConfigCollapsesInactiveFieldsAndRestoresTheirAcceptedValues()
        {
            string key = "yu5h1lib.ui.test.chat-performance." + System.Guid.NewGuid().ToString("N");
            try
            {
                var config = ChatTextPerformanceSettings.Mount((RectTransform)root.transform, chat, key);
                Assert.That(config.Show(), Is.True);
                var typewriter = FindToggle(config, "逐字");
                var fade = FindToggle(config, "淡入");
                var move = FindToggle(config, "位移歸位");
                var fadeSequence = FindToggle(config, "淡入依序觸發");
                var moveSequence = FindToggle(config, "位移依序觸發");
                var percentage = System.Array.Find(config.Tabs.GetComponentsInChildren<InputField>(true),
                    field => field.name == "表演比例（%）");
                Assert.That(percentage, Is.Not.Null);
                Assert.That(percentage.gameObject.activeInHierarchy, Is.False);
                Assert.That(fadeSequence.gameObject.activeInHierarchy, Is.False);
                Assert.That(moveSequence.gameObject.activeInHierarchy, Is.False);

                fade.isOn = true;
                fadeSequence.isOn = true;
                percentage.onEndEdit.Invoke("37");
                Assert.That(chat.TextPerformance.Percentage, Is.EqualTo(37f));
                Assert.That(percentage.gameObject.activeInHierarchy, Is.True);
                Assert.That(fadeSequence.interactable, Is.True);

                move.isOn = true;
                moveSequence.isOn = true;
                typewriter.isOn = false;
                Assert.That(chat.TextPerformance.FadeSequential, Is.True);
                Assert.That(chat.TextPerformance.MoveSequential, Is.True);
                Assert.That(fadeSequence.interactable, Is.False);
                Assert.That(moveSequence.interactable, Is.False);
                typewriter.isOn = true;
                Assert.That(fadeSequence.isOn && moveSequence.isOn, Is.True);

                fade.isOn = false;
                Assert.That(fadeSequence.gameObject.activeInHierarchy, Is.False);
                Assert.That(percentage.gameObject.activeInHierarchy, Is.True);
                Assert.That(percentage.GetComponentsInParent<RectTransform>(true).Any(parent => parent.name == "Move in"));
                Assert.That(percentage.text, Is.EqualTo("37"));
                percentage.onEndEdit.Invoke("101");
                Assert.That(chat.TextPerformance.Percentage, Is.EqualTo(37f));
                Assert.That(percentage.text, Is.EqualTo("37"));
                move.isOn = false;
                Assert.That(percentage.gameObject.activeInHierarchy, Is.False);
                Assert.That(config.Close(), Is.True);
                Assert.That(config.Show(), Is.True);
                move.isOn = true;
                Assert.That(moveSequence.isOn, Is.True);
                Assert.That(percentage.text, Is.EqualTo("37"));
            }
            finally
            {
                Preferences.DefaultStorage.Delete(key);
            }
        }

        [Test]
        public void ConfigManualApplyAndCloseUseTheSharedDraft()
        {
            string key = "yu5h1lib.ui.test.chat-performance." + System.Guid.NewGuid().ToString("N");
            try
            {
                var config = ChatTextPerformanceSettings.Mount((RectTransform)root.transform, chat, key);
                Assert.That(config.Show(), Is.True);
                config.SaveBar.ModeToggle.isOn = false;
                var fade = FindToggle(config, "淡入");
                fade.isOn = true;
                Assert.That(chat.TextPerformance.Fade, Is.False);
                Assert.That(config.Close(), Is.True);
                Assert.That(config.Show(), Is.True);
                Assert.That(fade.isOn, Is.False);
                fade.isOn = true;
                config.SaveBar.SaveButton.onClick.Invoke();
                Assert.That(chat.TextPerformance.Fade, Is.True);
                Assert.That(config.Close(), Is.True);
                Assert.That(config.Show(), Is.True);
                Assert.That(fade.isOn, Is.True);
            }
            finally
            {
                Preferences.DefaultStorage.Delete(key);
            }
        }

        [Test]
        public void ConfigReadFailureKeepsTheSavedDataAndCanRetry()
        {
            string key = "yu5h1lib.ui.test.chat-performance." + System.Guid.NewGuid().ToString("N");
            try
            {
                const string unreadable = "{broken";
                PlayerPrefs.SetString(key, unreadable);
                var config = ChatTextPerformanceSettings.Mount((RectTransform)root.transform, chat, key);
                Assert.That(config.Show(), Is.True);
                Assert.That(config.CanEdit, Is.False);
                Assert.That(PlayerPrefs.GetString(key), Is.EqualTo(unreadable));
                var retry = config.SaveBar.Actions.Find("Retry load").GetComponent<Button>();
                Assert.That(retry.gameObject.activeInHierarchy, Is.True);

                var value = ChatTextPerformance.Default;
                value.Fade = true;
                Preferences.DefaultStorage.Save(key, new Yu5h1Lib.Serialization.DataView
                {
                    ["textPerformance"] = JsonUtility.ToJson(value)
                });
                retry.onClick.Invoke();
                Assert.That(config.CanEdit, Is.True);
                Assert.That(chat.TextPerformance.Fade, Is.True);
                Assert.That(retry.gameObject.activeInHierarchy, Is.False);
            }
            finally
            {
                Preferences.DefaultStorage.Delete(key);
            }
        }

        [Test]
        public void NarrowConfigKeepsDurationEndpointsOnOneRow()
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(320, 600);
            string key = "yu5h1lib.ui.test.chat-performance." + System.Guid.NewGuid().ToString("N");
            try
            {
                var config = ChatTextPerformanceSettings.Mount((RectTransform)root.transform, chat, key);
                Assert.That(config.Show(), Is.True);
                FindToggle(config, "淡入").isOn = true;
                Canvas.ForceUpdateCanvases();
                var duration = System.Array.Find(config.Tabs.GetComponentsInChildren<RectTransform>(),
                    rect => rect.name == "時間（秒）");
                Assert.That(duration, Is.Not.Null);
                var bounds = duration.GetComponentsInChildren<InputField>();
                Assert.That(bounds.Length, Is.EqualTo(2));
                var first = (RectTransform)bounds[0].transform;
                var second = (RectTransform)bounds[1].transform;
                Assert.That(first.rect.width, Is.GreaterThanOrEqualTo(44f));
                Assert.That(second.rect.width, Is.GreaterThanOrEqualTo(44f));
                Assert.That(first.TransformPoint(first.rect.center).y,
                    Is.EqualTo(second.TransformPoint(second.rect.center).y).Within(.1f));
                Assert.That(first.TransformPoint(new Vector3(first.rect.xMax, 0, 0)).x,
                    Is.LessThan(second.TransformPoint(new Vector3(second.rect.xMin, 0, 0)).x));
            }
            finally
            {
                Preferences.DefaultStorage.Delete(key);
            }
        }

        private static Toggle FindToggle(SettingsPanel config, string row) =>
            System.Array.Find(config.Tabs.GetComponentsInChildren<Toggle>(true), toggle => toggle.transform.parent.name == row);

        private static float WorldY(RectTransform rect, float localY) =>
            rect.TransformPoint(new Vector3(0, localY, 0)).y;

        private static List<UIVertex> Capture(Text label)
        {
            Canvas.ForceUpdateCanvases();
            using var vertices = new VertexHelper();
            var populate = label.GetType().GetMethod("OnPopulateMesh",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null,
                new[] { typeof(VertexHelper) }, null);
            populate.Invoke(label, new object[] { vertices });
            var result = new List<UIVertex>();
            vertices.GetUIVertexStream(result);
            return result;
        }
    }
}
