using System;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.AddressableAssets
{
    /// <summary>
    /// The scene's stand-in for content that exists only after loading. Scene events (Start, a button, an item scrolling
    /// into view) call <see cref="Prepare"/> with a key; the agent loads a <typeparamref name="TAsset"/> through the
    /// <see cref="LoaderHandler"/> with its <see cref="request"/>, turns it into the <typeparamref name="TResult"/> it
    /// shows (<see cref="Produce"/>: the asset itself, or an instance of a prefab), and the request raises Loaded with it.
    /// Place one agent per piece of content; it is not a singleton.
    /// <para>
    /// Release: the agent owns the one result it shows and the asset it came from. A successful load of another key
    /// replaces the current result once the new one is ready (the old one stays in use while loading, and is given back
    /// only after Loaded handed out the new one); <see cref="Release"/> and destroying the agent discard the result and
    /// give the asset back to the loader that loaded it (<see cref="AssetLoader.Release"/>). A failed, rejected or
    /// canceled load leaves nothing behind and keeps the current result. Preparing the key already shown does nothing;
    /// preparing the key being loaded waits for that load; preparing another key cancels the running load first.
    /// </para>
    /// </summary>
    public abstract class ContentAgent<TAsset, TResult> : MonoBehaviour, IContentSource<TResult>
        where TAsset : Object where TResult : Object
    {
        [SerializeField, Inline] private ContentLoad<TResult> _request;

        private CancellationTokenSource _running;
        private bool _loading;
        private string _pendingKey;
        private Loaded _pending;
        private Loaded _current;

        private struct Loaded
        {
            public string key;
            public AssetLoader loader;
            public TAsset asset;
            public TResult result;
        }

        /// <summary>The request each load runs; created on first use when the field is empty.</summary>
        public ContentLoad<TResult> request
        {
            get
            {
                if (_request == null)
                {
                    _request = CreateRequest();
                    _request.name = _request.GetType().Name;
                }
                return _request;
            }
        }

        /// <summary>The result currently shown; null before the first successful load or after <see cref="Release"/>.</summary>
        protected TResult current => _current.result;
        /// <summary>The key of the result currently shown.</summary>
        public string key => _current.result != null ? _current.key : null;

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
        /// Loads the content at <paramref name="key"/> and returns whether it is now the current result. Canceling
        /// <paramref name="token"/> or destroying the agent cancels the load and returns false.
        /// </summary>
        public async Awaitable<bool> PrepareAsync(string key, CancellationToken token = default)
        {
            if (_current.result != null && _current.key == key)
                return true;
            if (_loading && _pendingKey == key)
            {
                while (_loading)
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                return _current.result != null && _current.key == key;
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

        /// <summary>Cancels a running load, discards the current result and gives its asset back to the loader.</summary>
        public void Release()
        {
            Cancel();
            Discard(_current);
            _current = default;
        }

        async Awaitable<TResult> IContentSource<TResult>.LoadContentAsync(string key, IProgress<float> progress, CancellationToken token)
        {
            var loader = LoaderHandler.instance.loader;
            var asset = await loader.LoadAsync<TAsset>(key, progress, token);
            if (asset == null)
                return null;
            _pending = new Loaded { key = key, loader = loader, asset = asset };
            token.ThrowIfCancellationRequested();
            _pending.result = Produce(asset, key);
            return _pending.result;
        }

        /// <summary>Creates the request when the field is empty.</summary>
        protected abstract ContentLoad<TResult> CreateRequest();

        /// <summary>Turns the loaded asset into the result the agent shows, before the request raises Loaded.</summary>
        protected abstract TResult Produce(TAsset asset, string key);

        /// <summary>Discards a result that is no longer shown, before its asset is given back. Nothing by default.</summary>
        protected virtual void DiscardResult(TResult result) { }

        private void Cancel()
        {
            if (_running == null)
                return;
            _running.Cancel();
            _running = null;
        }

        private void Discard(Loaded loaded)
        {
            if (loaded.result != null)
                DiscardResult(loaded.result);
            if (loaded.asset != null && loaded.loader != null)
                loaded.loader.Release(loaded.asset);
        }

        protected virtual void OnDestroy()
        {
            Discard(_current);
            _current = default;
        }
    }
}
