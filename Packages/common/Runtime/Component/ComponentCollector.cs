using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yu5h1Lib;

namespace Yu5h1Lib
{
    public class ComponentCollector : MonoBehaviour
    {

    }
    public abstract class ComponentCollector<T> : ComponentCollector where T : Component
    {
        [SerializeField] protected bool includeChildren = true;
        [SerializeField] protected bool includeInactive = true;

        [SerializeField, ReadOnly]
        protected T[] _components;

        public IReadOnlyList<T> Components => _components;
        public int Count => _components?.Length ?? 0;
        public bool IsEmpty => Count == 0;

        public T First => _components?.FirstOrDefault();
        public T this[int index] => _components[index];

        public virtual void Refresh()
        {
            _components = includeChildren
                ? GetComponentsInChildren<T>(includeInactive)
                : GetComponents<T>();
        }

        protected virtual void Awake() => Refresh();

#if UNITY_EDITOR
        protected virtual void OnValidate() => Refresh();
#endif
    } 
}