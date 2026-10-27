using System;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.AddressableAssets
{
    /// <summary>
    /// The request a <see cref="ContentAgent{TAsset, TResult}"/> holds: loads by key through the
    /// <see cref="LoaderHandler"/>, lets the agent turn the asset into what it shows, and raises the events of
    /// <see cref="AsyncLoadObject{T}"/> with it:
    /// <code>Begin(key) → Progress → End(key) → judge → Loaded(result) → True／False → Completed</code>
    /// Wire the scene's loading presentation to these events. The agent sets the key on each Prepare, so it is not a
    /// setting here.
    /// </summary>
    public abstract class ContentLoad<T> : AsyncLoadObject<T>, IProgress<float> where T : Object
    {
        private string _key;

        internal IContentSource<T> owner;

        public override string key => _key;

        internal void SetKey(string key) => _key = key;

        protected override Awaitable<T> Load(CancellationToken token) => owner.LoadContentAsync(key, this, token);

        void IProgress<float>.Report(float value) => ReportProgress(value);
    }

    /// <summary>What a <see cref="ContentLoad{T}"/> loads through: its agent.</summary>
    internal interface IContentSource<T> where T : Object
    {
        Awaitable<T> LoadContentAsync(string key, IProgress<float> progress, CancellationToken token);
    }
}
