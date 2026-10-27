using UnityEngine;

namespace Yu5h1Lib.AddressableAssets
{
    /// <summary>
    /// The scene's stand-in for a character or other prefab content: <see cref="ContentAgent{TAsset,TResult}.Prepare"/>
    /// loads the prefab at a key, instantiates it under <c>parent</c> (the scene root when empty), hands the instance to
    /// the optional <see cref="initializer"/>, and the request raises Loaded with it. Keys, cancellation and release
    /// follow <see cref="ContentAgent{TAsset,TResult}"/>; a discarded instance is destroyed.
    /// </summary>
    public class LoaderAgent : ContentAgent<GameObject, GameObject>
    {
        [SerializeField] private InstanceInitializer _initializer;
        [SerializeField] private Transform _parent;

        /// <summary>The request each load runs; created on first use when the field is empty.</summary>
        public new InstanceLoad request => base.request as InstanceLoad;

        /// <summary>The optional component that sets up each new instance.</summary>
        public InstanceInitializer initializer
        {
            get => _initializer;
            set => _initializer = value;
        }

        /// <summary>Where new instances are placed; the scene root when null.</summary>
        public Transform parent
        {
            get => _parent;
            set => _parent = value;
        }

        /// <summary>The instance currently shown; null before the first successful load or after <c>Release</c>.</summary>
        public GameObject instance => current;

        protected override ContentLoad<GameObject> CreateRequest() => ScriptableObject.CreateInstance<InstanceLoad>();

        protected override GameObject Produce(GameObject prefab, string key)
        {
            var instance = Instantiate(prefab, _parent);
            if (_initializer != null)
                _initializer.Initialize(instance, key);
            return instance;
        }

        protected override void DiscardResult(GameObject instance)
        {
            if (Application.isPlaying)
                Destroy(instance);
            else
                DestroyImmediate(instance);
        }
    }
}
