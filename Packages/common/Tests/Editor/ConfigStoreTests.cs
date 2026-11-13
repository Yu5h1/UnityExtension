using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib.Tests
{
    public sealed class ConfigStoreTests
    {
        [Test]
        public async Task ConfigStoreCommitsBeforeApplyAndRetriesWithoutWriting()
        {
            var write = new TaskCompletionSource<bool>();
            var observed = new List<ConfigStore<int>.Status>();
            var writes = 0;
            var applies = 0;
            using var store = new ConfigStore<int>(1, x => x, _ => { }, _ => Task.FromResult((false, 0)),
                (value, _) => { writes++; return write.Task; },
                (_, _) => ++applies == 2 ? Task.FromException(new IOException("apply")) : Task.CompletedTask);
            store.Changed += () => observed.Add(store.CurrentStatus);
            Assert.That(await store.LoadAsync(), Is.True);
            var pending = store.SaveAsync(2);
            Assert.That(store.Accepted, Is.EqualTo(1));
            Assert.That(await store.SaveAsync(3), Is.False);
            Assert.That(await store.LoadAsync(), Is.False);
            write.SetResult(true);
            Assert.That(await pending, Is.False);
            Assert.That(store.Accepted, Is.EqualTo(2));
            Assert.That(store.CurrentStatus, Is.EqualTo(ConfigStore<int>.Status.ApplyError));
            Assert.That(await store.SaveAsync(1), Is.False);
            Assert.That(await store.RetryApplyAsync(), Is.True);
            Assert.That(writes, Is.EqualTo(1));
            Assert.That(applies, Is.EqualTo(3));
            Assert.That(observed, Is.EqualTo(new[]
            {
                ConfigStore<int>.Status.Loading, ConfigStore<int>.Status.Applying, ConfigStore<int>.Status.Ready,
                ConfigStore<int>.Status.Saving, ConfigStore<int>.Status.Applying, ConfigStore<int>.Status.ApplyError,
                ConfigStore<int>.Status.Applying, ConfigStore<int>.Status.Ready
            }));
        }

        [Test]
        public async Task ConfigStorePreservesAcceptedValuesOnWriteFailureAndIsolatesCopies()
        {
            var fail = true;
            var writes = 0;
            var input = new[] { 1 };
            using var store = new ConfigStore<int[]>(input, value => (int[])value.Clone(),
                value => { if (value[0] < 0) throw new ArgumentException(); },
                _ => Task.FromResult((false, (int[])null)),
                (value, _) =>
                {
                    writes++;
                    value[0] = 90;
                    return fail ? Task.FromException(new IOException("write")) : Task.CompletedTask;
                },
                (value, _) => { value[0] = 91; return Task.CompletedTask; });
            input[0] = 92;
            Assert.That(await store.LoadAsync(), Is.True);
            Assert.That(await store.SaveAsync(new[] { -1 }), Is.False);
            Assert.That(writes, Is.Zero);
            var draft = new[] { 2 };
            Assert.That(await store.SaveAsync(draft), Is.False);
            Assert.That(store.CurrentStatus, Is.EqualTo(ConfigStore<int[]>.Status.SaveError));
            Assert.That(store.Accepted[0], Is.EqualTo(1));
            fail = false;
            Assert.That(await store.SaveAsync(draft), Is.True);
            var accepted = store.Accepted;
            accepted[0] = draft[0] = 93;
            Assert.That(store.Accepted[0], Is.EqualTo(2));
        }

        [Test]
        public async Task ConfigStoreCancelsAndIgnoresLateReadAfterDispose()
        {
            var response = new TaskCompletionSource<(bool, int)>();
            var token = default(CancellationToken);
            var applyCount = 0;
            var disposeCount = 0;
            using var store = new ConfigStore<int>(1, x => x, _ => { }, signal => { token = signal; return response.Task; },
                (_, _) => Task.CompletedTask, (_, _) => { applyCount++; return Task.CompletedTask; });
            store.Changed += () =>
            {
                if (store.CurrentStatus == ConfigStore<int>.Status.Disposed) disposeCount++;
            };
            var pending = store.LoadAsync();
            store.Dispose();
            store.Dispose();
            Assert.That(token.IsCancellationRequested, Is.True);
            Assert.Throws<ObjectDisposedException>(() => store.Changed += () => { });
            response.SetResult((true, 2));
            Assert.That(await pending, Is.False);
            Assert.That(store.Accepted, Is.EqualTo(1));
            Assert.That(await store.LoadAsync(), Is.False);
            Assert.That(applyCount, Is.Zero);
            Assert.That(disposeCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ConfigStoreReadsNativeStorageWithoutOverwritingCorruptData()
        {
            var key = "Yu5h1Lib.Tests.ConfigStore." + Guid.NewGuid();
            try
            {
                PlayerPrefs.SetString(key, "invalid");
                ConfigStore<int> Create() => new(1, x => x, _ => { }, _ =>
                {
                    var result = Preferences.DefaultStorage.Load(key);
                    if (result.Status == PreferencesLoadStatus.Error)
                        throw new IOException(result.Error);
                    return Task.FromResult(result.Status == PreferencesLoadStatus.Loaded
                        ? (true, int.Parse(result.Data["count"])) : (false, 0));
                }, (value, _) =>
                {
                    Preferences.DefaultStorage.Save(key, new DataView { ["count"] = value.ToString() });
                    return Task.CompletedTask;
                }, (_, _) => Task.CompletedTask);
                using var store = Create();
                Assert.That(await store.LoadAsync(), Is.False);
                Assert.That(await store.SaveAsync(4), Is.False);
                Assert.That(PlayerPrefs.GetString(key), Is.EqualTo("invalid"));
                PlayerPrefs.DeleteKey(key);
                Assert.That(await store.LoadAsync(), Is.True);
                Assert.That(store.Accepted, Is.EqualTo(1));
                Assert.That(await store.SaveAsync(4), Is.True);
                using var reopened = Create();
                Assert.That(await reopened.LoadAsync(), Is.True);
                Assert.That(reopened.Accepted, Is.EqualTo(4));
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }
    }
}
