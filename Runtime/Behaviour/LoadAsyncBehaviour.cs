using UnityEngine;

namespace Yu5h1Lib
{
    public abstract class LoadAsyncBehaviour : BaseMonoBehaviour
    {
        public abstract void OnProcessing(float percentage);
    }
    public abstract class LoadAsyncBehaviour<T> : LoadAsyncBehaviour where T : Component
    {
        [SerializeField, ReadOnly]
        private T _component;
        public T component => _component;
        protected override void OnInitializing()
        {
            this.GetComponent(ref _component);
        }

    }
}