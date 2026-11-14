using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Yu5h1Lib.UI.Acceptance;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.UI.Tests
{
    /// <summary>Exercises live theme changes in the delivered settings case through native uGUI events.</summary>
    public sealed class SettingsPanelThemeSwitchTests
    {
        private const string CaseId = "settingspanel";
        private const string CaseDescription = "Config、Setting 與確認保存";

        private string key;
        private string[] previousCaseIds;
        private IUxUiModuleDemoCase previousCase;
        private GameObject hostObject;
        private GameObject eventSystemObject;
        private UxUiAcceptanceHost host;
        private SettingsPanel config;
        private SettingsPanel setting;
        private int writes;
        private int applies;

        [UnitySetUp]
        public IEnumerator MountSettings()
        {
            key = "Yu5h1Lib.Tests.LiveTheme." + Guid.NewGuid();
            writes = applies = 0;
            previousCaseIds = UxUiModuleRegistry.AllCases.Select(item => item.Id).ToArray();
            previousCase = UxUiModuleRegistry.Get(CaseId);
            if (EventSystem.current == null)
                eventSystemObject = new GameObject("Live theme event system", typeof(EventSystem));
            hostObject = new GameObject("Live theme settings canvas", typeof(RectTransform), typeof(Canvas));
            host = hostObject.AddComponent<UxUiAcceptanceHost>();
            var demo = new UxUiSettingsDemoCase(key,
                (_, _, _) => { writes++; return Task.CompletedTask; },
                (_, _, _) => { applies++; return Task.CompletedTask; });
            UxUiModuleRegistry.Register(demo, CaseDescription);
            host.SelectCase(CaseId);
            yield return null;

            Assert.That(host.CurrentCase, Is.SameAs(demo));
            var panels = host.ContentContainer.GetComponentsInChildren<SettingsPanel>(true);
            config = panels.Single(panel => panel.Groups != null);
            setting = panels.Single(panel => panel.Groups == null);
            Assert.That(writes, Is.Zero);
            Assert.That(applies, Is.EqualTo(1));
            AssertCasePalette(UguiControlPalette.Dark);
        }

        [UnityTearDown]
        public IEnumerator UnmountSettings()
        {
            if (hostObject != null)
            {
                hostObject.SetActive(false);
                Object.Destroy(hostObject);
            }
            if (eventSystemObject != null)
                Object.Destroy(eventSystemObject);

            foreach (var id in UxUiModuleRegistry.AllCases.Select(item => item.Id).Except(previousCaseIds).ToArray())
                UxUiModuleRegistry.Unregister(id);
            if (previousCase != null)
                UxUiModuleRegistry.Register(previousCase, CaseDescription);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConfigPointerSwitchesInPlaceAndPreservesADraftAndGroupHistory()
        {
            Submit(FindButton(host.ContentContainer, "Tab direction"));
            Submit(config.Opener);
            Submit(config.SaveBar.ModeToggle);
            Submit(FindButton(config.Tabs, "details"));
            Submit(FindButton(config.Tabs, "general"));
            Submit(FindButton(config.Groups, "sound"));
            var field = VisibleField(config);
            Assert.That(field.isOn, Is.True);
            Submit(field);
            Submit(FindButton(config.Tabs, "details"));

            var tab = config.Tabs.ActiveButton;
            var themeSwitch = FindButton(config.HeaderActions, "Theme switch");
            var dialog = FindImage(config, "Settings panel");
            var activeGroup = config.Groups.ActiveButton;
            tab.Select();
            yield return null;

            foreach (var palette in new[] { UguiControlPalette.Light, UguiControlPalette.Dark })
            {
                Click(themeSwitch);
                yield return null;

                Assert.That(config.IsOpen, Is.True);
                Assert.That(FindImage(config, "Settings panel"), Is.SameAs(dialog));
                Assert.That(FindButton(config.HeaderActions, "Theme switch"), Is.SameAs(themeSwitch));
                Assert.That(config.Groups.ActiveButton, Is.SameAs(activeGroup));
                Assert.That(config.ActiveGroup, Is.EqualTo("sound"));
                Assert.That(config.Tabs.Orientation, Is.EqualTo(Tabs.Direction.Column));
                Assert.That(config.Tabs.SelectedId, Is.EqualTo("details"));
                Assert.That(config.Tabs.ActiveButton, Is.SameAs(tab));
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(tab.gameObject));
                AssertDraft(config, field, palette);
                AssertCasePalette(palette);
            }

            Submit(FindButton(config.Groups, "hints"));
            Assert.That(config.Tabs.SelectedId, Is.EqualTo("general"));
            Submit(FindButton(config.Groups, "sound"));
            Assert.That(config.Tabs.SelectedId, Is.EqualTo("details"));
            Submit(FindButton(config.Tabs, "general"));
            Assert.That(VisibleField(config), Is.SameAs(field));
            AssertDraft(config, field, UguiControlPalette.Dark);
        }

        [UnityTest]
        public IEnumerator SettingKeyboardSwitchesInPlaceAndKeepsTheFocusedControl()
        {
            Submit(setting.Opener);
            Submit(setting.SaveBar.ModeToggle);
            var field = VisibleField(setting);
            Assert.That(field.isOn, Is.True);
            Submit(field);

            var tab = setting.Tabs.ActiveButton;
            var themeSwitch = FindButton(setting.HeaderActions, "Theme switch");
            var saveButton = setting.SaveBar.SaveButton;
            themeSwitch.Select();
            yield return null;

            foreach (var palette in new[] { UguiControlPalette.Light, UguiControlPalette.Dark })
            {
                Submit(themeSwitch);
                yield return null;

                Assert.That(setting.IsOpen, Is.True);
                Assert.That(setting.Tabs.Orientation, Is.EqualTo(Tabs.Direction.Row));
                Assert.That(setting.Tabs.SelectedId, Is.EqualTo("general"));
                Assert.That(setting.Tabs.ActiveButton, Is.SameAs(tab));
                Assert.That(setting.SaveBar.SaveButton, Is.SameAs(saveButton));
                Assert.That(FindButton(setting.HeaderActions, "Theme switch"), Is.SameAs(themeSwitch));
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(themeSwitch.gameObject));
                Assert.That(VisibleField(setting), Is.SameAs(field));
                AssertDraft(setting, field, palette);
                AssertCasePalette(palette);
            }

            field.Select();
            yield return null;
            EventSystem.current.SetSelectedGameObject(null);
            Click(themeSwitch);
            yield return null;

            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(field.gameObject));
            Assert.That(setting.Tabs.ActiveButton, Is.SameAs(tab));
            AssertDraft(setting, field, UguiControlPalette.Light);
            AssertCasePalette(UguiControlPalette.Light);
        }

        private void AssertDraft(SettingsPanel panel, Toggle field, UguiControlPalette palette)
        {
            Assert.That(panel.SaveBar.ModeToggle.isOn, Is.False);
            Assert.That(panel.SaveBar.SaveButton.IsInteractable(), Is.True);
            Assert.That(field.isOn, Is.False);
            Assert.That(field.targetGraphic.color, Is.EqualTo(palette.Hover), "Sample field background binding");
            Assert.That(field.graphic.color, Is.EqualTo(palette.Accent), "Sample field selection binding");
            Assert.That(field.GetComponentInChildren<Text>(true).color, Is.EqualTo(palette.ItemText), "Sample field label binding");
            Assert.That(writes, Is.Zero);
            Assert.That(applies, Is.EqualTo(1));
            Assert.That(PlayerPrefs.HasKey(key), Is.False);
        }

        private void AssertCasePalette(UguiControlPalette palette)
        {
            Assert.That(FindImage(host.ContentContainer, "SettingsDemo").color, Is.EqualTo(palette.PageBackground));
            foreach (var panel in new[] { config, setting })
            {
                var dialog = FindImage(panel, "Settings panel");
                Assert.That(dialog.color, Is.EqualTo(palette.PanelBackground));
                Assert.That(FindImage(panel, "Backdrop").color, Is.EqualTo(palette.Overlay));
                var title = dialog.GetComponentsInChildren<Text>(true).Single(label => label.name == "Title");
                Assert.That(title.color, Is.EqualTo(palette.PanelHeading));
                Assert.That(panel.SaveBar.ModeToggle.targetGraphic.color, Is.EqualTo(palette.Hover));
                Assert.That(panel.SaveBar.ModeToggle.graphic.color, Is.EqualTo(palette.Accent));
                Assert.That(panel.SaveBar.SaveButton.colors.normalColor, Is.EqualTo(palette.PanelBackground));
                var selected = panel.Tabs.ActiveButton;
                Assert.That(selected.colors.normalColor, Is.EqualTo(palette.PanelBackground));
                Assert.That(FindImage(selected, "Selection").color, Is.EqualTo(palette.Accent));
                if (panel.Groups != null)
                    Assert.That(panel.Groups.ActiveButton.colors.normalColor, Is.EqualTo(palette.PanelBackground));
            }
        }

        private static Toggle VisibleField(SettingsPanel panel) =>
            panel.GetComponentsInChildren<Toggle>().Single(toggle => toggle != panel.SaveBar.ModeToggle);

        private static Image FindImage(Component container, string objectName) =>
            container.GetComponentsInChildren<Image>(true).Single(image => image.name == objectName);

        private static Button FindButton(Component container, string objectName) =>
            container.GetComponentsInChildren<Button>().Single(button => button.name == objectName);

        private static void Click(Button button)
        {
            var events = EventSystem.current;
            var pointer = new PointerEventData(events)
            {
                button = PointerEventData.InputButton.Left,
                eligibleForClick = true
            };
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        }

        private static void Submit(Selectable control) =>
            ExecuteEvents.Execute(control.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }
}
