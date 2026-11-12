
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib.UI.Acceptance;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.UI.Tests
{
    /// <summary>Checks the ChatPanel and ScrollView through live uGUI geometry and public component entry points.</summary>
    public sealed class UxUiChatPanelTests
    {
        private GameObject root;
        private EventSystem events;
        private EventSystem priorEvents;
        private bool registeredEventSystem;
        private ChatPanel panel;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ChatPanel test", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)root.transform).sizeDelta = new Vector2(500, 600);
            events = new GameObject("Event system", typeof(EventSystem)).GetComponent<EventSystem>();
            panel = NewRect("Panel", root.transform, 500, 600).gameObject.AddComponent<ChatPanel>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            if (registeredEventSystem)
            {
                if (priorEvents != null)
                    EventSystem.current = priorEvents;
                InvokeEventSystemLifecycle("OnDisable");
            }
            Object.DestroyImmediate(events.gameObject);
            UxUiModuleRegistry.Clear();
        }

        [Test]
        public void PinnedUxInputsMatchManifestAndRuntimeDesign()
        {
            var package = PackageInfo.FindForAssetPath("Packages/com.yu5h1.ui/package.json");
            Assert.That(package, Is.Not.Null);
            TestContext.Progress.WriteLine($"Resolved Yu5h1Lib.UI: {package.resolvedPath}");
            string snapshot = Path.Combine(package.resolvedPath, "Tests", "Editor", "UX~");
            VerifyHash(snapshot, "ChatPanel/ChatPanel.md", "978beceee8f4942347d7c676911966943d1b046e08815bd4f6c91b0a05ca85a0");
            VerifyHash(snapshot, "ChatPanel/chat-panel-design.json", "e81b6d93440c179c7f706c3bb165e03511696c76981b72be280a4ffa9f5cbb04");
            VerifyHash(snapshot, "ChatPanel/chat-panel-cases.json", "0b1f2ba760afccc6ce92fa9b17e70c3b694d16103f84956eb21ba20af71e21bc");
            VerifyHash(snapshot, "ScrollView/ScrollView.md", "057e332b17fce5417a42ecc1ec9f2b9b74da3b2d71736fa267f2700a20275c29");
            VerifyHash(snapshot, "ScrollView/scroll-view-design.json", "45cd604c41a817542c9f23a17c626bf48c67d6ed04d70a066ec216cc2d503e8f");
            VerifyHash(snapshot, "ScrollView/scroll-view-cases.json", "a9882e8b407c18dd796972988415bd247c149b02586878e724e2a7f29db3030a");
            string chat = File.ReadAllText(Path.Combine(snapshot, "ChatPanel", "chat-panel-design.json"));
            string scroll = File.ReadAllText(Path.Combine(snapshot, "ScrollView", "scroll-view-design.json"));
            var options = new ChatPanel.Options();
            Assert.That(options.GraphemesPerSecond, Is.EqualTo(Token(chat, "streamingCharsPerSecond")));
            Assert.That(options.RowMaxWidthPercent, Is.EqualTo(Token(chat, "rowMaxWidthPercent")));
            Assert.That(options.ImageMaxHeight, Is.EqualTo(Token(chat, "imageMaxHeight")));
            Assert.That(options.ImagePlaceholderSize, Is.EqualTo(Token(chat, "imagePlaceholderSize")));
            Assert.That(options.ViewerPreviewInset, Is.EqualTo(Token(chat, "viewerPreviewInset")));
            Assert.That(options.ViewerZoomGestureThreshold, Is.EqualTo(Token(chat, "viewerZoomGestureThreshold")));
            Assert.That(options.FollowResumeTolerance, Is.EqualTo(Token(scroll, "followResumeTolerance")));
        }

        [TestCase(true, "思考中...5s", "思考中...6s")]
        [TestCase(false, "思考中...", "思考中.")]
        public void ThinkingUsesOneClockThenStopsCyclingWhenSecondsAreShown(bool seconds, string atFive, string atSix)
        {
            panel.Configure(new ChatPanel.Options { ShowThinkingSeconds = seconds });
            var reply = panel.BeginReply();
            Assert.That(reply.Text, Is.EqualTo("思考中."));
            panel.Advance(1f);
            Assert.That(reply.Text, Is.EqualTo("思考中.."));
            panel.Advance(1f);
            Assert.That(reply.Text, Is.EqualTo("思考中..."));
            panel.Advance(3f);
            Assert.That(reply.Text, Is.EqualTo(atFive));
            panel.Advance(1f);
            Assert.That(reply.Text, Is.EqualTo(atSix));
            var instant = panel.BeginReply();
            instant.Complete("Ready");
            Assert.That(instant.Text, Is.Empty);
            panel.Advance(.1f);
            Assert.That(instant.Text, Is.EqualTo("Rea"));
        }

        [Test]
        public void SnapshotsPreservePrefixAndLimitPerFrame()
        {
            var reply = panel.BeginReply();
            reply.Push("Hello");
            panel.Advance(.1f);
            Assert.That(reply.Text, Is.EqualTo("Hel"));
            reply.Push("Hello world!");
            Assert.That(reply.Text, Is.EqualTo("Hel"));
            panel.Advance(1f);
            Assert.That(reply.Text, Is.EqualTo("Hello world"));
            reply.Push("Different prefix");
            Assert.That(reply.Text, Is.Empty);
            panel.Advance(.01f);
            Assert.That(reply.Text, Is.Empty);
            reply.Complete("Different prefix finished!");
            panel.Advance(1f);
            Assert.That(reply.Text, Is.EqualTo("Differen"));
        }

        [Test]
        public void RevealUsesHostSpeedAndPace()
        {
            panel.Configure(new ChatPanel.Options { GraphemesPerSecond = 20 });
            var reply = panel.BeginReply();
            reply.Push("一二三四五六七八九十");
            panel.Advance(.25f);
            Assert.That(reply.Text, Is.EqualTo("一二三四五"));
            panel.Advance(.1f);
            Assert.That(reply.Text, Is.EqualTo("一二三四五六七"));
            reply.Pace = .5f;
            panel.Advance(.1f);
            Assert.That(reply.Text, Is.EqualTo("一二三四五六七八"));
        }

        [Test]
        public void RevealHoldsOpenGraphemeAndCompletesExactlyOnce()
        {
            const string final = "國旗🇹🇼與膚色👍🏽é👨‍👩‍👧‍👦";
            var reply = panel.BeginReply();
            int completed = 0;
            reply.RevealCompleted += () => completed++;
            reply.Push(final);
            foreach (string prefix in new[] { "國", "國旗", "國旗🇹🇼", "國旗🇹🇼與", "國旗🇹🇼與膚", "國旗🇹🇼與膚色", "國旗🇹🇼與膚色👍🏽" })
            {
                panel.Advance(1f / 30f);
                Assert.That(reply.Text, Is.EqualTo(prefix));
            }
            panel.ReduceMotion = true;
            panel.Advance(0);
            Assert.That(reply.Text, Is.EqualTo("國旗🇹🇼與膚色👍🏽é"));
            Assert.That(completed, Is.Zero);
            reply.Complete();
            Assert.That(reply.Text, Is.EqualTo(final));
            panel.Advance(1f);
            reply.Complete("Late update");
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(reply.Text, Is.EqualTo(final));
        }

        [Test]
        public void ConsecutiveFlagsAndLiteralJoinerDoNotMergeUnrelatedGraphemes()
        {
            Assert.That(GraphemeBoundaries.GetEnds("🇹🇼🇯🇵"), Is.EqualTo(new[] { 4, 8 }));
            Assert.That(GraphemeBoundaries.GetEnds("a\u200db"), Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void ClearCancelsCompletionAndDropsLateImageUpdates()
        {
            var reply = panel.BeginReply();
            var image = panel.AddImage();
            reply.RevealCompleted += () => Assert.Fail("Cleared reply completed.");
            reply.Complete("Text");
            panel.ClearMessages();
            panel.Advance(1f);
            image.SetFailed();
            Assert.That(panel.MessageContent.childCount, Is.Zero);
            Assert.That(panel.ViewerOpen, Is.False);
        }

        [Test]
        public void AlignsWithDatesAndGrowsTextOnlyVertically()
        {
            panel.Configure(new ChatPanel.Options { RowMaxWidthPercent = 70f, ShowTimestamps = true });
            var date = new DateTime(2026, 11, 10, 21, 3, 0);
            panel.AddMessage("First", false, date, "other", true);
            panel.AddMessage("Second", true, date.AddDays(1), "self", true);
            Assert.That(panel.MessageContent.GetChild(0).GetComponent<Text>().text, Is.EqualTo("2026/11/10"));
            Assert.That(panel.MessageContent.GetChild(2).GetComponent<Text>().text, Is.EqualTo("2026/11/11"));
            Assert.That(panel.MessageContent.Find("self/Timestamp").GetComponent<Text>().text, Is.EqualTo("21:03"));
            var left = Bubble("other");
            var right = Bubble("self");
            Assert.That(left.rect.width, Is.EqualTo(308f).Within(.05f));
            Assert.That(right.rect.width, Is.EqualTo(left.rect.width).Within(.05f));
            Assert.That(right.position.x, Is.GreaterThan(left.position.x));

            var reply = panel.BeginReply(id: "reply");
            reply.Complete(string.Concat(System.Linq.Enumerable.Repeat("A longer reply with several words. ", 8)));
            panel.Advance(1f / 30f);
            var bubble = Bubble("reply");
            float startWidth = bubble.rect.width;
            float startX = bubble.position.x;
            float startHeight = bubble.rect.height;
            panel.ReduceMotion = true;
            panel.Advance(0f);
            Assert.That(bubble.rect.width, Is.EqualTo(startWidth).Within(.05f));
            Assert.That(bubble.position.x, Is.EqualTo(startX).Within(.05f));
            Assert.That(bubble.rect.height, Is.GreaterThan(startHeight));

            ((RectTransform)panel.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 320f);
            panel.RefreshLayout();
            Assert.That(bubble.rect.width, Is.EqualTo(224f).Within(.05f));
            Assert.That(reply.IsComplete, Is.True);
        }

        [Test]
        public void ThumbnailLoadingFailureAndModalInteraction()
        {
            priorEvents = EventSystem.current;
            InvokeEventSystemLifecycle("OnEnable");
            registeredEventSystem = true;
            EventSystem.current = events;
            var safe = NewRect("Safe Area", root.transform, 300, 200);
            safe.anchoredPosition = new Vector2(-20, 30);
            panel.Configure(new ChatPanel.Options { ViewerSafeArea = safe });
            var pending = panel.AddImage(new Vector2(800f, 400f), "Wide");
            var failure = panel.AddImage(description: "Failure");
            Assert.That(pending.DisplaySize, Is.EqualTo(new Vector2(425f, 212.5f)));
            Assert.That(panel.OpenImage(pending), Is.False);
            Assert.That(failure.DisplaySize, Is.EqualTo(new Vector2(160f, 160f)));
            failure.SetFailed();
            Assert.That(failure.Ready, Is.False);
            Assert.That(panel.OpenImage(failure), Is.False);

            var texture = new Texture2D(80, 40);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 80, 40), new Vector2(.5f, .5f));
            try
            {
                pending.SetLoaded(sprite);
                Assert.That(pending.DisplaySize, Is.EqualTo(new Vector2(80, 40)));
                Assert.That(pending.Ready, Is.True);
                var opener = panel.MessageContent.GetChild(0).GetComponentInChildren<Button>();
                var closed = new System.Collections.Generic.List<ChatPanel.ViewerCloseReason>();
                panel.ImageViewerClosed += closed.Add;
                int opened = 0;
                panel.ImageViewerOpened += () => opened++;
                opener.onClick.Invoke();
                Assert.That(opened, Is.EqualTo(1));
                Assert.That(panel.OpenImage(pending), Is.False);
                Assert.That(opened, Is.EqualTo(1));
                Assert.That(panel.ViewerOpen, Is.True);
                Assert.That(panel.ViewerFull, Is.False);
                var viewer = root.transform.Find("Chat Image Viewer");
                var pictureRect = (RectTransform)viewer.Find("Wide");
                var picture = pictureRect.gameObject;
                Assert.That(pictureRect.rect.size, Is.EqualTo(new Vector2(268, 134)));
                Assert.That(pictureRect.position, Is.EqualTo(safe.position));
                Assert.That(events.currentSelectedGameObject, Is.EqualTo(picture));

                Pinch(picture, 100, 105);
                Assert.That(panel.ViewerFull, Is.False);
                Pinch(picture, 100, 125);
                Assert.That(panel.ViewerFull, Is.True);
                Assert.That(pictureRect.rect.size, Is.EqualTo(new Vector2(300, 150)));
                ExecuteEvents.Execute(picture, new PointerEventData(events) { scrollDelta = Vector2.down }, ExecuteEvents.scrollHandler);
                Assert.That(panel.ViewerFull, Is.False);
                ChatPanel.ViewerDismissSource? denied = null;
                panel.CanDismissImage = source => { denied = source; return false; };
                ExecuteEvents.Execute(picture, new BaseEventData(events), ExecuteEvents.cancelHandler);
                Assert.That(panel.ViewerOpen, Is.True);
                Assert.That(denied, Is.EqualTo(ChatPanel.ViewerDismissSource.Escape));
                panel.CanDismissImage = null;
                viewer.Find("Close image").GetComponent<Button>().onClick.Invoke();
                panel.CloseImage();
                Assert.That(closed, Is.EqualTo(new[] { ChatPanel.ViewerCloseReason.Cancel }));
                Assert.That(events.currentSelectedGameObject, Is.EqualTo(opener.gameObject));

                opener.onClick.Invoke();
                ExecuteEvents.Execute(viewer.gameObject, new PointerEventData(events), ExecuteEvents.pointerClickHandler);
                Assert.That(closed, Has.Count.EqualTo(2));
                Assert.That(panel.ViewerOpen, Is.False);
                opener.onClick.Invoke();
                var behavior = viewer.GetComponent("ChatImageViewer");
                InvokeLifecycle(behavior, "OnApplicationFocus", false);
                Assert.That(closed, Is.EqualTo(new[] { ChatPanel.ViewerCloseReason.Cancel,
                    ChatPanel.ViewerCloseReason.Cancel, ChatPanel.ViewerCloseReason.Programmatic }));
                opener.onClick.Invoke();
                panel.gameObject.SetActive(false);
                InvokeLifecycle(panel, "OnDisable");
                Assert.That(panel.ViewerOpen, Is.False);
                Assert.That(closed, Has.Count.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void HostSendAndBeginReplyRestoreFollowButHistoryDoesNot()
        {
            for (int i = 0; i < 40; i++)
                panel.AddMessage($"History {i}", fromHistory: true);
            var scroll = panel.ScrollView;
            Assert.That(scroll.MaxOffset, Is.GreaterThan(0f));
            scroll.StopFollowing();
            scroll.ScrollToOffset(0f);
            panel.AddMessage("Archived self message", true, fromHistory: true);
            Assert.That(scroll.Following, Is.False);
            Assert.That(scroll.Offset, Is.Zero);
            panel.AddMessage("Sent", true);
            Assert.That(scroll.Following, Is.True);
            Assert.That(scroll.Offset, Is.EqualTo(scroll.MaxOffset));

            scroll.StopFollowing();
            scroll.ScrollToOffset(0f);
            panel.BeginReply();
            Assert.That(scroll.Following, Is.True);
            Assert.That(scroll.Offset, Is.EqualTo(scroll.MaxOffset));
        }

        [Test]
        public void OneInputSystemWheelNotchPreservesReadingDuringReveal()
        {
            for (int i = 0; i < 40; i++)
                panel.AddMessage($"History {i}", fromHistory: true);
            var reply = panel.BeginReply();
            reply.Complete("A reply in progress");
            var scroll = panel.ScrollView;
            scroll.OnScroll(new PointerEventData(events) { scrollDelta = new Vector2(0f, 6f) });
            float readingOffset = scroll.Offset;
            Assert.That(scroll.Following, Is.False);
            Assert.That(scroll.MaxOffset - readingOffset, Is.GreaterThan(scroll.ResumeTolerance));
            panel.Advance(.2f);
            Assert.That(scroll.Offset, Is.EqualTo(readingOffset).Within(.05f));
        }

        [Test]
        public void HistoryReplacementKeepsTheReaderOnTheSameMessage()
        {
            void Populate(ChatPanel view, bool longer)
            {
                for (int i = 0; i < 40; i++)
                    view.AddMessage(i == 0 && longer ? new string('A', 300) : $"History {i}",
                        i % 2 == 0, id: $"history-{i}", fromHistory: true);
            }

            panel.ReplaceHistory(view => Populate(view, false));
            var scroll = panel.ScrollView;
            scroll.StopFollowing();
            scroll.ScrollToOffset(510f);
            var before = (RectTransform)panel.MessageContent.Find("history-20");
            float screenY = before.position.y;
            float offset = scroll.Offset;

            panel.ReplaceHistory(view => Populate(view, true));
            var after = (RectTransform)panel.MessageContent.Find("history-20");
            Assert.That(scroll.Following, Is.False);
            Assert.That(scroll.Offset, Is.GreaterThan(offset));
            Assert.That(after.position.y, Is.EqualTo(screenY).Within(.05f));
        }

        [Test]
        public void DeliveredSceneMountsChatPanelFromTheSharedSample()
        {
            string[] scenes = AssetDatabase.FindAssets("UxUiAcceptanceDemo t:Scene");
            Assert.That(scenes, Has.Length.EqualTo(1));
            string path = AssetDatabase.GUIDToAssetPath(scenes[0]);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,
                UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                UxUiAcceptanceHost host = null;
                foreach (var item in scene.GetRootGameObjects())
                    host ??= item.GetComponentInChildren<UxUiAcceptanceHost>();
                Assert.That(host, Is.Not.Null);
                host.InitializeHost();
                host.SelectCase("chatpanel");
                Canvas.ForceUpdateCanvases();
                Assert.That(host.CurrentCase.Id, Is.EqualTo("chatpanel"));
                var demo = host.GetComponentInChildren<UxUiChatPanelDemo>();
                Assert.That(demo.Panel.MessageContent.childCount, Is.EqualTo(4));
                Assert.That(demo.Panel.ScrollView.Following, Is.True);
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void PackageChatPanelPrefabSupportsAConsumerCanvas()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.yu5h1.ui/Runtime/Prefabs/ChatPanel.prefab");
            Assert.That(prefab, Is.Not.Null);
            var instance = Object.Instantiate(prefab, root.transform);
            var chat = instance.GetComponent<ChatPanel>();
            Assert.That(chat, Is.Not.Null);
            var rect = (RectTransform)instance.transform;
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));

            using (var serialized = new SerializedObject(chat))
            {
                var options = serialized.FindProperty("options");
                options.FindPropertyRelative("MessageBottomInset").intValue = 8;
                options.FindPropertyRelative("FollowResumeTolerance").floatValue = 12f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            chat.AddMessage("Message from the consumer", id: "consumer");
            Assert.That(chat.ScrollView.viewport.rect.size, Is.EqualTo(rect.rect.size));
            Assert.That(chat.ScrollView.ResumeTolerance, Is.EqualTo(12f));
            Assert.That(chat.MessageContent.GetComponent<VerticalLayoutGroup>().padding.bottom, Is.EqualTo(8));
            Assert.That(chat.MessageContent.Find("consumer").GetComponentInChildren<Text>().text,
                Is.EqualTo("Message from the consumer"));
            Assert.That(instance.GetComponentInChildren<InputField>(), Is.Null);
        }

        [TestCase(1280f, 720f, 36f, TestName = "SampleLayout_Desktop")]
        [TestCase(360f, 600f, 36f, TestName = "SampleLayout_PhonePortrait")]
        [TestCase(640f, 360f, 36f, TestName = "SampleLayout_PhoneLandscape")]
        [TestCase(280f, 500f, 78f, TestName = "SampleLayout_NarrowToolbarWraps")]
        public void SampleRowsKeepOnlyMessageViewportFlexible(float width, float height, float toolbarHeight)
        {
            var frame = NewRect("Sample", root.transform, width, height);
            var demo = frame.gameObject.AddComponent<UxUiChatPanelDemo>();
            demo.Build();
            var toolbar = (RectTransform)frame.Find("Demo controls");
            var composer = (RectTransform)frame.Find("Conversation/Composer slot/Sample host composer");
            var viewport = demo.Panel.ScrollView.viewport;
            var send = (RectTransform)composer.Find("送出");
            var input = composer.GetComponentInChildren<InputFieldAddon>();
            Assert.That(input, Is.Not.Null);
            var field = (InputField)input.Raw;
            Assert.That(field.GetComponent<InputFieldState>(), Is.Not.Null);
            var multiline = composer.GetComponentInChildren<MultilineInputField>();

            LayoutRebuilder.ForceRebuildLayoutImmediate(frame);
            multiline.RefreshLayout();
            LayoutRebuilder.ForceRebuildLayoutImmediate(frame);
            Assert.That(toolbar.rect.height, Is.EqualTo(toolbarHeight).Within(.05f));
            Assert.That(composer.rect.height, Is.EqualTo(44f).Within(.05f));
            Assert.That(viewport.rect.height, Is.EqualTo(height - toolbarHeight - 108f).Within(.05f));
            foreach (RectTransform button in toolbar)
            {
                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(toolbar, button);
                Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(toolbar.rect.xMin - .05f));
                Assert.That(bounds.max.x, Is.LessThanOrEqualTo(toolbar.rect.xMax + .05f));
            }

            var sendBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(frame, send);
            float sendBottom = sendBounds.min.y;
            float sendRight = sendBounds.max.x;
            field.text = "1\n2\n3\n4\n5";
            multiline.RefreshLayout();
            LayoutRebuilder.ForceRebuildLayoutImmediate(frame);
            Assert.That(multiline.VisibleLineCount, Is.EqualTo(5));
            Assert.That(multiline.HasOverflow, Is.False);
            float expanded = composer.rect.height;
            Assert.That(expanded, Is.EqualTo(111f).Within(.05f));
            Assert.That(viewport.rect.height, Is.EqualTo(height - toolbarHeight - 64f - expanded).Within(.05f));

            field.text += "\n6\n7";
            multiline.RefreshLayout();
            LayoutRebuilder.ForceRebuildLayoutImmediate(frame);
            Assert.That(multiline.HasOverflow, Is.True);
            Assert.That(composer.rect.height, Is.EqualTo(expanded).Within(.05f));
            Assert.That(send.rect.height, Is.EqualTo(38f).Within(.05f));
            Assert.That(RectTransformUtility.CalculateRelativeRectTransformBounds(frame, send).max.x,
                Is.EqualTo(sendRight).Within(.05f));
            Assert.That(RectTransformUtility.CalculateRelativeRectTransformBounds(frame, send).min.y,
                Is.EqualTo(sendBottom).Within(.05f));

            field.text = "1";
            multiline.RefreshLayout();
            LayoutRebuilder.ForceRebuildLayoutImmediate(frame);
            Assert.That(composer.rect.height, Is.EqualTo(44f).Within(.05f));
            Assert.That(viewport.rect.height, Is.EqualTo(height - toolbarHeight - 108f).Within(.05f));
            TestContext.WriteLine($"{width}×{height} logical: toolbar {toolbarHeight:0.#}, composer 44→{expanded:0.#}→44, message viewport {viewport.rect.height:0.#}");
        }

        [Test]
        public void SampleDragKeepsReadingPositionAcrossPortraitAndLandscapeReflow()
        {
            var frame = NewRect("Sample", root.transform, 360f, 600f);
            var demo = frame.gameObject.AddComponent<UxUiChatPanelDemo>();
            demo.Build();
            var view = demo.Panel;
            var scroll = view.ScrollView;
            scroll.inertia = false;

            void Resize(float width, float height)
            {
                frame.sizeDelta = new Vector2(width, height);
                LayoutRebuilder.ForceRebuildLayoutImmediate(frame);
                InvokeLifecycle(view, "LateUpdate");
                InvokeLifecycle(scroll, "LateUpdate");
            }

            void Drag(float distance)
            {
                var start = RectTransformUtility.WorldToScreenPoint(null,
                    scroll.viewport.TransformPoint(scroll.viewport.rect.center));
                var pointer = new PointerEventData(events) { pointerId = 1, position = start, pressPosition = start };
                ExecuteEvents.ExecuteHierarchy(scroll.viewport.gameObject, pointer, ExecuteEvents.initializePotentialDrag);
                ExecuteEvents.ExecuteHierarchy(scroll.viewport.gameObject, pointer, ExecuteEvents.beginDragHandler);
                pointer.delta = Vector2.up * distance;
                pointer.position += pointer.delta;
                ExecuteEvents.ExecuteHierarchy(scroll.viewport.gameObject, pointer, ExecuteEvents.dragHandler);
                ExecuteEvents.ExecuteHierarchy(scroll.viewport.gameObject, pointer, ExecuteEvents.endDragHandler);
            }

            float RowTop(RectTransform row) => scroll.viewport.rect.yMax -
                scroll.viewport.InverseTransformPoint(row.TransformPoint(new Vector3(0f, row.rect.yMax))).y;

            RectTransform FirstVisible()
            {
                foreach (RectTransform row in view.MessageContent)
                    if (RowTop(row) + row.rect.height > .001f)
                        return row;
                throw new InvalidOperationException("No visible message.");
            }

            Resize(360f, 600f);
            view.ReplaceHistory(chat =>
            {
                for (int i = 0; i < 40; i++)
                    chat.AddMessage($"第 {i + 1} 則歷史訊息。可以向上捲動，觀察畫面是否保留閱讀位置。",
                        id: $"history-{i}", fromHistory: true);
            });
            Drag(-165f);
            Assert.That(scroll.Following, Is.False);
            Assert.That(scroll.MaxOffset - scroll.Offset, Is.EqualTo(165f).Within(.05f));
            var row = FirstVisible();
            float top = RowTop(row);
            float portraitHeight = scroll.ContentSize;

            Resize(640f, 360f);
            Assert.That(scroll.ContentSize, Is.LessThan(portraitHeight));
            Assert.That(scroll.Following, Is.False);
            Assert.That(FirstVisible(), Is.SameAs(row));
            Assert.That(RowTop(row), Is.EqualTo(top).Within(.05f));

            Resize(360f, 600f);
            Assert.That(FirstVisible(), Is.SameAs(row));
            Assert.That(RowTop(row), Is.EqualTo(top).Within(.05f));
            Assert.That(scroll.Following, Is.False);

            Drag(scroll.MaxOffset - scroll.Offset);
            Assert.That(scroll.Offset, Is.EqualTo(scroll.MaxOffset).Within(.05f));
            Assert.That(scroll.Following, Is.True);
            Resize(640f, 360f);
            Assert.That(scroll.Offset, Is.EqualTo(scroll.MaxOffset).Within(.05f));
        }

        private void InvokeEventSystemLifecycle(string method) =>
            InvokeLifecycle(events, method);

        private static void InvokeLifecycle(Component component, string method, params object[] args) =>
            component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, args);

        private RectTransform Bubble(string id) => (RectTransform)panel.MessageContent.Find(id + "/Bubble");

        private void Pinch(GameObject picture, float initial, float final)
        {
            var first = new PointerEventData(events) { pointerId = 7, position = Vector2.zero };
            var second = new PointerEventData(events) { pointerId = 9, position = new Vector2(initial, 0) };
            ExecuteEvents.Execute(picture, first, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(picture, second, ExecuteEvents.pointerDownHandler);
            second.position = new Vector2(final, 0);
            ExecuteEvents.Execute(picture, second, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(picture, second, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(picture, first, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(picture, first, ExecuteEvents.pointerClickHandler);
        }

        private static void VerifyHash(string root, string file, string expected)
        {
            using var sha = SHA256.Create();
            string actual = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(root, file))))
                .Replace("-", "").ToLowerInvariant();
            Assert.That(actual, Is.EqualTo(expected), file);
        }

        private static float Token(string json, string name)
        {
            var value = Regex.Match(json, "\"" + name + "\"\\s*:\\s*\\{\\s*\"value\"\\s*:\\s*([0-9.]+)");
            Assert.That(value.Success, Is.True, name);
            return float.Parse(value.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        private static RectTransform NewRect(string name, Transform parent, float width, float height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }
    }
}
