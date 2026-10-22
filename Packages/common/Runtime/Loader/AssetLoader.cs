using System;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib
{
    /// <summary>
    /// Loads an object by key, such as a Resources path or an Addressables address. A component, so a loader is chosen
    /// by putting it in a field: the Addressables package's loader handler holds one and swaps it by replacing the
    /// component in that field.
    /// <para>
    /// Every loader follows the same contract: <see cref="LoadAsync{T}"/> returns null when the key has nothing to load
    /// and never throws for that; cancellation through the token throws <see cref="OperationCanceledException"/>, which
    /// <see cref="AsyncTaskObject.ExecuteAsync"/> turns into a canceled run, and returns no object.
    /// </para>
    /// </summary>
    public abstract class AssetLoader : MonoBehaviour
    {
        /// <summary>
        /// Loads the <typeparamref name="T"/> at <paramref name="key"/>, reporting 0..1 through
        /// <paramref name="progress"/>. Returns null when there is none.
        /// </summary>
        public abstract Awaitable<T> LoadAsync<T>(string key, IProgress<float> progress = null, CancellationToken token = default) where T : Object;

        /// <summary>
        /// Gives back an object this loader returned, once its holder no longer uses it. The default does nothing: a
        /// loader whose objects need releasing (Addressables handles) overrides it.
        /// </summary>
        public virtual void Release(Object asset) { }
    }
}
