using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib.UI
{
    public abstract class InputAdapter : UIControl
    {
        public TextAdapter TextAdapter;
        public Component component;

        [SerializeField]
        protected UnityEvent<string> _submit;
        public event UnityAction<string> submit
        {
            add => _submit.AddListener(value);
            remove => _submit.RemoveListener(value);
        }
        protected abstract UnityEvent<string> GetComponentSubmitEvent();

        private void Start()
        {
            GetComponentSubmitEvent().AddListener(OnSummit);
        }

        private void OnSummit(string content) => _submit?.Invoke(content);

        public abstract void TogglePasswordVisible();
    }


    public abstract class InputAdapter<T0,T1> : InputAdapter
        where T0 : Component
        where T1 : Component
    {
        
        protected override UnityEvent<string> GetComponentSubmitEvent() => component switch
        {
            T0 t0 => GetSubmitEvent(t0),
            T1 t1 => GetSubmitEvent(t1),
            _ => Unhandled()
        };
        protected abstract UnityEvent<string> GetSubmitEvent(T0 t0);
        protected abstract UnityEvent<string> GetSubmitEvent(T1 t1);

        public override void TogglePasswordVisible()
        {
            switch (component)
            {
                case T0 t0:
                    TogglePasswordVisible(t0);
                    break;
                case T1 t1:
                    TogglePasswordVisible(t1);
                    break;
                default:
                    break;
            }
        }
        protected abstract void TogglePasswordVisible(T0 t0);
        protected abstract void TogglePasswordVisible(T1 t1);


        public UnityEvent<string> Unhandled()
        {
            $"TextAdapter: 無法處理 {component.GetType().Name} 類型的 component。".printWarning();
            return null;
        }

        protected override void OnInitializing()
        {
            base.OnInitializing();
            if (TryGetComponent(out T0 t0))
                component = t0;
            else if (TryGetComponent(out T1 t1))
                component = t1;
        }

        
    }

}
