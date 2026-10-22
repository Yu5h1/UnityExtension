using System;
using System.Threading;
using UnityEngine;

namespace Yu5h1Lib.AddressableAssets
{
    /// <summary>
    /// The scene's stand-in for content that exists only after loading. Scene events (Start, a button, an item scrolling
    /// into view) call <see cref="Prepare"/> with a key; the agent loads the prefab through the
    /// <see cref="LoaderHandler"/> with its <see cref="request"/>, instantiates it under <c>parent</c> (the scene root when
    /// empty), hands the instance to the optional <see cref="initializer"/>, and the request raises Loaded with it.
    /// Place one agent per kind of content; it is not a singleton.
    /// <para>
    /// Release: the agent owns the one instance it shows and the asset it was made from. A successful load of another
    /// key replaces the current instance once the new one is ready (the old one stays visible while loading);
    /// <see cref="Release"/> and destroying the agent destroy the instance and give the asset back to the loader that
    /// loaded it (<see cref="AssetLoader.Release"/>). A failed, rejected or canceled load leaves nothing behind and keeps
    /// the current instance. Preparing the key already shown does nothing; preparing another key while one is loading
    /// cancels that load first.
    /// </para>
    /// </summary>
    public class LoaderAgent : MonoBehaviour
    {
        [SerializeField, Inline] private InstanceLoad _request;
        [SerializeField] private InstanceInitializer _initializer;
        [SerializeField] private Transform _parent;

        private CancellationTokenSource _running;
        private bool _loading;
        private string _pendingKey;
        private Loaded _pending;
        private Loaded _current;

        private struct Loaded
        {
            public string key;
            public AssetLoader loader;
            public GameObject asset;
            public GameObject instance;
        }

        /// <summary>The request each load runs; created on first use when the field is empty.</summary>
        public InstanceLoad request
        {
            get
            {
                if (_request == null)
                {
                    _request = ScriptableObject.CreateInstance<InstanceLoad>();
                    _request.name = nameof(InstanceLoad);
                }
                return _request;
            }
        }

        /// <summary>The optional component that sets up each new instance.</summary>
        public InstanceInitializer initializer
        {
            get => _initializer;
            set => _initializer = value;
        }

        /// <summary>Where new instances are placed; the scene root when null.</summary>
        public Transform parent
        {
            get => _parent;
            set => _parent = value;
        }

        /// <summary>The instance currently shown; null before the first successful load or after <see cref="Release"/>.</summary>
        public GameObject instance => _current.instance;
        /// <summary>The key of <see cref="instance"/>.</summary>
        public string key => _current.instance != null ? _current.key : null;

        /// <summary>Loads the content at <paramref name="key"/>; for UnityEvents. Failures are reported through the request's events.</summary>
        public async void Prepare(string key)
        {
            try
            {
                await PrepareAsync(key);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        /// <summary>
        /// Loads the content at <paramref name="key"/> and returns whether it is now <see cref="instance"/>. Canceling
        /// <paramref name="token"/> or destroying the agent cancels the load and returns false.
        /// </summary>
        public async Awaitable<bool> PrepareAsync(string key, CancellationToken token = default)
        {
            if (_current.instance != null && _current.key == key)
                return true;
            if (_loading && _pendingKey == key)
            {
                while (_loading)
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                return _current.instance != null && _current.key == key;
            }
            Cancel();
            while (_loading)
                await Awaitable.NextFrameAsync(destroyCancellationToken);

            var load = request;
            using var running = CancellationTokenSource.CreateLinkedTokenSource(token, destroyCancellationToken);
            _running = running;
            _loading = true;
            _pendingKey = key;
            load.owner = this;
            load.SetKey(key);
            var succeeded = false;
            try
            {
                succeeded = await load.ExecuteAsync(running.Token);
            }
            finally
            {
                if (_running == running)
                    _running = null;
                _loading = false;
                _pendingKey = null;
                var loaded = _pending;
                _pending = default;
                if (succeeded)
                {
                    Discard(_current);
                    _current = loaded;
                }
                else
                    Discard(loaded);
            }
            return succeeded;
        }

        /// <summary>Cancels a running load, destroys the instance and gives its asset back to the loader.</summary>
        public void Release()
        {
            Cancel();
            Discard(_current);
            _current = default;
        }

        internal async Awaitable<GameObject> LoadInstanceAsync(string key, IProgress<float> progress, CancellationToken token)
        {
            var loader = LoaderHandler.instance.loader;
            var asset = await loader.LoadAsync<GameObject>(key, progress, token);
            if (asset == null)
                return null;
            _pending = new Loaded { key = key, loader = loader, asset = asset };
            token.ThrowIfCancellationRequested();
            _pending.instance = Instantiate(asset, _parent);
            if (_initializer != null)
                _initializer.Initialize(_pending.instance, key);
            return _pending.instance;
        }

        private void Cancel()
        {
            if (_running == null)
                return;
            _running.Cancel();
            _running = null;
        }

        private static void Discard(Loaded loaded)
        {
            if (loaded.instance != null)
            {
                if (Application.isPlaying)
                    Destroy(loaded.instance);
                else
                    DestroyImmediate(loaded.instance);
            }
            if (loaded.asset != null && loaded.loader != null)
                loaded.loader.Release(loaded.asset);
        }

        private void OnDestroy()
        {
            Discard(_current);
            _current = default;
        }
    }
}
