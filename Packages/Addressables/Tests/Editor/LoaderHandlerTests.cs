using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.AddressableAssets.Tests
{
    /// <summary>
    /// <see cref="LoaderHandler"/> found as the scene's singleton, loading with the loader in its field and with the
    /// Resources loader once the field is cleared. The Resources probe is
    /// <c>Resources/Yu5h1Lib.AddressableAssets.Tests/LoaderAgentProbe.prefab</c> beside this file, under an Editor
    /// folder so it never reaches a build.
    /// </summary>
    public class LoaderHandlerTests
    {
        internal const string ProbeKey = "Yu5h1Lib.AddressableAssets.Tests/LoaderAgentProbe";

        private readonly List<Object> created = new List<Object>();
        private GameObject handlerObject;
        private LoaderHandler handler;

        internal class TestLoader : AssetLoader
        {
            public readonly List<string> keys = new List<string>();
            public Object result;

            public override Awaitable<T> LoadAsync<T>(string key, IProgress<float> progress = null, CancellationToken token = default)
            {
                keys.Add(key);
                var source = new AwaitableCompletionSource<T>();
                source.SetResult(result as T);
                return source.Awaitable;
            }
        }

        [SetUp]
        public void SetUp()
        {
            LoaderHandler.RemoveInstanceCache();
            handlerObject = new GameObject(nameof(LoaderHandler));
            created.Add(handlerObject);
            handler = handlerObject.AddComponent<LoaderHandler>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
                if (obj != null)
                    Object.DestroyImmediate(obj);
            created.Clear();
            LoaderHandler.RemoveInstanceCache();
        }

        [Test]
        public void Instance_IsTheHandlerInTheScene()
        {
            Assert.AreSame(handler, LoaderHandler.instance);
        }

        [Test]
        public void EmptyField_LoadsWithResources()
        {
            Assert.IsInstanceOf<ResourcesLoader>(LoaderHandler.instance.loader);
        }

        [Test, Timeout(10000)]
        public async Task LoaderInField_LoadsFirst_ClearedField_LoadsWithResources()
        {
            var substitute = new GameObject("Substitute");
            created.Add(substitute);
            var testLoader = handlerObject.AddComponent<TestLoader>();
            testLoader.result = substitute;
            handler.loader = testLoader;

            var first = await LoaderHandler.instance.LoadAsync<GameObject>(ProbeKey);
            handler.loader = null;
            var second = await LoaderHandler.instance.LoadAsync<GameObject>(ProbeKey);

            Assert.AreSame(substitute, first);
            CollectionAssert.AreEqual(new[] { ProbeKey }, testLoader.keys);
            Assert.IsInstanceOf<ResourcesLoader>(handler.loader);
            Assert.IsNotNull(second);
            Assert.AreEqual("LoaderAgentProbe", second.name);
        }
    }
}
