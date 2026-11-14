using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Yu5h1Lib.UI.Acceptance;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.UI.Tests
{
    /// <summary>Exercises the delivered tabs with native uGUI move events and checks an unsubmitted settings edit.</summary>
    public sealed class TabsKeyboardAcceptanceTests
    {
        private GameObject root;
        private GameObject events;

        [SetUp]
        public void CreateScene()
        {
            root = new GameObject("Tabs acceptance canvas", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            events = new GameObject("Tabs acceptance event system", typeof(EventSystem));
        }

        [TearDown]
        public void DestroyScene()
        {
            Object.Destroy(root);
            Object.Destroy(events);
        }

        [UnityTest]
        public IEnumerator RowAndColumnConsumeOnlyTheirOwnAxesAndWrapWithFocus()
        {
            var tabs = Tabs.Create(root.transform);
            tabs.SetItems(new[]
            {
                new Tabs.Item("first", "First"),
                new Tabs.Item("middle", "Middle"),
                new Tabs.Item("last", "Last")
            });
            yield return null;

            Assert.That(tabs.Orientation, Is.EqualTo(Tabs.Direction.Row));
            tabs.ActiveButton.Select();
            Move(tabs, MoveDirection.Right, "middle", true);
            Move(tabs, MoveDirection.Up, "middle", false);
            Move(tabs, MoveDirection.Down, "middle", false);
            Move(tabs, MoveDirection.Left, "first", true);
            Move(tabs, MoveDirection.Left, "last", true);
            Move(tabs, MoveDirection.Right, "first", true);
            Move(tabs, MoveDirection.Right, "middle", true);

            var middle = tabs.ActiveButton;
            tabs.Orientation = Tabs.Direction.Column;
            yield return null;
            AssertSelection(tabs, "middle");
            Assert.That(tabs.ActiveButton, Is.SameAs(middle));
            Move(tabs, MoveDirection.Right, "middle", false);
            Move(tabs, MoveDirection.Left, "middle", false);
            Move(tabs, MoveDirection.Down, "last", true);
            Move(tabs, MoveDirection.Down, "first", true);
            Move(tabs, MoveDirection.Up, "last", true);
            Move(tabs, MoveDirection.Up, "middle", true);
        }

        [UnityTest]
        public IEnumerator ManualDraftSurvivesBothAxesAndBothOrientationChanges()
        {
            var key = "Yu5h1Lib.Tests.Tabs." + Guid.NewGuid();
            var demo = new UxUiSettingsDemoCase(key);
            try
            {
                demo.Mount((RectTransform)root.transform);
                yield return null;
                var config = root.GetComponentsInChildren<SettingsPanel>(true).Single(panel => panel.Groups != null);
                var tabs = config.Tabs;
                Submit(config.Opener);
                Submit(config.SaveBar.ModeToggle);

                var draft = VisibleField(config);
                Assert.That(config.SaveBar.ModeToggle.isOn, Is.False);
                Assert.That(draft.isOn, Is.True);
                Submit(draft);
                AssertDraft(config, draft, key);

                tabs.ActiveButton.Select();
                Move(tabs, MoveDirection.Right, "details", true);
                Assert.That(draft.gameObject.activeInHierarchy, Is.False);
                Move(tabs, MoveDirection.Left, "general", true);
                AssertDraft(config, draft, key);

                var selected = tabs.ActiveButton;
                tabs.Orientation = Tabs.Direction.Column;
                yield return null;
                AssertSelection(tabs, "general");
                Assert.That(tabs.ActiveButton, Is.SameAs(selected));
                AssertDraft(config, draft, key);
                Move(tabs, MoveDirection.Down, "details", true);
                Assert.That(draft.gameObject.activeInHierarchy, Is.False);

                tabs.Orientation = Tabs.Direction.Row;
                yield return null;
                AssertSelection(tabs, "details");
                Assert.That(draft.gameObject.activeInHierarchy, Is.False);
                Assert.That(draft.isOn, Is.False);
                Move(tabs, MoveDirection.Left, "general", true);
                AssertDraft(config, draft, key);
            }
            finally
            {
                demo.Unmount();
                PlayerPrefs.DeleteKey(key);
            }
        }

        private static Toggle VisibleField(SettingsPanel panel) =>
            panel.GetComponentsInChildren<Toggle>().Single(toggle => toggle != panel.SaveBar.ModeToggle);

        private static void AssertDraft(SettingsPanel panel, Toggle draft, string key)
        {
            Assert.That(VisibleField(panel), Is.SameAs(draft), "The field is the original object.");
            Assert.That(draft.isOn, Is.False, "The edited value is retained.");
            Assert.That(panel.SaveBar.ModeToggle.isOn, Is.False, "The panel remains in manual mode.");
            Assert.That(PlayerPrefs.HasKey(key), Is.False, "The draft is still unsubmitted.");
        }

        private static void Move(Tabs tabs, MoveDirection direction, string expected, bool consumed)
        {
            var input = new AxisEventData(EventSystem.current) { moveDir = direction };
            ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject, input, ExecuteEvents.moveHandler);
            Assert.That(input.used, Is.EqualTo(consumed), direction.ToString());
            AssertSelection(tabs, expected);
        }

        private static void AssertSelection(Tabs tabs, string expected)
        {
            Assert.That(tabs.SelectedId, Is.EqualTo(expected));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(tabs.ActiveButton.gameObject));
        }

        private static void Submit(Selectable target) =>
            ExecuteEvents.Execute(target.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }
}
