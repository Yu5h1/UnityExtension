using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    public class InputFieldAdapter : SelectableAdapter<IInputFieldOps>, IInputFieldOps,IBindable
    {
        [SerializeField] private Toggle _PasswordMaskToggle;
        public bool showPasswordMaskToggle
        {
            get => _PasswordMaskToggle?.gameObject?.activeInHierarchy == true;
            set => _PasswordMaskToggle?.gameObject?.SetActive(value);
        }

        #region Ops 
        public string text { get => Ops.text; set => Ops.text = value; }
        public string placeholder { get => Ops.placeholder; set => Ops.placeholder = value; }
        public bool MaskPassword { get => Ops.MaskPassword; set => Ops.MaskPassword = value; }
        public int characterLimit { get => Ops.characterLimit; set => Ops.characterLimit = value; }
        public void SetTextWithoutNotify(string value) => Ops.SetTextWithoutNotify(value);
        public void DeactivateInputField() => Ops.DeactivateInputField();

        #endregion
        private bool _allowSubmit = true;
        public bool allowSubmit
        {
            get => _allowSubmit;
            set
            {
                if (_allowSubmit == value)
                    return;
                _allowSubmit = value;
                if (value)
                    Ops.submit += OnSubmit;
                else
                    Ops.submit -= OnSubmit;
            }
        }

        [SerializeField] private bool DontSubmitIfIsNullOrWhiteSpace;

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

   

        #region IBingable
        public string GetFieldName() => gameObject.name;
        public string GetValue() => text;
        public void SetValue(string value) => text = value;
        public void SetValue(Object bindable)
        {
            if (bindable is IBindable Ibindable)
                SetValue(Ibindable.GetValue());
        }
        #endregion


        protected override void OnInitializing()
        {
            base.OnInitializing();
            submit += OnSubmit;
            textChanged += TextChanged;
        }
        private void OnSubmit(string txt)
        {
            if (!allowSubmit)
                return;
            if (DontSubmitIfIsNullOrWhiteSpace && txt.IsEmpty())
                return;
            _submit?.Invoke(txt);
        }
        private void TextChanged(string txt)
        {
            
        }
        public void Submit() => TriggerEvent(ExecuteEvents.submitHandler);

        protected override void OnDisable()
        {
            base.OnDisable();
        }

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
        //    //if (DisableIMEOnSelect && forerlyMode != null)
        //    //    Input.imeCompositionMode = forerlyMode.Value;
        //}
        //void DisableIME()
        //{

        //    Input.imeCompositionMode = IMECompositionMode.;
        //}


    }
}