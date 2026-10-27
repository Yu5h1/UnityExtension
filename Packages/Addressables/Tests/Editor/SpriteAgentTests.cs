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
    /// <see cref="SpriteAgent"/> loading sprites through a scene <see cref="LoaderHandler"/> whose loader serves
    /// in-memory sprites: Loaded hands out the sprite, a new key gives the old sprite back only after Loaded handed out
    /// the new one, a missing key keeps the current sprite, the key already shown is not loaded again, and
    /// <see cref="ContentAgent{TAsset,TResult}.Release"/> gives the sprite back.
    /// </summary>
    public class SpriteAgentTests
    {
        private readonly List<Object> created = new List<Object>();
        private readonly List<string> log = new List<string>();
        private SpriteAgent agent;
        private SpriteLoader loader;

        private class SpriteLoader : AssetLoader
        {
            public readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
            public List<string> log;
            public int loads;

            public override Awaitable<T> LoadAsync<T>(string key, IProgress<float> progress = null, CancellationToken token = default)
            {
                loads++;
                var source = new AwaitableCompletionSource<T>();
                source.SetResult(sprites.TryGetValue(key, out var sprite) ? sprite as T : null);
                return source.Awaitable;
            }

            public override void Release(Object asset) => log.Add($"Release:{asset.name}");
        }

        [SetUp]
        public void SetUp()
        {
            LoaderHandler.RemoveInstanceCache();
            var handlerObject = new GameObject(nameof(LoaderHandler));
            created.Add(handlerObject);
            var handler = handlerObject.AddComponent<LoaderHandler>();
            loader = handlerObject.AddComponent<SpriteLoader>();
            loader.log = log;
            handler.loader = loader;
            foreach (var name in new[] { "a", "b" })
            {
                var texture = new Texture2D(2, 2);
                var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
                sprite.name = name;
                created.Add(texture);
                created.Add(sprite);
                loader.sprites[name] = sprite;
            }
            var agentObject = new GameObject(nameof(SpriteAgent));
            created.Add(agentObject);
            agent = agentObject.AddComponent<SpriteAgent>();
            created.Add(agent.request);
            agent.request.onLoaded.AddListener(s => log.Add($"Loaded:{s.name}"));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
                if (obj != null)
                    Object.DestroyImmediate(obj);
            created.Clear();
            log.Clear();
            LoaderHandler.RemoveInstanceCache();
        }

        [Test]
        public async Task Prepare_LoadsTheSprite()
        {
            Assert.IsTrue(await agent.PrepareAsync("a"));
            Assert.AreSame(loader.sprites["a"], agent.sprite);
            Assert.AreEqual("a", agent.key);
            CollectionAssert.AreEqual(new[] { "Loaded:a" }, log);
        }

        [Test]
        public async Task AnotherKey_ReleasesTheOldSpriteAfterLoadedHandedOutTheNewOne()
        {
            await agent.PrepareAsync("a");
            await agent.PrepareAsync("b");
            Assert.AreSame(loader.sprites["b"], agent.sprite);
            CollectionAssert.AreEqual(new[] { "Loaded:a", "Loaded:b", "Release:a" }, log);
        }

        [Test]
        public async Task MissingKey_KeepsTheCurrentSprite()
        {
            await agent.PrepareAsync("a");
            Assert.IsFalse(await agent.PrepareAsync("missing"));
            Assert.AreSame(loader.sprites["a"], agent.sprite);
            CollectionAssert.AreEqual(new[] { "Loaded:a" }, log);
        }

        [Test]
        public async Task SameKey_IsNotLoadedAgain()
        {
            await agent.PrepareAsync("a");
            Assert.IsTrue(await agent.PrepareAsync("a"));
            Assert.AreEqual(1, loader.loads);
        }

        [Test]
        public async Task Release_GivesTheSpriteBack()
        {
            await agent.PrepareAsync("a");
            agent.Release();
            Assert.IsNull(agent.sprite);
            Assert.IsNull(agent.key);
            CollectionAssert.AreEqual(new[] { "Loaded:a", "Release:a" }, log);
        }
    }
}
