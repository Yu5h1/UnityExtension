using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    /// Configuration level 1 through a real scene host <see cref="Preferences{T}"/> on the real PlayerPrefs: defaults
    /// merged over a save (CFG-01), a save that cannot be read (CFG-02), key isolation (CFG-08) and a destroyed host
    /// no longer being called by the entry (CFG-07, the native equivalent on synchronous storage). The test keys are
    /// backed up and restored.
    /// </summary>
    public class PreferencesHostTests
    {
        private class TestHost : Preferences<TestHost>
        {
            public string key;
            public int saves;
            public override string KEY => key;

            protected override void Start() { }

            public void SetDefaults(params string[] pairs)
            {
                var defaults = new DataView();
                for (int i = 0; i < pairs.Length; i += 2)
                    defaults[pairs[i]] = pairs[i + 1];
                defaultSetting = defaults;
            }

            public override void SaveToPlayerPrefs()
            {
                saves++;
                base.SaveToPlayerPrefs();
            }
        }

        private const string KeyA = "Yu5h1Lib.Tests.Host.A";
        private const string KeyB = "Yu5h1Lib.Tests.Host.B";

        private readonly Dictionary<string, string> backup = new Dictionary<string, string>();
        private readonly List<GameObject> objects = new List<GameObject>();

        [SetUp]
        public void BackUp()
        {
            backup.Clear();
            foreach (var key in new[] { KeyA, KeyB })
            {
                if (PlayerPrefs.HasKey(key))
                    backup[key] = PlayerPrefs.GetString(key);
                PlayerPrefs.DeleteKey(key);
                Preferences.Unload(key);
            }
        }

        [TearDown]
        public void Restore()
        {
            foreach (var go in objects)
                if (go != null)
                    Object.DestroyImmediate(go);
            objects.Clear();
            foreach (var key in new[] { KeyA, KeyB })
            {
                Preferences.ClearLoadError(key);
                Preferences.SetSaveOnChanged(key, true);
                Preferences.Unload(key);
                Preferences.Unregister(key, Preferences.GetStorage(key));
                PlayerPrefs.DeleteKey(key);
                if (backup.TryGetValue(key, out var value))
                    PlayerPrefs.SetString(key, value);
            }
            PlayerPrefs.Save();
        }

        private TestHost NewHost(string key, params string[] defaults)
        {
            var go = new GameObject("PreferencesHostTests " + key);
            objects.Add(go);
            var host = go.AddComponent<TestHost>();
            host.key = key;
            host.SetDefaults(defaults);
            return host;
        }

        private static void SaveRaw(string key, params string[] pairs)
        {
            var data = new DataView();
            for (int i = 0; i < pairs.Length; i += 2)
                data[pairs[i]] = pairs[i + 1];
            PlayerPrefs.SetString(key, PreferencesFormat.ToJson(data));
        }

        private static void ExpectLoadError(string key)
            => LogAssert.Expect(LogType.Error, new Regex(@"Preferences \[" + Regex.Escape(key) + @"\]: the save cannot be read"));

        // ---- CFG-01 ----

        [UnityTest]
        public IEnumerator NoSaveUsesTheDefaultsAndWritesNothing()
        {
            var host = NewHost(KeyA, "count", "1", "mode", "fast");
            yield return null;

            Assert.AreEqual("1", host.current["count"]);
            Assert.AreEqual("fast", host.current["mode"]);
            Assert.AreEqual(0, host.saves);
            Assert.IsFalse(PlayerPrefs.HasKey(KeyA), "defaults alone must not create a save");
            Assert.IsNull(host.LoadError);
        }

        [UnityTest]
        public IEnumerator ASaveIsRestoredAndAMissingFieldGetsItsDefaultWrittenOnce()
        {
            SaveRaw(KeyA, "count", "4");
            var host = NewHost(KeyA, "count", "1", "mode", "fast");
            yield return null;

            Assert.AreEqual("4", host.current["count"], "a saved value wins over the default");
            Assert.AreEqual("fast", host.current["mode"], "a field the save lacks takes its default");
            Assert.AreEqual(1, host.saves, "the default migration writes exactly once");
            var saved = PreferencesFormat.Parse(PlayerPrefs.GetString(KeyA));
            Assert.AreEqual("fast", saved.Data["mode"]);
            Assert.AreEqual("4", saved.Data["count"]);
        }

        // ---- CFG-02 ----

        [UnityTest]
        public IEnumerator AnUnreadableSaveRunsOnDefaultsAndIsNeverOverwritten()
        {
            PlayerPrefs.SetString(KeyA, "{oops");
            ExpectLoadError(KeyA);
            var host = NewHost(KeyA, "count", "1");
            yield return null;

            Assert.AreEqual("1", host.current["count"], "the view runs on the host defaults");
            Assert.IsNotNull(host.LoadError);

            host.current["count"] = "9";
            Preferences.Save(KeyA);
            Assert.AreEqual(0, host.saves);
            Assert.AreEqual("{oops", PlayerPrefs.GetString(KeyA));

            SaveRaw(KeyA, "count", "4");
            Assert.IsTrue(host.Reload());
            Assert.IsNull(host.LoadError);
            Assert.AreEqual("4", host.current["count"]);
            Assert.AreEqual(0, host.saves, "reloading must not write");

            host.current["count"] = "5";
            Assert.AreEqual(1, host.saves);
            Assert.AreEqual("5", PreferencesFormat.Parse(PlayerPrefs.GetString(KeyA)).Data["count"]);
        }

        [UnityTest]
        public IEnumerator ReloadKeepsTheHostDefaultsForFieldsTheFixedSaveLacks()
        {
            PlayerPrefs.SetString(KeyA, "null");
            ExpectLoadError(KeyA);
            var host = NewHost(KeyA, "count", "1", "mode", "fast");
            yield return null;
            Assert.IsNotNull(host.LoadError);

            SaveRaw(KeyA, "count", "4");
            Assert.IsTrue(host.Reload());

            Assert.AreEqual("4", host.current["count"]);
            Assert.AreEqual("fast", host.current["mode"]);
        }

        [UnityTest]
        public IEnumerator ResetToDefaultsIsAnExplicitResetThatReplacesTheUnreadableSave()
        {
            PlayerPrefs.SetString(KeyA, "{oops");
            ExpectLoadError(KeyA);
            var host = NewHost(KeyA, "count", "1");
            yield return null;
            Assert.IsNotNull(host.LoadError);

            host.ResetToDefaults();

            Assert.IsNull(host.LoadError);
            Assert.AreEqual(1, host.saves);
            var saved = PreferencesFormat.Parse(PlayerPrefs.GetString(KeyA));
            Assert.AreEqual(PreferencesLoadStatus.Loaded, saved.Status, saved.Error);
            Assert.AreEqual("1", saved.Data["count"]);
        }

        [UnityTest]
        public IEnumerator ADeletedSaveLiftsTheErrorAndTheKeyUsesDefaultsAgain()
        {
            PlayerPrefs.SetString(KeyA, "{oops");
            ExpectLoadError(KeyA);
            var host = NewHost(KeyA, "count", "1");
            yield return null;
            Assert.IsNotNull(host.LoadError);

            Preferences.Delete(KeyA);

            Assert.IsFalse(PlayerPrefs.HasKey(KeyA));
            Assert.IsNull(host.LoadError);
            host.current["count"] = "2";
            Assert.AreEqual(1, host.saves);
        }

        // ---- CFG-08 ----

        [UnityTest]
        public IEnumerator TwoHostsWithDifferentKeysDoNotOverwriteEachOther()
        {
            SaveRaw(KeyB, "count", "7");
            var a = NewHost(KeyA, "count", "1");
            var b = NewHost(KeyB, "count", "1");
            yield return null;
            var rawB = PlayerPrefs.GetString(KeyB);

            a.current["count"] = "2";

            Assert.AreEqual(1, a.saves);
            Assert.AreEqual(0, b.saves);
            Assert.AreEqual(rawB, PlayerPrefs.GetString(KeyB));
            Assert.AreEqual("7", b.current["count"]);
        }

        [UnityTest]
        public IEnumerator HostChangedFiresOncePerChangeAndStopsAfterUnsubscribe()
        {
            var host = NewHost(KeyA, "count", "1");
            yield return null;
            var view = host.current;
            int first = 0, second = 0;
            UnityEngine.Events.UnityAction h1 = () => first++;
            UnityEngine.Events.UnityAction h2 = () => second++;
            host.changed += h1;
            host.changed += h2;

            view["count"] = "2";
            Assert.AreEqual((1, 1), (first, second));
            Assert.AreEqual(1, host.saves);

            host.changed -= h2;
            view["count"] = "3";
            Assert.AreEqual((2, 1), (first, second));
            Assert.AreEqual(2, host.saves);
        }

        // ---- CFG-07 ----

        [UnityTest]
        public IEnumerator ADestroyedHostIsNoLongerCalledByTheEntry()
        {
            var host = NewHost(KeyA, "count", "1");
            yield return null;
            host.current["count"] = "2";
            Assert.AreEqual(1, host.saves);
            Assert.AreNotSame(Preferences.DefaultStorage, Preferences.GetStorage(KeyA), "a live host owns its KEY's storage");

            Object.Destroy(host.gameObject);
            yield return null;

            Assert.IsTrue(host == null);
            Assert.AreSame(Preferences.DefaultStorage, Preferences.GetStorage(KeyA), "a destroyed host must log out");

            var view = Preferences.GetView(KeyA);
            view["count"] = "3";
            Assert.AreEqual("3", PreferencesFormat.Parse(PlayerPrefs.GetString(KeyA)).Data["count"],
                "the change still reaches PlayerPrefs through the default storage");
        }

        [UnityTest]
        public IEnumerator ANewHostTakesOverAKeyAfterTheOldOneIsDestroyed()
        {
            var first = NewHost(KeyA, "count", "1");
            yield return null;
            first.current["count"] = "2";
            Object.Destroy(first.gameObject);
            yield return null;

            var second = NewHost(KeyA, "count", "1");
            yield return null;

            Assert.AreEqual("2", second.current["count"]);
            second.current["count"] = "3";
            Assert.AreEqual(1, second.saves);
            Assert.AreEqual("3", PreferencesFormat.Parse(PlayerPrefs.GetString(KeyA)).Data["count"]);
        }

        [UnityTest]
        public IEnumerator UnloadMakesAHostReadItsKeyAgain()
        {
            var host = NewHost(KeyA, "count", "1");
            yield return null;
            host.current["count"] = "2";
            var before = host.current;

            Assert.IsTrue(Preferences.Unload(KeyA));
            SaveRaw(KeyA, "count", "5");
            var after = host.current;

            Assert.AreNotSame(before, after);
            Assert.AreEqual("5", after["count"]);
        }
    }
}
