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
    /// <summary>Exercises direction, loop and reuse through the imported acceptance scene.</summary>
    public sealed class ScrollViewSampleTests
    {
        private Scene sampleScene;

        [UnityTest]
        public IEnumerator SwitchesBothAxesAndLoopOnOneScrollView()
        {
            var scenes = AssetDatabase.FindAssets("UxUiAcceptanceDemo t:Scene", new[] { "Assets/Samples" });
            Assert.That(scenes, Has.Length.EqualTo(1), "Import the Yu5h1Lib.UI UxAcceptance sample.");
            string scenePath = AssetDatabase.GUIDToAssetPath(scenes[0]);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));

            sampleScene = SceneManager.GetSceneByPath(scenePath);
            var host = sampleScene.GetRootGameObjects().Select(root => root.GetComponentInChildren<UxUiAcceptanceHost>())
                .FirstOrDefault(candidate => candidate != null);
            Assert.That(host, Is.Not.Null);
            host.ShowCatalog();
            var card = host.transform.Find("HostRoot/Catalog/CatalogScroll/Viewport/ModuleGrid/scrollview");
            Assert.That(card, Is.Not.Null);
            card.GetComponent<Button>().onClick.Invoke();
            Assert.That(host.CurrentCase?.Id, Is.EqualTo("scrollview"), host.StatusText);
            var dashboard = host.ContentContainer.Find("ScrollViewDemo");
            var direction = dashboard.Find("Controls/Direction").GetComponent<Button>();
            var loop = dashboard.Find("Controls/Loop").GetComponent<Button>();
            var view = dashboard.GetComponentInChildren<ScrollView>();
            var bar = view.verticalScrollbar;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(view.vertical && !view.horizontal && !view.Loop, Is.True);
            var verticalCells = view.content.Cast<Transform>().ToArray();

            loop.onClick.Invoke();
            VerifyCycle(view, bar);
            direction.onClick.Invoke();
            Assert.That(dashboard.GetComponentsInChildren<ScrollView>(), Is.EqualTo(new[] { view }));
            Assert.That(view.horizontal && !view.vertical && view.Loop, Is.True);
            Assert.That(view.horizontalScrollbar, Is.SameAs(bar));
            Assert.That(view.content.Cast<Transform>().Any(verticalCells.Contains), Is.True);
            VerifyCycle(view, bar);

            loop.onClick.Invoke();
            view.ScrollToOffset(view.ContentSize + 20f);
            Assert.That(view.Loop, Is.False);
            Assert.That(view.Offset, Is.EqualTo(view.ContentSize - view.viewport.rect.width).Within(.05f));
            direction.onClick.Invoke();
            Assert.That(view.vertical && !view.horizontal && !view.Loop, Is.True);
            Assert.That(view.verticalScrollbar, Is.SameAs(bar));
            Assert.That(view.Offset, Is.Zero.Within(.05f));

            host.ShowCatalog();
            Assert.That(host.CurrentCase, Is.Null);
            Assert.That(host.ContentContainer.childCount, Is.Zero);
        }

        private static void VerifyCycle(ScrollView view, Scrollbar bar)
        {
            float cycle = view.ContentSize;
            Assert.That(view.Loop, Is.True);
            view.ScrollToOffset(cycle - 20f);
            Assert.That(view.content.Find("Item 1000"), Is.Not.Null);
            Assert.That(view.content.Find("Item 1"), Is.Not.Null);

            void Wheel(float pixels)
            {
                float delta = -pixels / view.scrollSensitivity;
                var pointer = new PointerEventData(EventSystem.current)
                {
                    scrollDelta = view.horizontal ? new Vector2(delta, 0f) : new Vector2(0f, delta),
                };
                ExecuteEvents.ExecuteHierarchy(view.viewport.gameObject, pointer, ExecuteEvents.scrollHandler);
            }

            Wheel(60f);
            Assert.That(view.Offset, Is.EqualTo(40f).Within(.05f));
            Wheel(-60f);
            Assert.That(view.Offset, Is.EqualTo(cycle - 20f).Within(.05f));
            Canvas.ForceUpdateCanvases();
            bar.value = .5f;
            Assert.That(view.Offset, Is.EqualTo(cycle * .5f).Within(.05f));

            view.ScrollToOffset(cycle - 20f);
            int created = view.CreatedItemCount;
            for (int i = 0; i < 6; i++)
                Wheel(cycle / 2f);
            Assert.That(view.Offset, Is.EqualTo(cycle - 20f).Within(.05f));
            Assert.That(view.CreatedItemCount, Is.EqualTo(created));
            TestContext.WriteLine($"{(view.horizontal ? "Horizontal" : "Vertical")}: {created} created, {view.content.childCount} active after three turns.");
        }

        [UnityTearDown]
        public IEnumerator UnloadSample()
        {
            if (sampleScene.IsValid() && sampleScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(sampleScene);
        }
    }
}
#endif
