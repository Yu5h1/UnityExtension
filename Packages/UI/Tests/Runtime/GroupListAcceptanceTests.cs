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
    /// <summary>Exercises group buttons and last-selected categories in the delivered Config demo.</summary>
    public sealed class GroupListAcceptanceTests
    {
        [UnityTest]
        public IEnumerator ConfigRestoresEachGroupsLastSelectedTab()
        {
            var root = new GameObject("GroupList acceptance canvas", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var events = new GameObject("GroupList acceptance event system", typeof(EventSystem));
            var demo = new UxUiSettingsDemoCase("Yu5h1Lib.Tests.GroupList." + Guid.NewGuid());
            try
            {
                demo.Mount((RectTransform)root.transform);
                yield return null;
                var config = root.GetComponentsInChildren<SettingsPanel>(true).Single(panel => panel.Groups != null);
                Submit(config.Opener);
                AssertVisiblePage(config, "hints", "general", "顯示提示");

                Submit(FindButton(config.Tabs, "details"));
                AssertVisiblePage(config, "hints", "details", "提示會顯示在操作區。");

                Submit(FindButton(config.Groups, "sound"));
                AssertVisiblePage(config, "sound", "general", "播放音效");
                Submit(FindButton(config.Groups, "hints"));
                AssertVisiblePage(config, "hints", "details", "提示會顯示在操作區。");
                Submit(FindButton(config.Groups, "sound"));
                AssertVisiblePage(config, "sound", "general", "播放音效");

                Submit(FindButton(config.Tabs, "details"));
                AssertVisiblePage(config, "sound", "details", "音效會在操作時播放。");
                Submit(FindButton(config.Groups, "hints"));
                Submit(FindButton(config.Tabs, "general"));
                AssertVisiblePage(config, "hints", "general", "顯示提示");
                Submit(FindButton(config.Groups, "sound"));
                AssertVisiblePage(config, "sound", "details", "音效會在操作時播放。");
                Submit(FindButton(config.Groups, "hints"));
                AssertVisiblePage(config, "hints", "general", "顯示提示");
            }
            finally
            {
                demo.Unmount();
                Object.Destroy(root);
                Object.Destroy(events);
            }
        }

        private static Button FindButton(Component container, string id) =>
            container.GetComponentsInChildren<Button>().Single(button => button.name == id);

        private static void AssertVisiblePage(SettingsPanel panel, string group, string tab, string text)
        {
            Assert.That(panel.Groups.SelectedId, Is.EqualTo(group), "GroupList selection");
            Assert.That(panel.ActiveGroup, Is.EqualTo(group), "Config group");
            Assert.That(panel.Tabs.SelectedId, Is.EqualTo(tab), "Restored category");
            Assert.That(panel.Tabs.GetComponentsInChildren<Text>().Select(label => label.text),
                Does.Contain(text), "Visible page belongs to the restored category and group");
        }

        private static void Submit(Selectable target) =>
            ExecuteEvents.Execute(target.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }
}
