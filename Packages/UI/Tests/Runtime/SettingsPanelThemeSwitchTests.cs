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
    /// <summary>Exercises live and persisted theme changes in the delivered settings case through native uGUI events.</summary>
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
        private Image caseBackground;
        private SettingsPanel config;
        private SettingsPanel setting;
        private int writes;
        private int applies;
        private Func<Task> write;
        private Func<bool?> readSystemTheme;

        private string ThemeKey => key + ".Theme";

        [UnitySetUp]
        public IEnumerator MountSettings()
        {
            key = "Yu5h1Lib.Tests.LiveTheme." + Guid.NewGuid();
            writes = applies = 0;
            write = () => Task.CompletedTask;
            readSystemTheme = () => null;
            previousCaseIds = UxUiModuleRegistry.AllCases.Select(item => item.Id).ToArray();
            previousCase = UxUiModuleRegistry.Get(CaseId);
            if (EventSystem.current == null)
                eventSystemObject = new GameObject("Live theme event system", typeof(EventSystem));
            OpenHost();
            yield return null;

            Assert.That(writes, Is.Zero);
            Assert.That(applies, Is.EqualTo(1));
            Assert.That(PlayerPrefs.HasKey(ThemeKey), Is.False, "Default appearance must not create a preference.");
            AssertCasePalette(UguiControlPalette.Dark);
        }

        private void OpenHost(UxUiSettingsDemoCase demo = null)
        {
            hostObject = new GameObject("Live theme settings canvas", typeof(RectTransform), typeof(Canvas));
            host = hostObject.AddComponent<UxUiAcceptanceHost>();
            demo ??= new UxUiSettingsDemoCase(key,
                (_, _, _) => { writes++; return write(); },
                (_, _, _) => { applies++; return Task.CompletedTask; },
                () => readSystemTheme());
            UxUiModuleRegistry.Register(demo, CaseDescription);
            host.SelectCase(CaseId);

            Assert.That(host.CurrentCase, Is.SameAs(demo));
            ReadPanels();
        }

        private void ResetCase()
        {
            var previousDemo = host.CurrentCase;
            host.ResetCurrentCase();
            Assert.That(host.CurrentCase, Is.SameAs(previousDemo));
            ReadPanels();
        }

        private void ReadPanels()
        {
            caseBackground = host.ContentContainer.GetComponentsInChildren<Image>()
                .Single(image => image.name == "SettingsDemo");
            var panels = caseBackground.GetComponentsInChildren<SettingsPanel>(true);
            config = panels.Single(panel => panel.Groups != null);
            setting = panels.Single(panel => panel.Groups == null);
        }

        private void CloseHost()
        {
            if (hostObject == null)
                return;
            hostObject.SetActive(false);
            Object.Destroy(hostObject);
            hostObject = null;
        }

        [UnityTearDown]
        public IEnumerator UnmountSettings()
        {
            CloseHost();
            if (eventSystemObject != null)
                Object.Destroy(eventSystemObject);

            PlayerPrefs.DeleteKey(ThemeKey);
            PlayerPrefs.Save();

            foreach (var id in UxUiModuleRegistry.AllCases.Select(item => item.Id).Except(previousCaseIds).ToArray())
                UxUiModuleRegistry.Unregister(id);
            if (previousCase != null)
                UxUiModuleRegistry.Register(previousCase, CaseDescription);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConfigAndSettingRestoreBothSchemesOnFirstPaintOfAFreshHost()
        {
            Submit(config.Opener);
            Submit(config.SaveBar.ModeToggle);
            var field = VisibleField(config);
            Submit(field);
            Click(FindButton(config.HeaderActions, "Theme switch"));

            AssertPreferenceMode("light");
            AssertDraft(config, field, UguiControlPalette.Light);

            CloseHost();
            yield return null;
            OpenHost();

            AssertCasePalette(UguiControlPalette.Light);
            AssertPreferenceMode("light");
            Submit(setting.Opener);
            Assert.That(VisibleField(setting).isOn, Is.True, "The closed case's draft was never committed.");
            Assert.That(writes, Is.Zero);
            Assert.That(applies, Is.EqualTo(2), "Only the two mounts applied ordinary settings.");

            Submit(FindButton(setting.HeaderActions, "Theme switch"));
            AssertPreferenceMode("dark");
            Assert.That(writes, Is.Zero);
            Assert.That(applies, Is.EqualTo(2));

            CloseHost();
            yield return null;
            OpenHost();

            AssertCasePalette(UguiControlPalette.Dark);
            AssertPreferenceMode("dark");
            Assert.That(writes, Is.Zero);
            Assert.That(applies, Is.EqualTo(3));
            Assert.That(PlayerPrefs.HasKey(key), Is.False);
        }

        [UnityTest]
        public IEnumerator SwitchingThemeLeavesAnAutomaticSavePending()
        {
            var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            write = () => pending.Task;
            try
            {
                Submit(setting.Opener);
                var field = VisibleField(setting);
                Assert.That(setting.SaveBar.ModeToggle.isOn, Is.True);
                Assert.That(field.isOn, Is.True);
                Submit(field);
                Assert.That(writes, Is.EqualTo(1));
                Assert.That(applies, Is.EqualTo(1));
                Assert.That(setting.CanEdit, Is.False);

                Submit(FindButton(setting.HeaderActions, "Theme switch"));

                AssertPreferenceMode("light");
                AssertCasePalette(UguiControlPalette.Light);
                Assert.That(field.isOn, Is.False);
                Assert.That(setting.CanEdit, Is.False);
                Assert.That(writes, Is.EqualTo(1));
                Assert.That(applies, Is.EqualTo(1));
                Assert.That(PlayerPrefs.HasKey(key), Is.False);
                pending.SetResult(true);
                var deadline = Time.realtimeSinceStartup + 5f;
                while (!setting.CanEdit && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(setting.CanEdit, Is.True, "The original settings save completed.");
                Assert.That(writes, Is.EqualTo(1));
                Assert.That(applies, Is.EqualTo(2));
            }
            finally
            {
                pending.TrySetCanceled();
            }
        }

        [TestCase("{\"version\":2,\"mode\":\"light\"}")]
        [TestCase("{\"version\":01,\"mode\":\"light\"}")]
        [TestCase("{\"version\":1,\"mode\":\"light\",\"unexpected\":true}")]
        [TestCase("{\"version\":1,\"mode\":\"custom\",\"custom\":{\"base\":\"light\"}}")]
        public void InvalidPreferenceUsesTheFallbackAndKeepsTheOriginalRecord(string raw)
        {
            PlayerPrefs.SetString(ThemeKey, raw);
            ResetCase();

            AssertCasePalette(UguiControlPalette.Dark);
            Assert.That(PlayerPrefs.GetString(ThemeKey), Is.EqualTo(raw));
            Submit(config.Opener);
            Submit(FindButton(config.HeaderActions, "Theme switch"));

            AssertCasePalette(UguiControlPalette.Light);
            Assert.That(PlayerPrefs.GetString(ThemeKey), Is.EqualTo(raw));
            Assert.That(writes, Is.Zero);
            Assert.That(applies, Is.EqualTo(2));
        }

        [Test]
        public void SystemPreferenceUsesTheStartingPaletteWhenTheSystemValueIsUnavailable()
        {
            const string raw = "{\"version\":1,\"mode\":\"system\"}";
            PlayerPrefs.SetString(ThemeKey, raw);
            ResetCase();
            AssertCasePalette(UguiControlPalette.Dark);
            Assert.That(PlayerPrefs.GetString(ThemeKey), Is.EqualTo(raw));
        }

        [UnityTest]
        public IEnumerator ConfigSelectsSystemAndBothPanelsRestoreItBeforeTheFirstFrame()
        {
            bool isDark = false;
            readSystemTheme = () => isDark;
            Submit(config.Opener);
            Submit(FindButton(config.HeaderActions, "Follow system theme"));
            AssertCasePalette(UguiControlPalette.Light);
            AssertPreferenceMode("system");
            AssertSystemSelection(config, UguiControlPalette.Light, selected: true);
            Assert.That(writes, Is.Zero);
            Assert.That(applies, Is.EqualTo(1));

            CloseHost();
            yield return null;
            isDark = true;
            const string raw = "{ \"mode\": \"system\", \"version\": 1 }";
            PlayerPrefs.SetString(ThemeKey, raw);
            OpenHost();

            AssertCasePalette(UguiControlPalette.Dark);
            Assert.That(PlayerPrefs.GetString(ThemeKey), Is.EqualTo(raw), "Loading must not rewrite the preference.");
            Submit(setting.Opener);
            AssertSystemSelection(setting, UguiControlPalette.Dark, selected: true);
            Submit(FindButton(setting.HeaderActions, "Theme switch"));
            AssertCasePalette(UguiControlPalette.Light);
            AssertPreferenceMode("light");
            AssertSystemSelection(setting, UguiControlPalette.Light, selected: false);
            Submit(FindButton(setting.HeaderActions, "Follow system theme"));
            AssertCasePalette(UguiControlPalette.Dark);
            AssertPreferenceMode("system");
            Assert.That(writes, Is.Zero);
            Assert.That(applies, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator SystemChangesRecolorInPlaceWithoutWritingOrApplyingTheDraft()
        {
            bool isDark = true;
            int reads = 0;
            readSystemTheme = () => { reads++; return isDark; };
            Submit(config.Opener);
            Submit(config.SaveBar.ModeToggle);
            var field = VisibleField(config);
            Submit(field);
            Submit(FindButton(config.Tabs, "details"));
            var tab = config.Tabs.ActiveButton;
            var group = config.Groups.ActiveButton;
            Click(FindButton(config.HeaderActions, "Follow system theme"));
            tab.Select();

            const string raw = "{ \"mode\": \"system\", \"version\": 1 }";
            PlayerPrefs.SetString(ThemeKey, raw);
            var timeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                foreach (var nextPalette in new[] { UguiControlPalette.Light, UguiControlPalette.Dark })
                {
                    isDark = nextPalette == UguiControlPalette.Dark;
                    var deadline = Time.realtimeSinceStartup + 3f;
                    while (caseBackground.color != nextPalette.PageBackground && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    AssertCasePalette(nextPalette);
                    Assert.That(config.IsOpen, Is.True);
                    Assert.That(config.Tabs.ActiveButton, Is.SameAs(tab));
                    Assert.That(config.Tabs.SelectedId, Is.EqualTo("details"));
                    Assert.That(config.Groups.ActiveButton, Is.SameAs(group));
                    Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(tab.gameObject));
                    AssertDraft(config, field, nextPalette);
                    AssertSystemSelection(config, nextPalette, selected: true);
                    Assert.That(PlayerPrefs.GetString(ThemeKey), Is.EqualTo(raw), "A system signal must not write preferences.");
                }

                Submit(FindButton(config.HeaderActions, "Theme switch"));
                AssertPreferenceMode("light");
                var readsInFixedMode = reads;
                yield return new WaitForSecondsRealtime(0.45f);
                AssertCasePalette(UguiControlPalette.Light);
                Assert.That(reads, Is.EqualTo(readsInFixedMode), "Fixed theme should stop observing the system.");

                Submit(FindButton(config.HeaderActions, "Follow system theme"));
                AssertPreferenceMode("system");
                CloseHost();
                var readsAfterClosing = reads;
                yield return new WaitForSecondsRealtime(0.45f);
                Assert.That(reads, Is.EqualTo(readsAfterClosing), "Closing the case should stop observing the system.");
                Assert.That(writes, Is.Zero);
                Assert.That(applies, Is.EqualTo(1));
            }
            finally
            {
                Time.timeScale = timeScale;
            }
        }

        [Test]
        public void ChangingModePreservesAllEightCustomColors()
        {
            var colors = new[]
            {
                (Name: "pageBackground", Hex: "#AABBCC"), (Name: "panelBackground", Hex: "#CCDDEE"),
                (Name: "heading", Hex: "#EEFF00"), (Name: "panelHeading", Hex: "#AA11BB"),
                (Name: "itemText", Hex: "#BB22CC"), (Name: "secondaryText", Hex: "#CC33DD"),
                (Name: "border", Hex: "#DD44EE"), (Name: "accent", Hex: "#EE55FF")
            };
            var custom = string.Join(",", colors.Select(color => $"\"{color.Name}\":\"{color.Hex}\""));
            var raw = "{\"version\":1,\"mode\":\"custom\",\"custom\":{\"base\":\"light\"," + custom + "}}";
            PlayerPrefs.SetString(ThemeKey, raw);
            ResetCase();

            AssertCasePalette(UguiControlPalette.Light);
            Assert.That(PlayerPrefs.GetString(ThemeKey), Is.EqualTo(raw));
            Submit(setting.Opener);
            Submit(FindButton(setting.HeaderActions, "Theme switch"));

            var saved = PlayerPrefs.GetString(ThemeKey);
            Assert.That(saved, Does.Match("\"mode\"\\s*:\\s*\"dark\""));
            Assert.That(saved, Does.Match("\"base\"\\s*:\\s*\"light\""));
            foreach (var color in colors)
                Assert.That(saved, Does.Match($"\"{color.Name}\"\\s*:\\s*\"{color.Hex.ToLowerInvariant()}\""));

            ResetCase();
            AssertCasePalette(UguiControlPalette.Dark);
            Assert.That(PlayerPrefs.GetString(ThemeKey), Is.EqualTo(saved));
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

        private void AssertPreferenceMode(string mode)
        {
            var version = "\"version\"\\s*:\\s*1";
            var choice = $"\"mode\"\\s*:\\s*\"{mode}\"";
            var raw = PlayerPrefs.GetString(ThemeKey);
            Assert.That(raw, Does.Match($"^\\s*\\{{\\s*(?:{version}\\s*,\\s*{choice}|{choice}\\s*,\\s*{version})\\s*\\}}\\s*$"));
        }

        private static void AssertSystemSelection(SettingsPanel panel, UguiControlPalette palette, bool selected)
        {
            var button = FindButton(panel.HeaderActions, "Follow system theme");
            var label = button.GetComponentInChildren<Text>();
            Assert.That(label.text, Is.EqualTo("跟隨系統"));
            Assert.That(button.colors.normalColor, Is.EqualTo(selected ? palette.Accent : palette.PanelBackground));
        }

        private void AssertCasePalette(UguiControlPalette palette)
        {
            Assert.That(caseBackground.color, Is.EqualTo(palette.PageBackground));
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
