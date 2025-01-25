using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// AddComponent<T> if null
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ComponentController<T> : MonoBehaviour where T : Component
    {
        [SerializeField,ReadOnly]
        private T _component;
        public T component => _component;

        protected virtual void CheckComponent()
        {
            if (!_component && !TryGetComponent(out _component))
                _component = gameObject.AddComponent<T>();
        }
        protected virtual void Reset()
        {
            CheckComponent();
        }
        protected virtual void Start()
        {
            CheckComponent();
        }
    }
}
