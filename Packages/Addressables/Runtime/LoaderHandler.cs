using System;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.AddressableAssets
{
    /// <summary>
    /// The one place content is loaded through. It holds the loader in a field: swapping loaders is replacing the
    /// component in that field, and an empty field loads from Resources with a <see cref="ResourcesLoader"/> on this
    /// GameObject. Code reaches it through <see cref="SingletonBehaviour{T}.instance"/>: the instance in the scene, else
    /// one made from a <c>LoaderHandler</c> prefab in Resources (where the loader can be set up), else a new one, which
    /// loads from Resources.
    /// <para>
    /// Scene events do not call the handler; they call a <see cref="LoaderAgent"/>, which loads through it.
    /// </para>
    /// </summary>
    public class LoaderHandler : SingletonBehaviour<LoaderHandler>
    {
        [SerializeField] private AssetLoader _loader;

        private ResourcesLoader _resourcesLoader;

        /// <summary>
        /// The loader in use: the one in the field, or the Resources loader when the field is empty. Setting it fills the
        /// field; setting null goes back to Resources.
        /// </summary>
        public AssetLoader loader
        {
            get
            {
                if (_loader != null)
                    return _loader;
                if (_resourcesLoader == null && !TryGetComponent(out _resourcesLoader))
                    _resourcesLoader = gameObject.AddComponent<ResourcesLoader>();
                return _resourcesLoader;
            }
            set => _loader = value;
        }

        /// <summary>Loads the <typeparamref name="T"/> at <paramref name="key"/> with <see cref="loader"/>; see <see cref="AssetLoader.LoadAsync{T}"/>.</summary>
        public Awaitable<T> LoadAsync<T>(string key, IProgress<float> progress = null, CancellationToken token = default) where T : Object
            => loader.LoadAsync<T>(key, progress, token);

        protected override void OnInstantiated() { }

        protected override void OnInitializing() { }
    }
}
