using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.AddressableAssets.Tests
{
    /// <summary>
    /// <see cref="LoaderAgent"/> loading the Resources probe prefab through a scene <see cref="LoaderHandler"/>: the
    /// initializer runs once on the new instance before Loaded, a load without an initializer still succeeds, a missing
    /// key leaves nothing behind, the key already shown is not loaded again, and <see cref="LoaderAgent.Release"/>
    /// destroys the instance.
    /// </summary>
    public class LoaderAgentTests
    {
        private const string ProbeKey = LoaderHandlerTests.ProbeKey;

        private readonly List<Object> created = new List<Object>();
        private readonly List<string> log = new List<string>();
        private readonly List<GameObject> loaded = new List<GameObject>();
        private GameObject agentObject;
        private LoaderAgent agent;

        private class InitializedMarker : MonoBehaviour { }

        private class TestInitializer : InstanceInitializer
        {
            public readonly List<(GameObject instance, string key)> calls = new List<(GameObject, string)>();
            public List<string> log;

            public override void Initialize(GameObject instance, string key)
            {
                calls.Add((instance, key));
                log.Add("Initialize");
                instance.AddComponent<InitializedMarker>();
            }
        }

        [SetUp]
        public void SetUp()
        {
            LoaderHandler.RemoveInstanceCache();
            var handlerObject = new GameObject(nameof(LoaderHandler));
            created.Add(handlerObject);
            handlerObject.AddComponent<LoaderHandler>();
            agentObject = new GameObject(nameof(LoaderAgent));
            created.Add(agentObject);
            agent = agentObject.AddComponent<LoaderAgent>();
            created.Add(agent.request);
            agent.request.onBegin.AddListener(s => log.Add($"Begin:{s}"));
            agent.request.onEnd.AddListener(s => log.Add($"End:{s}"));
            agent.request.onLoaded.AddListener(g =>
            {
                loaded.Add(g);
                log.Add("Loaded");
            });
        }

        [TearDown]
        public void TearDown()
        {
            agent.Release();
            foreach (var obj in created)
                if (obj != null)
                    Object.DestroyImmediate(obj);
            created.Clear();
            log.Clear();
            loaded.Clear();
            LoaderHandler.RemoveInstanceCache();
        }

        [Test, Timeout(10000)]
        public async Task Prepare_InitializesTheNewInstanceOnceBeforeLoaded()
        {
            var initializer = agentObject.AddComponent<TestInitializer>();
            initializer.log = log;
            agent.initializer = initializer;

            var succeeded = await agent.PrepareAsync(ProbeKey);

            Assert.IsTrue(succeeded);
            var instance = agent.instance;
            Assert.IsNotNull(instance);
            Assert.IsTrue(instance.scene.IsValid());
            Assert.AreEqual(ProbeKey, agent.key);
            Assert.AreEqual(1, initializer.calls.Count);
            Assert.AreSame(instance, initializer.calls[0].instance);
            Assert.AreEqual(ProbeKey, initializer.calls[0].key);
            Assert.IsNotNull(instance.GetComponent<InitializedMarker>());
            Assert.AreSame(instance, agent.request.Result);
            CollectionAssert.AreEqual(new[] { instance }, loaded);
            CollectionAssert.AreEqual(new[]
            {
                $"Begin:{ProbeKey}", "Initialize", $"End:{ProbeKey}", "Loaded"
            }, log);
        }

        [Test, Timeout(10000)]
        public async Task Prepare_WithoutInitializer_Loads()
        {
            var succeeded = await agent.PrepareAsync(ProbeKey);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(agent.instance);
            CollectionAssert.AreEqual(new[] { agent.instance }, loaded);
        }

        [Test, Timeout(10000)]
        public async Task Prepare_MissingKey_FailsAndLeavesNoInstance()
        {
            var succeeded = await agent.PrepareAsync("Yu5h1Lib.AddressableAssets.Tests/NoSuchPrefab");

            Assert.IsFalse(succeeded);
            Assert.IsNull(agent.instance);
            CollectionAssert.AreEqual(new[]
            {
                "Begin:Yu5h1Lib.AddressableAssets.Tests/NoSuchPrefab", "End:Yu5h1Lib.AddressableAssets.Tests/NoSuchPrefab"
            }, log);
        }

        [Test, Timeout(10000)]
        public async Task Prepare_KeyAlreadyShown_KeepsTheInstance()
        {
            await agent.PrepareAsync(ProbeKey);
            var instance = agent.instance;
            log.Clear();

            var succeeded = await agent.PrepareAsync(ProbeKey);

            Assert.IsTrue(succeeded);
            Assert.AreSame(instance, agent.instance);
            CollectionAssert.IsEmpty(log);
        }

        [Test, Timeout(10000)]
        public async Task Release_DestroysTheInstance()
        {
            await agent.PrepareAsync(ProbeKey);
            var instance = agent.instance;

            agent.Release();

            Assert.IsTrue(instance == null);
            Assert.IsNull(agent.instance);
            Assert.IsNull(agent.key);
        }
    }
}
