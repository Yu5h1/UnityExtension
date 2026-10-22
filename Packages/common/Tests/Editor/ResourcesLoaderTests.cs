using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    /// <see cref="ResourcesLoader"/> on a real Resources asset (<c>Resources/Yu5h1Lib.Tests/ResourcesLoaderProbe.txt</c>
    /// beside this file, under an Editor folder so it never reaches a build): a key that exists, one that does not, and
    /// a load canceled while it runs, which reports nothing more once a second load of the same asset has completed.
    /// The loader awaits the request itself, so these tests need no frames to advance.
    /// </summary>
    public class ResourcesLoaderTests
    {
        private const string ProbeKey = "Yu5h1Lib.Tests/ResourcesLoaderProbe";

        private GameObject host;
        private ResourcesLoader loader;

        private class ProgressLog : IProgress<float>
        {
            public readonly List<float> values = new List<float>();
            public void Report(float value) => values.Add(value);
        }

        [SetUp]
        public void SetUp()
        {
            host = new GameObject(nameof(ResourcesLoaderTests));
            loader = host.AddComponent<ResourcesLoader>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(host);

        [Test, Timeout(10000)]
        public async Task ExistingKey_ReturnsTheObjectAndEndsAtFullProgress()
        {
            var progress = new ProgressLog();

            var asset = await loader.LoadAsync<TextAsset>(ProbeKey, progress);

            Assert.IsNotNull(asset);
            Assert.AreEqual("ResourcesLoaderProbe", asset.name);
            Assert.AreEqual(1f, progress.values[progress.values.Count - 1]);
        }

        [Test, Timeout(10000)]
        public async Task MissingKey_ReturnsNullWithoutThrowing()
        {
            var asset = await loader.LoadAsync<TextAsset>("Yu5h1Lib.Tests/NoSuchAsset");

            Assert.IsNull(asset);
        }

        [Test, Timeout(10000)]
        public async Task CanceledWhileLoading_ReturnsNoObjectAndStopsReporting()
        {
            var progress = new ProgressLog();
            using var cancellation = new CancellationTokenSource();
            var loading = loader.LoadAsync<TextAsset>(ProbeKey, progress, cancellation.Token);
            cancellation.Cancel();

            TextAsset asset = null;
            var canceled = false;
            try
            {
                asset = await loading;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            var reported = progress.values.Count;
            await Awaitable.FromAsyncOperation(Resources.LoadAsync<TextAsset>(ProbeKey));

            Assert.IsTrue(canceled);
            Assert.IsNull(asset);
            Assert.AreEqual(reported, progress.values.Count);
        }
    }
}
