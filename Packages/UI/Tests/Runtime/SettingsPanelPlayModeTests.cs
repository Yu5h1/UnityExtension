using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Yu5h1Lib.UI.Acceptance;

namespace Yu5h1Lib.UI.Tests
{
    public sealed class SettingsPanelPlayModeTests
    {
        [UnityTest]
        public IEnumerator SettingsPanelPlayModeSharesDraftAndRestoresOpenerFocus()
        {
            var key = "Yu5h1Lib.Tests.SettingsPanel." + System.Guid.NewGuid();
            var root = new GameObject("Settings Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var events = new GameObject("Settings EventSystem", typeof(EventSystem));
            var demo = new UxUiSettingsDemoCase(key);
            try
            {
                demo.Mount((RectTransform)root.transform);
                yield return null;
                var panels = root.GetComponentsInChildren<SettingsPanel>(true);
                var config = panels.Single(panel => panel.Groups != null);
                var setting = panels.Single(panel => panel.Groups == null);
                Activate(config.Opener);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(config.Tabs.ActiveButton.gameObject));
                var tab = config.Tabs.ActiveButton.gameObject;
                ExecuteEvents.Execute(tab, new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Right },
                    ExecuteEvents.moveHandler);
                Assert.That(config.Tabs.SelectedId, Is.EqualTo("details"));
                config.Tabs.Select("general", true);
                Activate(config.SaveBar.ModeToggle);
                var hints = config.GetComponentsInChildren<Toggle>().Single(control => control != config.SaveBar.ModeToggle);
                Activate(hints);
                Assert.That(hints.isOn, Is.False);
                Assert.That(PlayerPrefs.HasKey(key), Is.False);
                Assert.That(config.Close(), Is.True);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(config.Opener.gameObject));
                Activate(setting.Opener);
                var localField = setting.GetComponentsInChildren<Toggle>().Single(control => control != setting.SaveBar.ModeToggle);
                Assert.That(localField.isOn, Is.True);
                Activate(localField);
                Activate(setting.SaveBar.SaveButton);
                Assert.That(PlayerPrefs.HasKey(key), Is.True);
                Assert.That(setting.Close(), Is.True);
                Activate(config.Opener);
                Assert.That(hints.isOn, Is.False);
            }
            finally
            {
                demo.Unmount();
                Object.Destroy(root);
                Object.Destroy(events);
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        private static void Activate(Selectable control) =>
            ExecuteEvents.Execute(control.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }
}
