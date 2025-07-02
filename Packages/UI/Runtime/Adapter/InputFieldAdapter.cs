using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [DisallowMultipleComponent]
    public abstract class InputFieldAdapter : SelectableAdapter<Selectable>
    {
        [SerializeField]
        private UnityEvent<string> _textChanged;
        public event UnityAction<string> textChanged
        {
            add => _textChanged.AddListener(value);
            remove => _textChanged.RemoveListener(value);
        }
        [SerializeField]
        private UnityEvent<string> _submit;
        public event UnityAction<string> submit
        {
            add => _submit.AddListener(value);
            remove => _submit.RemoveListener(value);
        }
        public string text { get => GetText(); set => SetText(value); }
        protected abstract string GetText();
        protected abstract void SetText(string value);
        public abstract void SetTextWithoutNotify(string value);

        public string placeholder
        {
            get => GetPlaceholderText();
            set => SetPlaceholderText(value);
        }
        protected abstract string GetPlaceholderText();
        protected abstract void SetPlaceholderText(string value);

        //protected abstract void AddSubmitListenner();
        //protected abstract void AddSubmitListenner();

        [SerializeField]
        private UnityEvent<string> _endEdit;
        public event UnityAction<string> endEdit
        {
            add => _endEdit.AddListener(value);
            remove => _endEdit.RemoveListener(value);
        }
        //[SerializeField]
        //private UnityEvent<string> _select;
        //public event UnityAction<string> select
        //{
        //    add => _select.AddListener(value);
        //    remove => _select.RemoveListener(value);
        //}
        //[SerializeField]
        //private UnityEvent<string> _deselect;
        //public event UnityAction<string> deselect
        //{
        //    add => _deselect.AddListener(value);
        //    remove => _deselect.RemoveListener(value);
        //}
        protected abstract UnityEvent<string> GetSubmitEvent();
        protected abstract UnityEvent<string> GetValueChangedEvent();
        protected abstract UnityEvent<string> GetEndEditEvent();
        //protected abstract UnityEvent<string> GetSelectEvent();
        //protected abstract UnityEvent<string> GetDeselectEvent();

        protected virtual void Start()
        {
            GetSubmitEvent().AddListener(OnSummit);
            GetValueChangedEvent().AddListener(OnTextChanged);
            //GetValueChangedEvent().AddListener(OnValueChanged);
            GetEndEditEvent().AddListener(OnEndEdit);
            //GetSelectEvent().AddListener(OnSelectInput);
            //GetDeselectEvent().AddListener(OnDeselectInput);

        }
        protected virtual void OnSummit(string content) => _submit?.Invoke(content);
        protected virtual void OnTextChanged(string content) => _textChanged?.Invoke(content);
        private void OnEndEdit(string content) => _endEdit?.Invoke(content);
        //private void OnSelectInput(string content) => _select?.Invoke(content);
        //private void OnDeselectInput(string content) => _deselect?.Invoke(content);

        public virtual string GetText(Graphic graphic)
        { 
            if (graphic is Text t)
                return t.text;
            throw new System.NotImplementedException($"{graphic.GetType()}");

        }

        public void Submit() => OnSummit(text);

        public abstract void TogglePasswordVisible();
    }
    
    public abstract class InputFieldAdapter<T0,T1> : InputFieldAdapter
        where T0 : Selectable
        where T1 : Selectable
    {

        protected override string GetText() => selectable switch
        {
            T0 t0 => GetText(t0),
            T1 t1 => GetText(t1),
            _ => Unhandled<string>()
        };
        protected override void SetText(string value)
        {
            switch (selectable)
            {
                case T0 t0: SetText(t0, value); break;
                case T1 t1: SetText(t1, value); break;
                default: Unhandled(); break;
            }
        }
        protected abstract string GetText(T0 t0);
        protected abstract void SetText(T0 t0, string val);
        protected abstract string GetText(T1 t1);
        protected abstract void SetText(T1 t1, string val);

        public override void SetTextWithoutNotify(string value)
        {
            switch (selectable)
            {
                case T0 t0: SetTextWithoutNotify(t0, value); break;
                case T1 t1: SetTextWithoutNotify(t1, value); break;
                default: Unhandled(); break;
            }
        }
        protected abstract void SetTextWithoutNotify(T0 t0, string val);
        protected abstract void SetTextWithoutNotify(T1 t1, string val);

        protected override string GetPlaceholderText() => selectable switch
        {
            T0 t0 => GetPlaceholderText(t0),
            T1 t1 => GetPlaceholderText(t1),
            _ => Unhandled<string>()
        };
        protected override void SetPlaceholderText(string value)
        {
            switch (selectable)
            {
                case T0 t0: SetPlaceholderText(t0, value); break;
                case T1 t1: SetPlaceholderText(t1, value); break;
                default: Unhandled(); break;
            }
        }
        public abstract string GetPlaceholderText(T0 t0);
        public abstract void SetPlaceholderText(T0 t0, string value);
        public abstract string GetPlaceholderText(T1 t1);
        public abstract void SetPlaceholderText(T1 t1, string value);

        protected abstract void TogglePasswordVisible(T0 t0);
        protected abstract void TogglePasswordVisible(T1 t1);

        public void Unhandled()
        {
            $"{GetType().Name}: 無法處理 {selectable.GetType().Name} 類型的 component。".printWarning();
        }
        public T Unhandled<T>()
        {
            Unhandled();
            return default(T);
        }

        protected override void OnInitializing()
        {
            base.OnInitializing();
            if (!(selectable is T0 t0 || selectable is T1 t1 ))
            {
                if (TryGetComponent(out t0))
                    selectable = t0;
                else if (TryGetComponent(out t1))
                    selectable = t1;
            }
        }
        protected override UnityEvent<string> GetValueChangedEvent() => selectable switch
        {
            T0 t0 => GetValueChangedEvent(t0),
            T1 t1 => GetValueChangedEvent(t1),
            _ => Unhandled<UnityEvent<string>>()
        };
        protected abstract UnityEvent<string> GetValueChangedEvent(T0 t0);
        protected abstract UnityEvent<string> GetValueChangedEvent(T1 t1);

        protected override UnityEvent<string> GetSubmitEvent() => selectable switch
        {
            T0 t0 => GetSubmitEvent(t0),
            T1 t1 => GetSubmitEvent(t1),
            _ => Unhandled<UnityEvent<string>>()
        };
        protected abstract UnityEvent<string> GetSubmitEvent(T0 t0);
        protected abstract UnityEvent<string> GetSubmitEvent(T1 t1);

        protected override UnityEvent<string> GetEndEditEvent() => selectable switch
        {
            T0 t0 => GetEndEditEvent(t0),
            T1 t1 => GetEndEditEvent(t1),
            _ => Unhandled<UnityEvent<string>>()
        };
        protected abstract UnityEvent<string> GetEndEditEvent(T0 t0);
        protected abstract UnityEvent<string> GetEndEditEvent(T1 t1);


        public override void TogglePasswordVisible()
        {
            switch (selectable)
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
        //protected abstract UnityEvent<string> GetValueChangedEvent(T0 t0);
        //protected abstract UnityEvent<string> GetValueChangedEvent(T1 t1);
        //protected override UnityEvent<string> GetValueChangedEvent() => component switch
        //{
        //    T0 t0 => GetValueChangedEvent(t0),
        //    T1 t1 => GetValueChangedEvent(t1),
        //    _ => Unhandled()
        //};

        //protected abstract UnityEvent<string> GetEndEditEvent(T0 t0);
        //protected abstract UnityEvent<string> GetEndEditEvent(T1 t1);
        //protected override UnityEvent<string> GetEndEditEvent() => component switch
        //{
        //    T0 t0 => GetEndEditEvent(t0),
        //    T1 t1 => GetEndEditEvent(t1),
        //    _ => Unhandled()
        //};

        //protected abstract UnityEvent<string> GetSelectEvent(T0 t0);
        //protected abstract UnityEvent<string> GetSelectEvent(T1 t1);
        //protected override UnityEvent<string> GetSelectEvent() => component switch
        //{
        //    T0 t0 => GetSelectEvent(t0),
        //    T1 t1 => GetSelectEvent(t1),
        //    _ => Unhandled()
        //};

        //protected abstract UnityEvent<string> GetDeselectEvent(T0 t0);
        //protected abstract UnityEvent<string> GetDeselectEvent(T1 t1);
        //protected override UnityEvent<string> GetDeselectEvent() => component switch
        //{
        //    T0 t0 => GetDeselectEvent(t0),
        //    T1 t1 => GetDeselectEvent(t1),
        //    _ => Unhandled()
        //};

    }

}
