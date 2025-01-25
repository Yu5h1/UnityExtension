using UnityEngine;

namespace Yu5h1Lib
{
    public abstract class LoadAsyncBehaviour : MonoBehaviour
    {
        public abstract void OnProcessing(float percentage);
    }
    public abstract class LoadAsyncBehaviour<T> : LoadAsyncBehaviour where T : Component
    {
        private T _component;
        protected T component => _component ?? TryGetComponent(out _component) ? _component : null;
    } 
}