using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    /// Configuration level 1 through the real unified entry <see cref="Preferences"/> with an in-memory storage that
    /// counts reads and writes: default and load (CFG-01), load without overwrite (CFG-02, UX decision 91: every KEY),
    /// notification and isolation (CFG-08) and the host-less half of unload (CFG-07).
    /// </summary>
    public class PreferencesEntryTests
    {
        private class MemoryStorage : Preferences.IStatusStorage
        {
            public string raw;
            public int reads, writes;
            public bool throwOnRead;

            public PreferencesLoadResult Load(string key)
            {
                reads++;
                if (throwOnRead)
                    throw new IOException("disk unavailable");
                return raw == null ? PreferencesLoadResult.Missing : PreferencesFormat.Parse(raw);
            }

            public bool TryLoad(string key, out DataView data)
            {
                var result = Load(key);
                data = result.Status == PreferencesLoadStatus.Loaded ? result.Data : null;
                return data != null;
            }

            public void Save(string key, DataView data)
            {
                writes++;
                raw = PreferencesFormat.ToJson(data);
            }

            public void Delete(string key) => raw = null;
        }

        /// <summary>Knows nothing of load errors: a false TryLoad is all it can say.</summary>
        private class PlainStorage : Preferences.IStorage
        {
            public bool TryLoad(string key, out DataView data) { data = null; return false; }
            public void Save(string key, DataView data) { }
            public void Delete(string key) { }
        }

        /// <summary>Hands out the record object it keeps, as a host override holding its own records does (VCP
        /// UserDataView), and writes a save back into that record.</summary>
        private class SharedObjectStorage : Preferences.IStatusStorage
        {
            public readonly DataView record = new DataView { { "Email", "a@b" }, { "Name", "A" } };

            public PreferencesLoadResult Load(string key) => PreferencesLoadResult.Loaded(record);
            public bool TryLoad(string key, out DataView data) { data = record; return true; }
            public void Save(string key, DataView data) => record.CopyFrom(data);
            public void Delete(string key) { }
        }

        private readonly Dictionary<string, Preferences.IStorage> registered = new Dictionary<string, Preferences.IStorage>();

        private (string key, MemoryStorage storage) NewKey(string name, string raw = null)
        {
            var key = "Yu5h1Lib.Tests.Entry." + name;
            var storage = new MemoryStorage { raw = raw };
            Preferences.Register(key, storage);
            registered[key] = storage;
            return (key, storage);
        }

        [TearDown]
        public void ReleaseKeys()
        {
            foreach (var pair in registered)
            {
                Preferences.SetSaveOnChanged(pair.Key, true);
                Preferences.ClearLoadError(pair.Key);
                Preferences.Unload(pair.Key);
                Preferences.Unregister(pair.Key, pair.Value);
            }
            registered.Clear();
        }

        private static string Saved(params string[] pairs)
        {
            var data = new DataView();
            for (int i = 0; i < pairs.Length; i += 2)
                data[pairs[i]] = pairs[i + 1];
            return PreferencesFormat.ToJson(data);
        }

        /// <summary>Writes through the shared view, as a bound control does.</summary>
        private static void Put(string key, string field, string value) => Preferences.GetView(key)[field] = value;

        private static void ExpectLoadError(string key)
            => LogAssert.Expect(LogType.Error, new Regex(@"Preferences \[" + Regex.Escape(key) + @"\]: the save cannot be read"));

        // ---- CFG-01 ----

        [Test]
        public void NoSaveIsNotAnErrorAndReadsOnceWithoutWriting()
        {
            var (key, storage) = NewKey(nameof(NoSaveIsNotAnErrorAndReadsOnceWithoutWriting));
            var view = Preferences.GetView(key, out bool loaded);
            Preferences.GetView(key);

            Assert.IsFalse(loaded);
            Assert.AreEqual(0, view.Count);
            Assert.IsNull(Preferences.GetLoadError(key));
            Assert.AreEqual(1, storage.reads);
            Assert.AreEqual(0, storage.writes);
        }

        [Test]
        public void ASavedViewIsRestored()
        {
            var (key, storage) = NewKey(nameof(ASavedViewIsRestored), Saved("count", "4", "name", "x"));
            var view = Preferences.GetView(key, out bool loaded);

            Assert.IsTrue(loaded);
            Assert.AreEqual("4", view["count"]);
            Assert.AreEqual("x", view["name"]);
            Assert.AreEqual(1, storage.reads);
            Assert.AreEqual(0, storage.writes);
        }

        [Test]
        public void AStorageThatOnlyAnswersTryLoadCountsAsNoSave()
        {
            var key = "Yu5h1Lib.Tests.Entry.Plain";
            var plain = new PlainStorage();
            Preferences.Register(key, plain);
            registered[key] = plain;

            Preferences.GetView(key, out bool loaded);
            Assert.IsFalse(loaded);
            Assert.IsNull(Preferences.GetLoadError(key));
        }

        // ---- CFG-02 ----

        public static readonly string[] Damaged =
        {
            "{\"_entries\":[{\"key\":\"count\",\"value\":\"4\"}",
            "null",
            "",
            "[]",
            "{\"_entries\":[{\"key\":\"count\",\"value\":1}]}",
            "{\"_entries\":[{\"key\":\"count\",\"value\":\"4\"},{\"key\":\"$version\",\"value\":\"2\"}]}",
        };

        [TestCaseSource(nameof(Damaged))]
        public void ADamagedOrFutureSaveIsAnErrorThatIsNeverOverwritten(string raw)
        {
            var (key, storage) = NewKey("Damaged" + Math.Abs(raw.GetHashCode()), raw);
            ExpectLoadError(key);

            var view = Preferences.GetView(key, out bool loaded);
            Assert.IsFalse(loaded);
            Assert.IsNotNull(Preferences.GetLoadError(key));
            Assert.AreEqual(0, view.Count, "the view must not carry data read from the unreadable save");

            Put(key, "count", "9");
            Preferences.Save(key);

            Assert.AreEqual(0, storage.writes);
            Assert.AreEqual(raw, storage.raw);
            Assert.IsNotNull(Preferences.GetLoadError(key));

            storage.raw = Saved("count", "4");
            Assert.IsTrue(Preferences.Reload(key));
            Assert.IsNull(Preferences.GetLoadError(key));
            Assert.AreSame(view, Preferences.GetView(key), "reload must keep the shared view object");
            Assert.AreEqual("4", view["count"]);
            Assert.AreEqual(0, storage.writes, "reloading must not write");

            Put(key, "count", "5");
            Assert.AreEqual(1, storage.writes);
            StringAssert.Contains("\"$version\"", storage.raw);
            Assert.AreEqual("5", PreferencesFormat.Parse(storage.raw).Data["count"]);
        }

        [Test]
        public void ReloadKeepsTheErrorWhileTheSaveIsStillUnreadable()
        {
            var (key, storage) = NewKey(nameof(ReloadKeepsTheErrorWhileTheSaveIsStillUnreadable), "{oops");
            ExpectLoadError(key);
            Preferences.GetView(key);

            ExpectLoadError(key);
            Assert.IsFalse(Preferences.Reload(key));
            Assert.IsNotNull(Preferences.GetLoadError(key));
            Assert.AreEqual("{oops", storage.raw);
        }

        [Test]
        public void AFailedReadIsAnErrorToo()
        {
            var (key, storage) = NewKey(nameof(AFailedReadIsAnErrorToo), Saved("count", "4"));
            storage.throwOnRead = true;
            ExpectLoadError(key);

            Preferences.GetView(key);
            Put(key, "count", "9");

            Assert.IsNotNull(Preferences.GetLoadError(key));
            StringAssert.Contains("disk unavailable", Preferences.GetLoadError(key));
            Assert.AreEqual(0, storage.writes);

            storage.throwOnRead = false;
            Assert.IsTrue(Preferences.Reload(key));
            Assert.AreEqual("4", Preferences.GetView(key)["count"]);
        }

        [Test]
        public void AnOldSaveWithoutAVersionLoadsAndIsUpgradedOnTheNextSave()
        {
            var (key, storage) = NewKey(nameof(AnOldSaveWithoutAVersionLoadsAndIsUpgradedOnTheNextSave),
                "{\"_entries\":[{\"key\":\"count\",\"value\":\"4\"}]}");
            var view = Preferences.GetView(key, out bool loaded);

            Assert.IsTrue(loaded);
            Assert.IsNull(Preferences.GetLoadError(key));
            Assert.AreEqual("4", view["count"]);
            Assert.AreEqual(0, storage.writes);

            Put(key, "count", "5");
            Assert.AreEqual(1, storage.writes);
            StringAssert.Contains("\"$version\"", storage.raw);
        }

        [Test]
        public void AnExplicitResetReplacesTheUnreadableSave()
        {
            var (key, storage) = NewKey(nameof(AnExplicitResetReplacesTheUnreadableSave), "{oops");
            ExpectLoadError(key);
            Preferences.GetView(key);

            Preferences.ClearLoadError(key);
            Assert.IsNull(Preferences.GetLoadError(key));
            Put(key, "count", "5");

            Assert.AreEqual(1, storage.writes);
            Assert.AreEqual("5", PreferencesFormat.Parse(storage.raw).Data["count"]);
        }

        [Test]
        public void DeleteRemovesTheSaveAndTheErrorSoTheKeyIsPlainlyMissing()
        {
            var (key, storage) = NewKey(nameof(DeleteRemovesTheSaveAndTheErrorSoTheKeyIsPlainlyMissing), "{oops");
            ExpectLoadError(key);
            Preferences.GetView(key);

            Preferences.Delete(key);

            Assert.IsNull(storage.raw);
            Assert.IsNull(Preferences.GetLoadError(key));
            Put(key, "count", "1");
            Assert.AreEqual(1, storage.writes);
        }

        [Test]
        public void AKeyWithoutASaveIsNeverAnError()
        {
            var (key, storage) = NewKey(nameof(AKeyWithoutASaveIsNeverAnError));
            Put(key, "count", "1");

            Assert.IsNull(Preferences.GetLoadError(key));
            Assert.AreEqual(1, storage.writes);
        }

        // ---- CFG-08 ----

        [Test]
        public void EveryChangeIsSavedOnceAndKeysDoNotOverwriteEachOther()
        {
            var (a, storageA) = NewKey(nameof(EveryChangeIsSavedOnceAndKeysDoNotOverwriteEachOther) + "A", Saved("count", "1"));
            var (b, storageB) = NewKey(nameof(EveryChangeIsSavedOnceAndKeysDoNotOverwriteEachOther) + "B", Saved("count", "7"));
            var viewA = Preferences.GetView(a);
            Preferences.GetView(b);
            var rawB = storageB.raw;

            int first = 0, second = 0;
            Action observer1 = () => first++;
            Action observer2 = () => second++;
            viewA.Changed += observer1;
            viewA.Changed += observer2;

            Put(a, "count", "2");
            Assert.AreEqual(1, storageA.writes);
            Assert.AreEqual(0, storageB.writes);
            Assert.AreEqual(rawB, storageB.raw);
            Assert.AreEqual((1, 1), (first, second));

            viewA.Changed -= observer2;
            Put(a, "count", "3");
            Assert.AreEqual(2, storageA.writes);
            Assert.AreEqual(0, storageB.writes);
            Assert.AreEqual((2, 1), (first, second));
            Assert.AreEqual("3", PreferencesFormat.Parse(storageA.raw).Data["count"]);
            Assert.AreEqual("7", Preferences.GetView(b)["count"]);
        }

        [Test]
        public void SettingTheSameValueAgainDoesNotWrite()
        {
            var (key, storage) = NewKey(nameof(SettingTheSameValueAgainDoesNotWrite), Saved("count", "1"));
            Put(key, "count", "1");
            Assert.AreEqual(0, storage.writes);
        }

        // ---- CFG-07, host-less half ----

        [Test]
        public void UnloadReleasesOnlyThatKeyAndTheNextAccessReadsAgain()
        {
            var (a, storageA) = NewKey(nameof(UnloadReleasesOnlyThatKeyAndTheNextAccessReadsAgain) + "A", Saved("count", "1"));
            var (b, storageB) = NewKey(nameof(UnloadReleasesOnlyThatKeyAndTheNextAccessReadsAgain) + "B", Saved("count", "7"));
            var oldViewA = Preferences.GetView(a);
            Preferences.GetView(b);

            Assert.IsTrue(Preferences.Unload(a));
            storageA.raw = Saved("count", "2");

            var newViewA = Preferences.GetView(a);
            Assert.AreNotSame(oldViewA, newViewA);
            Assert.AreEqual("2", newViewA["count"]);
            Assert.AreEqual(2, storageA.reads);
            Assert.AreEqual(1, storageB.reads, "B must not be read again");
            Assert.AreEqual("7", Preferences.GetView(b)["count"]);
        }

        [Test]
        public void AViewThatWasUnloadedNoLongerWritesTheKey()
        {
            var (key, storage) = NewKey(nameof(AViewThatWasUnloadedNoLongerWritesTheKey), Saved("count", "1"));
            var oldView = Preferences.GetView(key);
            Preferences.Unload(key);
            var newView = Preferences.GetView(key);

            oldView["count"] = "99";

            Assert.AreEqual(0, storage.writes);
            Assert.AreEqual("1", newView["count"]);
        }

        [Test]
        public void UnloadRefusesWhileThereAreUnsavedChanges()
        {
            var (key, storage) = NewKey(nameof(UnloadRefusesWhileThereAreUnsavedChanges), Saved("count", "1"));
            Preferences.GetView(key);
            Preferences.SetSaveOnChanged(key, false);
            Put(key, "count", "2");

            LogAssert.Expect(LogType.Warning, new Regex(@"Preferences \[" + Regex.Escape(key) + @"\]: not unloaded"));
            Assert.IsFalse(Preferences.Unload(key));
            Assert.AreEqual(0, storage.writes);
            Assert.AreEqual("2", Preferences.GetView(key)["count"], "the draft must survive a refused unload");

            Preferences.Save(key);
            Assert.AreEqual(1, storage.writes);
            Assert.IsTrue(Preferences.Unload(key));
        }

        [Test]
        public void UnloadingAKeyThatWasNeverLoadedIsFine()
        {
            Assert.IsTrue(Preferences.Unload("Yu5h1Lib.Tests.Entry.NeverLoaded"));
        }

        // ---- the view is the entry's own object ----

        [Test]
        public void ViewIsACopyOfTheLoadedObject_SoSavingBackIntoItKeepsTheRecord()
        {
            var key = "Yu5h1Lib.Tests.Entry.SharedObject";
            var storage = new SharedObjectStorage();
            Preferences.Register(key, storage);
            registered[key] = storage;

            var view = Preferences.GetView(key);
            Assert.AreNotSame(storage.record, view);
            Preferences.SetSaveOnChanged(key, true);
            Put(key, "Name", "B");
            Assert.AreEqual("B", storage.record["Name"]);
            Assert.AreEqual("a@b", storage.record["Email"]);
            Assert.AreEqual(2, storage.record.Count);

            Assert.IsTrue(Preferences.Reload(key));
            Assert.AreSame(view, Preferences.GetView(key));
            Assert.AreEqual("B", view["Name"]);
            Assert.AreEqual(2, view.Count);
            Assert.AreEqual(2, storage.record.Count);
        }

        [Test]
        public void CopyFromItselfOrItsOwnEntriesKeepsTheEntries()
        {
            var data = new DataView { { "Email", "a@b" }, { "Name", "A" } };
            data.CopyFrom(data);
            Assert.AreEqual(2, data.Count);
            data.CopyFrom(data.Entries);
            Assert.AreEqual(2, data.Count);
            Assert.AreEqual("A", data["Name"]);
        }

        [Test]
        public void UnregisterOnlyTakesBackItsOwnStorage()
        {
            var key = "Yu5h1Lib.Tests.Entry.Unregister";
            var mine = new MemoryStorage();
            var other = new MemoryStorage();
            Preferences.Register(key, other);
            registered[key] = other;

            Preferences.Unregister(key, mine);
            Assert.AreSame(other, Preferences.GetStorage(key));

            Preferences.Unregister(key, other);
            Assert.AreSame(Preferences.DefaultStorage, Preferences.GetStorage(key));
        }
    }
}
