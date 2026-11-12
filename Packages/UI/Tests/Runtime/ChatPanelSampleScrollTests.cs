#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Yu5h1Lib.UI.Acceptance;

namespace Yu5h1Lib.UI.Tests
{
    /// <summary>Runs history and follow transitions in the imported ChatPanel sample scene.</summary>
    public sealed class ChatPanelSampleScrollTests
    {
        private const float Tolerance = .05f;

        private Scene sampleScene;

        [UnityTest]
        public IEnumerator HistoryKeepsReadingPositionAndScrollingToBottomRestoresFollow()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.yu5h1.ui/package.json");
            Assert.That(package, Is.Not.Null);
            TestContext.WriteLine($"Yu5h1Lib.UI resolved from {package.resolvedPath}");

            var scenes = AssetDatabase.FindAssets("UxUiAcceptanceDemo t:Scene", new[] { "Assets/Samples" });
            Assert.That(scenes, Has.Length.EqualTo(1), "Import the Yu5h1Lib.UI UxAcceptance sample.");
            string scenePath = AssetDatabase.GUIDToAssetPath(scenes[0]);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));

            sampleScene = SceneManager.GetSceneByPath(scenePath);
            var host = sampleScene.GetRootGameObjects()
                .Select(root => root.GetComponentInChildren<UxUiAcceptanceHost>())
                .FirstOrDefault(candidate => candidate != null);
            Assert.That(host, Is.Not.Null);
            host.SelectCase("chatpanel");
            Assert.That(host.CurrentCase?.Id, Is.EqualTo("chatpanel"), host.StatusText);
            var demo = host.ContentContainer.GetComponentInChildren<UxUiChatPanelDemo>();
            Assert.That(demo, Is.Not.Null);
            var frame = (RectTransform)demo.transform;
            frame.anchorMin = frame.anchorMax = Vector2.one * .5f;
            frame.sizeDelta = new Vector2(360f, 600f);
            Canvas.ForceUpdateCanvases();

            var panel = demo.Panel;
            var scroll = panel.ScrollView;
            scroll.inertia = false;
            var loadHistory = demo.transform.Find("Demo controls/載入歷史")?.GetComponent<Button>();
            Assert.That(loadHistory, Is.Not.Null);
            loadHistory.onClick.Invoke();
            yield return null;
            Assert.That(scroll.MaxOffset, Is.GreaterThan(165f));
            Assert.That(scroll.Following, Is.True);
            AssertAtBottom(scroll);

            Drag(scroll, -165f);
            var original = FirstVisible(panel);
            float originalTop = RowTop(scroll, original);
            Assert.That(scroll.Following, Is.False);
            Assert.That(scroll.MaxOffset - scroll.Offset, Is.EqualTo(165f).Within(Tolerance));

            string originalId = original.name;
            loadHistory.onClick.Invoke();
            yield return null;
            var reloaded = FirstVisible(panel);
            Assert.That(reloaded, Is.Not.SameAs(original));
            Assert.That(reloaded.name, Is.EqualTo(originalId));
            Assert.That(RowTop(scroll, reloaded), Is.EqualTo(originalTop).Within(Tolerance));
            Assert.That(scroll.Following, Is.False);
            TestContext.WriteLine($"Sample history reload: {reloaded.name}, row top {originalTop:0.##} → {RowTop(scroll, reloaded):0.##}");

            panel.ClearMessages();
            panel.ReplaceHistory(chat => AddHistory(chat, 0));
            Drag(scroll, -165f);
            Assert.That(scroll.Following, Is.False);
            var reading = FirstVisible(panel);
            string readingId = reading.name;
            float readingTop = RowTop(scroll, reading);
            float readingOffset = scroll.Offset;
            float contentBefore = scroll.ContentSize;

            panel.ReplaceHistory(chat => AddHistory(chat, 4));
            yield return null;
            var retained = FirstVisible(panel);
            Assert.That(retained.name, Is.EqualTo(readingId));
            Assert.That(RowTop(scroll, retained), Is.EqualTo(readingTop).Within(Tolerance));
            Assert.That(scroll.Following, Is.False);
            Assert.That(scroll.ContentSize, Is.GreaterThan(contentBefore));
            Assert.That(scroll.Offset, Is.GreaterThan(readingOffset));
            TestContext.WriteLine($"Four older messages prepended: {retained.name}, row top {readingTop:0.##} → {RowTop(scroll, retained):0.##}, scroll offset {readingOffset:0.##} → {scroll.Offset:0.##}");

            Drag(scroll, scroll.MaxOffset - scroll.Offset);
            Assert.That(scroll.Following, Is.True);
            AssertAtBottom(scroll);
            float previousBottom = scroll.MaxOffset;
            panel.AddMessage("New message from the other participant.", id: "latest");
            yield return null;
            Assert.That(scroll.MaxOffset, Is.GreaterThan(previousBottom));
            Assert.That(scroll.Following, Is.True);
            AssertAtBottom(scroll);
            TestContext.WriteLine($"User drag resumed follow: bottom moved from {previousBottom:0.##} to {scroll.Offset:0.##} with the next message.");
        }

        [UnityTearDown]
        public IEnumerator UnloadSample()
        {
            if (sampleScene.IsValid() && sampleScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(sampleScene);
        }

        private static void AddHistory(ChatPanel panel, int olderCount)
        {
            for (int i = 0; i < olderCount; i++)
                panel.AddMessage($"Older message {i + 1}", id: $"older-{i}", fromHistory: true);
            for (int i = 0; i < 40; i++)
                panel.AddMessage($"History message {i + 1}", id: $"reading-{i}", fromHistory: true);
        }

        private static RectTransform FirstVisible(ChatPanel panel)
        {
            foreach (RectTransform row in panel.MessageContent)
                if (row.gameObject.activeInHierarchy && RowTop(panel.ScrollView, row) + row.rect.height > .001f)
                    return row;
            throw new AssertionException("No visible history row.");
        }

        private static float RowTop(FollowingScrollView scroll, RectTransform row) => scroll.viewport.rect.yMax -
            scroll.viewport.InverseTransformPoint(row.TransformPoint(new Vector3(0f, row.rect.yMax))).y;

        private static void AssertAtBottom(FollowingScrollView scroll) =>
            Assert.That(scroll.Offset, Is.EqualTo(scroll.MaxOffset).Within(Tolerance));

        private static void Drag(FollowingScrollView scroll, float distance)
        {
            Assert.That(EventSystem.current, Is.Not.Null);
            Vector2 localStart = scroll.viewport.rect.center;
            Vector2 start = RectTransformUtility.WorldToScreenPoint(null, scroll.viewport.TransformPoint(localStart));
            Vector2 end = RectTransformUtility.WorldToScreenPoint(null,
                scroll.viewport.TransformPoint(localStart + Vector2.up * distance));
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 1, position = start, pressPosition = start };
            var target = scroll.viewport.gameObject;
            ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.beginDragHandler);
            pointer.delta = end - start;
            pointer.position = end;
            ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.endDragHandler);
        }
    }
}
#endif
