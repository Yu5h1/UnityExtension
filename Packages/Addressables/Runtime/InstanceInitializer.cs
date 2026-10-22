using UnityEngine;

namespace Yu5h1Lib.AddressableAssets
{
    /// <summary>
    /// Sets up an instance a <see cref="LoaderAgent"/> has just created from loaded content: the consumer derives from it
    /// to add its own components and apply its own data for that key (a character's audio, lip sync, camera offset…),
    /// so the loaded prefab carries only what it must. Put the derived component in the agent's initializer field.
    /// </summary>
    public abstract class InstanceInitializer : MonoBehaviour
    {
        /// <summary>
        /// Called once per instance, right after it is instantiated and before the agent's request raises Loaded.
        /// </summary>
        public abstract void Initialize(GameObject instance, string key);
    }
}
