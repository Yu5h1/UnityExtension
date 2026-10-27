using UnityEngine;

namespace Yu5h1Lib.AddressableAssets
{
    /// <summary>
    /// The request a <see cref="LoaderAgent"/> holds: loads a prefab by key, and raises the events of
    /// <see cref="AsyncLoadObject{T}"/> with the initialized instance:
    /// <code>Begin(key) → Progress → End(key) → judge → Loaded(instance) → True／False → Completed</code>
    /// Wire the scene's loading presentation to these events.
    /// </summary>
    public class InstanceLoad : ContentLoad<GameObject> { }
}
