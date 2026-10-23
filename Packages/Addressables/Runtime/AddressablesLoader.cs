using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.AddressableAssets
{
    /// <summary>
    /// Loads Addressables content by key (an address, or a label, of which the first match is loaded). Whether the
    /// content is in a local group (built into StreamingAssets) or on a server is the group's load path in the
    /// Addressables profile; this loader is the same for both. Put it in the <see cref="LoaderHandler"/>'s loader field.
    /// <para>
    /// Follows the <see cref="AssetLoader"/> contract: a key with no location for <typeparamref name="T"/> returns null
    /// without an error, because the locations are looked up first. Progress is the operation's
    /// <see cref="AsyncOperationHandle.PercentComplete"/> (dependencies included), and 1 when done. Canceling the token
    /// stops waiting at once and throws <see cref="OperationCanceledException"/>; Addressables cannot stop a started
    /// load, so its handle is released when the load completes. A failed load releases its handle and throws the
    /// operation's exception.
    /// </para>
    /// <para>
    /// Every object returned holds an Addressables handle until <see cref="Release"/> is called with it, once per
    /// load (the agent does this). Destroying the loader does not release them: the objects may still be in use.
    /// </para>
    /// </summary>
    public class AddressablesLoader : AssetLoader
    {
        private readonly Dictionary<Object, List<AsyncOperationHandle>> _handles = new Dictionary<Object, List<AsyncOperationHandle>>();

        public override async Awaitable<T> LoadAsync<T>(string key, IProgress<float> progress = null, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (key.IsEmpty())
                return null;
            progress?.Report(0f);

            var locations = Addressables.LoadResourceLocationsAsync(key, typeof(T));
            await WaitAsync(locations, token);
            if (locations.Status != AsyncOperationStatus.Succeeded)
                throw Fail(locations);
            var location = locations.Result.Count > 0 ? locations.Result[0] : null;
            Addressables.Release(locations);
            if (location == null)
                return null;

            var load = Addressables.LoadAssetAsync<T>(location);
            await WaitAsync(load, token, progress);
            if (load.Status != AsyncOperationStatus.Succeeded)
                throw Fail(load);
            var asset = load.Result;
            if (asset == null)
            {
                Addressables.Release(load);
                return null;
            }
            if (!_handles.TryGetValue(asset, out var held))
                _handles.Add(asset, held = new List<AsyncOperationHandle>());
            held.Add(load);
            progress?.Report(1f);
            return asset;
        }

        /// <summary>Releases one load of <paramref name="asset"/>; an object this loader did not load is ignored.</summary>
        public override void Release(Object asset)
        {
            if (asset == null || !_handles.TryGetValue(asset, out var held))
                return;
            var handle = held[held.Count - 1];
            held.RemoveAt(held.Count - 1);
            if (held.Count == 0)
                _handles.Remove(asset);
            Addressables.Release(handle);
        }

        /// <summary>
        /// Waits for <paramref name="handle"/> frame by frame, reporting its progress. On cancellation the handle is
        /// released (now, or when it completes) before <see cref="OperationCanceledException"/> is thrown.
        /// </summary>
        private static async Awaitable WaitAsync(AsyncOperationHandle handle, CancellationToken token, IProgress<float> progress = null)
        {
            try
            {
                while (!handle.IsDone)
                {
                    progress?.Report(handle.PercentComplete);
                    await Awaitable.NextFrameAsync(token);
                }
                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                if (handle.IsDone)
                    Addressables.Release(handle);
                else
                    handle.Completed += done => Addressables.Release(done);
                throw;
            }
        }

        private static Exception Fail(AsyncOperationHandle handle)
        {
            var error = handle.OperationException ?? new Exception($"Addressables operation {handle.DebugName} failed.");
            Addressables.Release(handle);
            return error;
        }
    }
}
