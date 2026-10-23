using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.AddressableAssets.Tests
{
    /// <summary>
    /// The parts of the <see cref="AddressablesLoader"/> contract that need no built content: an empty key, a canceled
    /// token and releasing a foreign object never reach Addressables. Loading built content is verified in a project
    /// that has a content build (Yu5h1LibTest, scene <c>Assets/AddressablesTest/AddressablesTest.unity</c>).
    /// </summary>
    public class AddressablesLoaderTests
    {
        private GameObject loaderObject;
        private AddressablesLoader loader;

        [SetUp]
        public void SetUp()
        {
            loaderObject = new GameObject(nameof(AddressablesLoader));
            loader = loaderObject.AddComponent<AddressablesLoader>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(loaderObject);

        [Test]
        public async Task EmptyKey_ReturnsNull()
        {
            Assert.IsNull(await loader.LoadAsync<GameObject>(null));
            Assert.IsNull(await loader.LoadAsync<GameObject>(string.Empty));
        }

        [Test]
        public void CanceledToken_Throws()
        {
            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            Assert.CatchAsync<OperationCanceledException>(async () =>
                await loader.LoadAsync<GameObject>("any", null, canceled.Token));
        }

        [Test]
        public void Release_ObjectItDidNotLoad_IsIgnored()
        {
            var foreign = new GameObject("Foreign");
            try
            {
                Assert.DoesNotThrow(() => loader.Release(foreign));
                Assert.DoesNotThrow(() => loader.Release(null));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(foreign);
            }
        }
    }
}
