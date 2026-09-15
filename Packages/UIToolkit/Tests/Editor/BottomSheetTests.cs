using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit.Tests
{
    public class BottomSheetTests
    {
        private GameObject host;
        private PanelSettings settings;
        private VisualElement root;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            host = new GameObject("BottomSheetTests");
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = settings;
            root = document.rootVisualElement;
            root.Add(BuildStructure());
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
        }

        /// <summary>The structure the control's documentation says a caller must supply.</summary>
        private static VisualElement BuildStructure(string omit = null)
        {
            var layer = new VisualElement { name = "sheet-layer" };
            layer.AddToClassList("hidden");

            void AddUnless(VisualElement parent, VisualElement child)
            {
                if (child.name != omit) parent.Add(child);
            }

            var sheet = new VisualElement { name = "sheet" };
            AddUnless(layer, new Button { name = "scrim" });
            AddUnless(layer, sheet);
            AddUnless(sheet, new VisualElement { name = "handle" });
            AddUnless(sheet, new Label { name = "sheet-title" });
            AddUnless(sheet, new Label { name = "sheet-kicker" });
            AddUnless(sheet, new Button { name = "sheet-close" });
            AddUnless(sheet, new ScrollView { name = "sheet-scroll" });
            return layer;
        }

        private static BottomSheet Sheet(VisualElement root) => new BottomSheet(root);

        [Test]
        public void ASheetStartsClosed()
        {
            Assert.IsFalse(Sheet(root).IsOpen);
        }

        [Test]
        public void OpeningShowsItAndSetsTheHeadings()
        {
            var sheet = Sheet(root);
            sheet.Open("Heading", "Context");

            Assert.IsTrue(sheet.IsOpen);
            Assert.AreEqual("Heading", root.Q<Label>("sheet-title").text);
            Assert.AreEqual("Context", root.Q<Label>("sheet-kicker").text);
        }

        [Test]
        public void ContentIsClearedOnEveryOpen()
        {
            var sheet = Sheet(root);
            sheet.Open("A", "");
            sheet.Content.Add(new Label("stale"));
            Assert.AreEqual(1, sheet.Content.childCount);

            sheet.Open("B", "");
            Assert.AreEqual(0, sheet.Content.childCount, "a reopened sheet must not show the last sheet's content");
        }

        [Test]
        public void ClosingIsNotInstant()
        {
            var sheet = Sheet(root);
            sheet.Open("A", "");
            sheet.Close();
            Assert.IsTrue(sheet.IsOpen, "it travels away rather than vanishing");
        }

        [Test]
        public void ItIsHiddenOnlyAfterTheAnimationFinishes()
        {
            var sheet = Sheet(root);
            sheet.Open("A", "");
            sheet.Close();

            for (int i = 0; i < 60 && sheet.IsOpen; i++) sheet.Tick(1f / 60, reduceMotion: false);
            Assert.IsFalse(sheet.IsOpen);
        }

        [Test]
        public void ReduceMotionClosesItInOneStep()
        {
            var sheet = Sheet(root);
            sheet.Open("A", "");
            sheet.Close();

            sheet.Tick(0, reduceMotion: true);
            Assert.IsFalse(sheet.IsOpen, "no one who asked for less motion should watch it travel");
        }

        [Test]
        public void TickingAClosedSheetIsHarmless()
        {
            var sheet = Sheet(root);
            Assert.DoesNotThrow(() => sheet.Tick(1f / 60, false));
            Assert.IsFalse(sheet.IsOpen);
        }

        [Test]
        public void ClosingTwiceIsHarmless()
        {
            var sheet = Sheet(root);
            sheet.Open("A", "");
            sheet.Close();
            Assert.DoesNotThrow(() => sheet.Close());
        }

        [Test]
        public void TheScrimButtonCloses()
        {
            var sheet = Sheet(root);
            sheet.Open("A", "");
            using (var evt = new NavigationSubmitEvent { target = root.Q<Button>("scrim") })
                root.Q<Button>("scrim").SendEvent(evt);

            for (int i = 0; i < 60 && sheet.IsOpen; i++) sheet.Tick(1f / 60, false);
            Assert.IsFalse(sheet.IsOpen);
        }

        [Test]
        public void EscapeClosesIt()
        {
            var sheet = Sheet(root);
            sheet.Open("A", "");

            using (var evt = KeyDownEvent.GetPooled('\0', KeyCode.Escape, EventModifiers.None))
            {
                evt.target = root.Q("sheet-layer");
                root.Q("sheet-layer").SendEvent(evt);
            }

            for (int i = 0; i < 60 && sheet.IsOpen; i++) sheet.Tick(1f / 60, false);
            Assert.IsFalse(sheet.IsOpen, "a modal that cannot be escaped is a trap");
        }

        [Test]
        public void TheHeightIsBoundedBelow()
        {
            var sheet = new BottomSheet(root, initialHeightFraction: .01f);
            sheet.Open("A", "");
            sheet.SetAvailableHeight(1000);
            sheet.Tick(1f / 60, false);

            Assert.GreaterOrEqual(root.Q("sheet").style.height.value.value, 250f,
                "a sheet shorter than its own handle cannot be grabbed again");
        }

        [Test]
        public void TheHeightFollowsTheRoomAvailable()
        {
            var sheet = new BottomSheet(root, initialHeightFraction: .5f);
            sheet.Open("A", "");

            sheet.SetAvailableHeight(1000);
            Assert.AreEqual(500f, root.Q("sheet").style.height.value.value, 1e-2f);

            sheet.SetAvailableHeight(600);
            Assert.AreEqual(300f, root.Q("sheet").style.height.value.value, 1e-2f);
        }

        [Test]
        public void AMissingElementIsReportedByName()
        {
            var incomplete = new VisualElement();
            incomplete.Add(BuildStructure(omit: "sheet-scroll"));

            var error = Assert.Throws<ArgumentException>(() => new BottomSheet(incomplete));
            StringAssert.Contains("sheet-scroll", error.Message,
                "the caller must be told which name is missing, not handed a null reference later");
        }

        [Test]
        public void ANullRootIsRefused()
        {
            Assert.Throws<ArgumentNullException>(() => new BottomSheet(null));
        }

        [UnityTest]
        public IEnumerator OpeningMovesFocusIntoTheSheet()
        {
            var outside = new Button { name = "outside" };
            root.Add(outside);
            yield return null;

            var sheet = Sheet(root);
            sheet.Open("A", "");
            yield return null;

            Assert.AreEqual(root.Q("handle"), root.panel.focusController.focusedElement,
                "a sheet that opens without focus cannot be driven by keyboard");
        }
    }
}
