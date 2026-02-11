using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    public abstract class BaseMonoBehaviour : MonoBehaviour
    {
        public bool initialized { get; private set;}
        private void InitializeAutomatically()
        {
            if (initialized)
                return;
            OnInitializing();
            initialized = true;
        }
        
        public void Init(bool force)
        {
            if (force)
                initialized = false;
            InitializeAutomatically();
        }
        [ContextMenu(nameof(Init),false,0)]
        public void Init() => Init(true);

        protected abstract void OnInitializing();

        protected virtual void Awake() => Init(true);

        public bool IsAvailable() => BehaviourEx.IsAvailable(this);


        public void ToggleActivate(GameObject obj) => obj.ToggleActive();
        public void ToggleActivate() => gameObject.ToggleActive();

        public virtual void Log(string msg) => msg.print();

        private IEnumerator BuildDelayInvoke(UnityAction action,int frames)
        {
            if (action == null)
                yield break;
            for (int i = 0; i < frames; i++)
                yield return null;            
            action.Invoke();
        }
        public void InvokeAfterFrames(UnityAction action, int frame)
            => StartCoroutine(BuildDelayInvoke(action, frame));
    }
}
