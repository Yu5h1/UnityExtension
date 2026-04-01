using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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


        protected override void OnInitializing()
        {
            base.OnInitializing();
            var existsElements = transform.GetComponentsInChildren<T>();
            for (int i = 0; i < initialSpawnCount; i++)
            {
                var element = i >= existsElements.Length ? CreateElement() : existsElements[i];
                elements.Add(element);
                element.gameObject.name = $"{i}.Element";
                _ElementSpawned?.Invoke(element);
            }
            var count = elements.Count;
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                int prev = (i - 1 + count) % count;

                var nextItem = elements[next];
                var prevItem = elements[prev];

                var nav = elements[i].navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnDown = nextItem;
                nav.selectOnRight = nextItem;
                nav.selectOnUp = prevItem;
                nav.selectOnLeft = prevItem;
                elements[i].navigation = nav;
            }
        }
        private void OnEnable()
        {
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
