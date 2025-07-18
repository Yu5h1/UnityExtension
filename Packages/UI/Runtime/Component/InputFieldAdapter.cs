using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    public class InputFieldAdapter : SelectableAdapter<IInputFieldOps>, IInputFieldOps
    {
        [SerializeField] private Toggle _PasswordMaskToggle;
        public bool showPasswordMaskToggle
        {
            get => _PasswordMaskToggle?.gameObject?.activeInHierarchy == true;
            set => _PasswordMaskToggle?.gameObject?.SetActive(value);
        }

        public string text { get => Ops.text; set => Ops.text = value; }
        public string placeholder { get => Ops.placeholder; set => Ops.placeholder = value; }
        public bool MaskPassword { get => Ops.MaskPassword; set => Ops.MaskPassword = value; }
        public int characterLimit { get => Ops.characterLimit; set => Ops.characterLimit = value; }

        public void SetTextWithoutNotify(string value) => Ops.SetTextWithoutNotify(value);

        public UnityEvent<string> _submit;
        public event UnityAction<string> submit
        {
            add => Ops.submit += value;
            remove => Ops.submit -= value;
        }

        public event UnityAction<string> textChanged
        {
            add => Ops.textChanged += value;
            remove => Ops.textChanged -= value;
        }

        public event UnityAction<string> endEdit
        {
            add => Ops.endEdit += value;
            remove => Ops.endEdit -= value;
        }


        protected override void OnInitializing()
        {
            base.OnInitializing();
            submit += OnSubmit;
            textChanged += InputFieldAdapter_textChanged;
        }

        private void InputFieldAdapter_textChanged(string txt)
        {
            
        }

        private void OnSubmit(string text)
        {
            _submit?.Invoke(text);
        }
        
        public void Submit() => TriggerEvent(ExecuteEvents.submitHandler);

        //public bool DisableIMEOnSelect;
        //public static IMECompositionMode? forerlyMode;
        //public override void OnSelect(BaseEventData eventData)
        //{
        //    base.OnSelect(eventData);
        //    if (DisableIMEOnSelect)
        //    {
        //        forerlyMode = Input.imeCompositionMode;
        //        Input.imeCompositionMode = IMECompositionMode.On;
        //    }

        //}
        //public override void OnDeselect(BaseEventData eventData)
        //{            
        //    base.OnDeselect(eventData);
        //    if (DisableIMEOnSelect && forerlyMode != null)
        //            Input.imeCompositionMode = forerlyMode.Value;
        //}
        //void DisableIME()
        //{

        //    Input.imeCompositionMode = IMECompositionMode.;
        //}


    }
}