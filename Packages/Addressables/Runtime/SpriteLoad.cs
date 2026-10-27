using UnityEngine;

namespace Yu5h1Lib.AddressableAssets
{
    /// <summary>
    /// The request a <see cref="SpriteAgent"/> holds: loads a Sprite by key and raises the events of
    /// <see cref="AsyncLoadObject{T}"/> with it:
    /// <code>Begin(key) → Progress → End(key) → judge → Loaded(sprite) → True／False → Completed</code>
    /// Wire Loaded to the Image or SpriteRenderer that shows it.
    /// </summary>
    public class SpriteLoad : ContentLoad<Sprite> { }
}
