using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    /// Real backend (CFG-P01): the unified entry on the real <see cref="PlayerPrefs"/> through
    /// <see cref="Preferences.PlayerPrefsStorage"/>. <see cref="Preferences.Unload"/> stands in for a restart.
    /// The test keys are backed up and restored, so a Unity project's own saves are not touched.
    /// </summary>
    public class PreferencesBackendTests
    {
        private const string KeyA = "Yu5h1Lib.Tests.Backend.A";
        private const string KeyB = "Yu5h1Lib.Tests.Backend.B";
        private const string KeyNone = "Yu5h1Lib.Tests.Backend.None";

        private readonly Dictionary<string, string> backup = new Dictionary<string, string>();

        [SetUp]
        public void BackUp()
        {
            backup.Clear();
            foreach (var key in new[] { KeyA, KeyB, KeyNone })
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
            foreach (var key in new[] { KeyA, KeyB, KeyNone })
            {
                Preferences.ClearLoadError(key);
                Preferences.SetSaveOnChanged(key, true);
                Preferences.Unload(key);
                PlayerPrefs.DeleteKey(key);
                if (backup.TryGetValue(key, out var value))
                    PlayerPrefs.SetString(key, value);
            }
            PlayerPrefs.Save();
        }

        private static string Count(string key)
        {
            var view = Preferences.GetView(key);
            return view.TryGetValue("count", out var value) ? value : null;
        }

        [Test]
        public void SaveRestartReloadAndKeyIsolation()
        {
            Preferences.GetView(KeyA)["count"] = "4";
            Assert.IsTrue(PlayerPrefs.HasKey(KeyA), "the change must reach PlayerPrefs");
            StringAssert.Contains("\"$version\"", PlayerPrefs.GetString(KeyA));
            Assert.IsFalse(PlayerPrefs.HasKey(KeyB), "writing A must not create B");

            Assert.IsTrue(Preferences.Unload(KeyA));
            Assert.AreEqual("4", Count(KeyA), "a restart must read back what was saved");

            Preferences.GetView(KeyB)["count"] = "7";
            Preferences.GetView(KeyA)["count"] = "5";
            Preferences.Unload(KeyA);
            Preferences.Unload(KeyB);
            Assert.AreEqual("5", Count(KeyA));
            Assert.AreEqual("7", Count(KeyB));
        }

        [Test]
        public void NoSaveAndAnUnreadableSaveCanBeToldApart()
        {
            Assert.IsNull(Count(KeyNone));
            Assert.IsNull(Preferences.GetLoadError(KeyNone));
            Assert.IsFalse(PlayerPrefs.HasKey(KeyNone), "reading a missing KEY must not create it");

            PlayerPrefs.SetString(KeyA, "{oops");
            Preferences.Unload(KeyA);
            LogAssert.Expect(LogType.Error, new Regex(@"Preferences \[" + Regex.Escape(KeyA) + @"\]: the save cannot be read"));
            Assert.IsNull(Count(KeyA));
            Assert.IsNotNull(Preferences.GetLoadError(KeyA));
        }

        [Test]
        public void AnUnreadableSaveIsKeptUntilFixedOrExplicitlyReset()
        {
            PlayerPrefs.SetString(KeyA, "{oops");
            LogAssert.Expect(LogType.Error, new Regex(@"Preferences \[" + Regex.Escape(KeyA) + @"\]: the save cannot be read"));
            Preferences.GetView(KeyA)["count"] = "9";

            Assert.AreEqual("{oops", PlayerPrefs.GetString(KeyA), "the original text must stay");

            PlayerPrefs.SetString(KeyA, "{\"_entries\":[{\"key\":\"count\",\"value\":\"3\"}]}");
            Assert.IsTrue(Preferences.Reload(KeyA));
            Assert.AreEqual("3", Count(KeyA));
            Preferences.GetView(KeyA)["count"] = "4";
            Assert.AreEqual("4", Preferences.GetView(KeyA)["count"]);
            StringAssert.Contains("\"$version\"", PlayerPrefs.GetString(KeyA));

            PlayerPrefs.SetString(KeyB, "null");
            LogAssert.Expect(LogType.Error, new Regex(@"Preferences \[" + Regex.Escape(KeyB) + @"\]: the save cannot be read"));
            Preferences.GetView(KeyB);
            Preferences.Delete(KeyB);
            Assert.IsFalse(PlayerPrefs.HasKey(KeyB));
            Assert.IsNull(Preferences.GetLoadError(KeyB));
        }
    }
}
