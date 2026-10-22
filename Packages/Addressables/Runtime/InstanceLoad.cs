using System;
using System.Threading;
using UnityEngine;

namespace Yu5h1Lib.AddressableAssets
{
    /// <summary>
    /// The request a <see cref="LoaderAgent"/> holds: loads a prefab by key through the <see cref="LoaderHandler"/>,
    /// instantiates and initializes it, and raises the events of <see cref="AsyncLoadObject{T}"/> with the instance:
    /// <code>Begin(key) → Progress → End(key) → judge → Loaded(instance) → True／False → Completed</code>
    /// Wire the scene's loading presentation to these events. The agent sets the key on each
    /// <see cref="LoaderAgent.Prepare"/>, so it is not a setting here.
    /// </summary>
    public class InstanceLoad : AsyncLoadObject<GameObject>, IProgress<float>
    {
        private string _key;

        internal LoaderAgent owner;

        public override string key => _key;

        internal void SetKey(string key) => _key = key;

        protected override Awaitable<GameObject> Load(CancellationToken token)
            => owner.LoadInstanceAsync(key, this, token);

        void IProgress<float>.Report(float value) => ReportProgress(value);
    }
}
