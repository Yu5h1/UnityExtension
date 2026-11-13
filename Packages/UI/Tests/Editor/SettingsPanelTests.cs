using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib.UI.Acceptance;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.UI.Tests
{
    public sealed class SettingsPanelTests
    {
        private GameObject root;

        [SetUp]
        public void CreateRoot() => root = new GameObject("Settings Tests", typeof(RectTransform), typeof(Canvas));

        [TearDown]
        public void DestroyRoot() => Object.DestroyImmediate(root);

        [Test]
        public void SettingsPanelRoutesGroupsAndKeepsTheSelectedPageAndFields()
        {
            var panel = SettingsPanel.Create((RectTransform)root.transform, SettingsPanel.Presentation.Config);
            var draft = MakePage(root.transform, "draft");
            var first = new SettingsPanel.Group("first", "First", new[]
            {
                new Tabs.Item("general", "General"), new Tabs.Item("details", "Details", draft)
            });
            var second = new SettingsPanel.Group("second", "Second", new[]
            {
                new Tabs.Item("general", "General", MakePage(root.transform, "second-general"))
            });
            panel.SetGroups(new[] { first, second });
            Assert.That(panel.Show(), Is.True);
            Assert.That(panel.Tabs.Navigate(MoveDirection.Left), Is.True);
            Assert.That(panel.Tabs.SelectedId, Is.EqualTo("details"));
            Assert.That(draft.gameObject.activeInHierarchy, Is.True);
            panel.Tabs.Orientation = Tabs.Direction.Column;
            Assert.That(panel.Tabs.Navigate(MoveDirection.Right), Is.False);
            Assert.That(panel.Tabs.Navigate(MoveDirection.Down), Is.True);
            Assert.That(panel.Tabs.SelectedId, Is.EqualTo("general"));
            Assert.That(panel.Tabs.FocusEdge(true), Is.True);
            Assert.That(panel.Groups.Select("second"), Is.True);
            Assert.That(panel.ActiveGroup, Is.EqualTo("second"));
            Assert.That(panel.Tabs.SelectedId, Is.EqualTo("general"));
            Assert.That(draft.gameObject.activeSelf, Is.False);
            panel.Groups.Select("first");
            Assert.That(panel.Tabs.SelectedId, Is.EqualTo("details"));
            Assert.That(draft.gameObject.activeInHierarchy, Is.True);
            Assert.Throws<ArgumentException>(() => panel.SetGroups(new[] { first, first }));
            Assert.That(panel.Tabs.SelectedId, Is.EqualTo("details"));
        }

        [Test]
        public void SettingsPanelValidatesAndRoutesSaveRequests()
        {
            var panel = SettingsPanel.Create((RectTransform)root.transform, SettingsPanel.Presentation.Setting);
            panel.SetGroups(new[] { new SettingsPanel.Group("first", "First", new[] { new Tabs.Item("general", "General") }) });
            panel.SetState(new SaveBar.State(autoSave: true));
            Assert.That(panel.Show(), Is.True);
            Assert.That(panel.Groups, Is.Null);
            Assert.That(panel.ActiveGroup, Is.EqualTo("first"));
            var modes = new List<bool>();
            var saves = 0;
            var retries = 0;
            panel.SaveBar.AutoSaveRequested += modes.Add;
            panel.SaveBar.SaveRequested += () => saves++;
            panel.SaveBar.RetryApplyRequested += () => retries++;
            Assert.That(panel.SaveBar.SaveButton.interactable, Is.False);
            panel.SaveBar.ModeToggle.isOn = false;
            Assert.That(modes, Is.EqualTo(new[] { false }));
            Assert.That(panel.SaveBar.ModeToggle.isOn, Is.True);
            panel.SetState(new SaveBar.State(false, busy: true));
            panel.SaveBar.SaveButton.onClick.Invoke();
            Assert.That(saves, Is.Zero);
            Assert.That(panel.Tabs.FocusEdge(true), Is.False);
            Assert.That(panel.Close(), Is.False);
            panel.SetState(new SaveBar.State(false, retrySave: true));
            panel.SaveBar.SaveButton.onClick.Invoke();
            Assert.That(saves, Is.EqualTo(1));
            panel.SetState(new SaveBar.State(false, retryApply: true));
            Assert.That(panel.CanEdit, Is.False);
            Assert.That(panel.SaveBar.ModeToggle.interactable, Is.False);
            panel.SaveBar.SaveButton.onClick.Invoke();
            Assert.That(retries, Is.EqualTo(1));
            Assert.That(saves, Is.EqualTo(1));
            Assert.That(panel.Close(), Is.True);
        }

        [Test]
        public void SettingsPanelAcceptanceCaseUsesTheDeliveredComponents()
        {
            var demo = new UxUiSettingsDemoCase("Yu5h1Lib.Tests.SettingsPanel." + Guid.NewGuid());
            try
            {
                demo.Mount((RectTransform)root.transform);
                var panels = root.GetComponentsInChildren<SettingsPanel>(true);
                Assert.That(panels.Length, Is.EqualTo(2));
                Assert.That(root.GetComponentsInChildren<GroupList>(true).Length, Is.EqualTo(1));
                foreach (var panel in panels)
                {
                    Assert.That(panel.Show(), Is.True);
                    Assert.That(panel.Tabs.SelectedId, Is.EqualTo("general"));
                    Assert.That(panel.SaveBar.gameObject.GetComponentInChildren<Toggle>().isOn, Is.True);
                    panel.Close();
                }
            }
            finally { demo.Unmount(); }
        }

        [TestCase("SettingsPanel", "SettingsPanel.md", "a87560ce27b8190cb4e7f584a6565283c29f4ef9b382ae451ab266785697690e")]
        [TestCase("SettingsPanel", "settings-design.json", "5f2f3a57e34b7c8134eef7c78d2c9c5e2add59962aeff1fa8437967d60bdeff1")]
        [TestCase("SettingsPanel", "settings-cases.json", "54d0f3aa87a1d6a5b6966ddf787f273fc3b1987f4dd0983f121058a1412d0f7b")]
        [TestCase("Configuration", "Configuration.md", "1b4928d75c16b7610be7e377b8d2aafed845fca42f1475866baf4eea0a7234b8")]
        public void SettingsPanelPinnedFilesMatchTheUxManifest(string folder, string file, string expected)
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.yu5h1.ui/package.json");
            var bytes = File.ReadAllBytes(Path.Combine(package.resolvedPath, "Tests", "Editor", "UX~", folder, file));
            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            Assert.That(actual, Is.EqualTo(expected));
        }

        private static RectTransform MakePage(Transform parent, string name)
        {
            var page = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            page.SetParent(parent);
            return page;
        }
    }
}
