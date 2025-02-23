using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    public abstract class BaseGroupHandler : UIControl {}

    [DisallowMultipleComponent]
    public abstract class GroupHandler<T> : BaseGroupHandler where T : Selectable
    {
        [SerializeField]
        protected T elementSource;
        [SerializeField]
        protected int initialSpawnCount;

        [SerializeField]
        private UnityEvent _Enabled;
        [SerializeField]
        private UnityEvent<T> _ElementSpawned;

        [ReadOnly]
        public List<T> elements = new List<T>();

        private bool initialized;
        private void Init()
        {
            if (initialized)
                return;
            var existsElements = transform.GetComponentsInChildren<T>();            
            for (int i = 0; i < initialSpawnCount; i++)
            {
                var element = i >= existsElements.Length ? CreateElement() : existsElements[i];
                elements.Add(element);
                element.gameObject.name = $"{i}.Element";
                _ElementSpawned?.Invoke(element);
            }            
            initialized = true;
            
        }

        private void OnEnable()
        {
            Init();
            _Enabled?.Invoke();
        }

        #region Methods
        public abstract T CreateElement();
        #endregion
    }

    public class GroupHandler : GroupHandler<Selectable>
    {
        public override Selectable CreateElement() => GameObject.Instantiate(elementSource, transform);
    }
}
