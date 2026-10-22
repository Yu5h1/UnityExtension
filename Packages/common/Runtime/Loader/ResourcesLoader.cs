using System;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib
{
    /// <summary>
    /// Loads from the project's Resources folders with <see cref="Resources.LoadAsync{T}(string)"/>; the key is the
    /// path under a Resources folder without extension. Resources content is built into the player, so this loader
    /// serves built-in content (fallbacks, validation), never content meant to stay out of the build.
    /// <para>
    /// It awaits the request's completion rather than polling frames, so it also completes in Edit Mode, where an
    /// unfocused editor barely advances frames. Progress is reported at the start and as 1 at the end: a Resources
    /// request has no useful intermediate progress. Unity cannot stop a started Resources request: after cancellation
    /// the request finishes on its own, nothing waits on it and its object is not returned; Unity unloads it with the
    /// other unused assets. <see cref="AssetLoader.Release"/> has nothing to give back.
    /// </para>
    /// </summary>
    public class ResourcesLoader : AssetLoader
    {
        public override async Awaitable<T> LoadAsync<T>(string key, IProgress<float> progress = null, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (key.IsEmpty())
                return null;
            var request = Resources.LoadAsync<T>(key);
            progress?.Report(request.progress);
            await Awaitable.FromAsyncOperation(request, token);
            token.ThrowIfCancellationRequested();
            progress?.Report(1f);
            return request.asset as T;
        }
    }
}
