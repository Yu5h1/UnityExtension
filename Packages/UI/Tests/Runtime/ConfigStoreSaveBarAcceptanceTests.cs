using System;
using System.Collections;
using System.IO;
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
    /// <summary>Drives the delivered Config demo and injects failures at its persistence and apply callbacks.</summary>
    public sealed class ConfigStoreSaveBarAcceptanceTests
    {
        private string key;
        private GameObject root;
        private GameObject eventSystem;
        private UxUiSettingsDemoCase demo;
        private SettingsPanel config;
        private Text preview;
        private Toggle hints;
        private int writes;
        private int applies;
        private Func<Task> write;
        private Func<Task> apply;

        [UnitySetUp]
        public IEnumerator OpenConfig()
        {
            key = "Yu5h1Lib.Tests.ConfigStoreSaveBar." + Guid.NewGuid();
            writes = applies = 0;
            write = apply = () => Task.CompletedTask;
            root = new GameObject("Settings acceptance canvas", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            eventSystem = new GameObject("Settings acceptance event system", typeof(EventSystem));
            demo = new UxUiSettingsDemoCase(key, async (scope, data, token) =>
            {
                writes++;
                await write();
                token.ThrowIfCancellationRequested();
                Preferences.DefaultStorage.Save(scope, data);
            }, (_, _, token) =>
            {
                token.ThrowIfCancellationRequested();
                applies++;
                return apply();
            });
            demo.Mount((RectTransform)root.transform);
            yield return null;
            config = root.GetComponentsInChildren<SettingsPanel>(true).Single(panel => panel.Groups != null);
            preview = root.GetComponentsInChildren<Text>().Single(text => text.name == "Applied");
            Submit(config.Opener);
            hints = config.GetComponentsInChildren<Toggle>().Single(toggle => toggle != config.SaveBar.ModeToggle);
            Assert.That(writes, Is.Zero);
            Assert.That(applies, Is.EqualTo(1));
            Assert.That(hints.isOn, Is.True);
        }

        [TearDown]
        public void CleanUp()
        {
            demo?.Unmount();
            Object.Destroy(root);
            Object.Destroy(eventSystem);
            if (key != null)
                Preferences.DefaultStorage.Delete(key);
        }

        [UnityTest]
        public IEnumerator AutosaveCommitsBeforeApplying()
        {
            Assert.That(config.SaveBar.ModeToggle.isOn, Is.True);
            Assert.That(config.SaveBar.SaveButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(config.SaveBar.SaveButton.IsInteractable(), Is.False);
            var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            write = () => pending.Task;
            try
            {
                Submit(hints);
                Assert.That(writes, Is.EqualTo(1));
                Assert.That(applies, Is.EqualTo(1));
                Assert.That(PlayerPrefs.HasKey(key), Is.False);
                Assert.That(preview.text, Does.Contain("提示 開"));
                Assert.That(config.Close(), Is.False);
                Assert.That(config.SaveBar.SaveButton.IsInteractable(), Is.False);
                pending.SetResult(true);
                yield return WaitFor(() => config.CanEdit);
                AssertApplied(false, 1, 2);
            }
            finally
            {
                pending.TrySetCanceled();
            }
        }

        [Test]
        public void ManualApplyPersistsAndCloseDiscardsOnlyTheUnsubmittedChange()
        {
            Submit(config.SaveBar.ModeToggle);
            Assert.That(config.SaveBar.ModeToggle.isOn, Is.False);
            Submit(hints);
            Assert.That(hints.isOn, Is.False);
            Assert.That(writes, Is.Zero);
            Assert.That(applies, Is.EqualTo(1));
            Assert.That(PlayerPrefs.HasKey(key), Is.False);
            Assert.That(config.Close(), Is.True);
            Submit(config.Opener);
            Assert.That(hints.isOn, Is.True);
            Assert.That(writes, Is.Zero);

            Submit(hints);
            Assert.That(writes, Is.Zero);
            Submit(config.SaveBar.SaveButton);
            AssertApplied(false, 1, 2);
            Submit(hints);
            Assert.That(hints.isOn, Is.True);
            Assert.That(config.Close(), Is.True);
            Submit(config.Opener);
            AssertApplied(false, 1, 2);
        }

        [Test]
        public void SaveFailureKeepsTheDraftAfterReopenAndRetryWritesThenApplies()
        {
            write = () => Task.FromException(new IOException("Injected write failure"));
            Submit(hints);
            Assert.That(writes, Is.EqualTo(1));
            Assert.That(applies, Is.EqualTo(1));
            Assert.That(PlayerPrefs.HasKey(key), Is.False);
            Assert.That(preview.text, Does.Contain("提示 開"));
            Assert.That(hints.isOn, Is.False);
            Assert.That(config.Close(), Is.True);
            Submit(config.Opener);
            Assert.That(hints.isOn, Is.False);
            AssertRetry("重試儲存", "保存失敗。");

            write = () => Task.CompletedTask;
            Submit(config.SaveBar.SaveButton);
            AssertApplied(false, 2, 2);
            Assert.That(config.SaveBar.SaveButton.IsInteractable(), Is.False);
        }

        [Test]
        public void ApplyFailureKeepsTheSavedValueAndRetryDoesNotWriteAgain()
        {
            apply = () => Task.FromException(new InvalidOperationException("Injected apply failure"));
            Submit(hints);
            Assert.That(writes, Is.EqualTo(1));
            Assert.That(applies, Is.EqualTo(2));
            AssertStored(false);
            Assert.That(preview.text, Does.Contain("提示 開"));
            Assert.That(config.CanEdit, Is.False);
            Assert.That(config.SaveBar.ModeToggle.IsInteractable(), Is.False);
            Assert.That(config.Close(), Is.True);
            Submit(config.Opener);
            Assert.That(hints.isOn, Is.False);
            AssertRetry("重試套用", "已保存，尚未套用。");

            apply = () => Task.CompletedTask;
            Submit(config.SaveBar.SaveButton);
            AssertApplied(false, 1, 3);
            Assert.That(config.SaveBar.SaveButton.IsInteractable(), Is.False);
        }

        private void AssertApplied(bool value, int expectedWrites, int expectedApplies)
        {
            Assert.That(writes, Is.EqualTo(expectedWrites));
            Assert.That(applies, Is.EqualTo(expectedApplies));
            Assert.That(hints.isOn, Is.EqualTo(value));
            Assert.That(config.CanEdit, Is.True);
            Assert.That(preview.text, Does.Contain(value ? "提示 開" : "提示 關"));
            Assert.That(config.SaveBar.GetComponentsInChildren<Text>().Any(label => label.name == "Status"), Is.False);
            AssertStored(value);
        }

        private void AssertStored(bool value)
        {
            var result = Preferences.DefaultStorage.Load(key);
            Assert.That(result.Status, Is.EqualTo(PreferencesLoadStatus.Loaded));
            Assert.That(bool.Parse(result.Data["hints"]), Is.EqualTo(value));
        }

        private void AssertRetry(string label, string message)
        {
            Assert.That(config.SaveBar.SaveButton.IsInteractable(), Is.True);
            Assert.That(config.SaveBar.SaveButton.GetComponentInChildren<Text>().text, Is.EqualTo(label));
            Assert.That(config.SaveBar.GetComponentsInChildren<Text>().Any(text => text.text == message), Is.True);
        }

        private static IEnumerator WaitFor(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 5;
            while (!condition() && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(condition(), Is.True, "The asynchronous save completed.");
        }

        private static void Submit(Selectable target) =>
            ExecuteEvents.Execute(target.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }
}
