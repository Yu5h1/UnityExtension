using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    using Result = VerificationLogic.Result;

    [DisallowMultipleComponent]
    public class VerificationPanel : UIControl
    {
        [SerializeField] private InputFieldAdapter[] _fields;
        public InputFieldAdapter[] fields { get => _fields; private set => _fields = value; }

        private InputFieldSetting[] settings;

        public Selectable[] buttons;

        [SerializeField] private RequestHandler<string> _requestHandler;

        public Result result { get; private set; }


        Coroutine WaitResultRoutine;

        public VerificationLogic.ResultHandler Validator;
        Func<IEnumerator> BuildWaitResult;


        protected override void OnInitializing()
        {
            base.OnInitializing();

            for (int i = 0; i < fields.Length; i++)
            {
                int index = i; // closure-safe
                fields[i].textChanged += txt =>
                {
                    if (settings.IsValid(index))
                        settings[index].textChanged?.Invoke(fields[index]);
                };
                fields[i].submit += txt =>
                {
                    if (settings.IsValid(index))
                        settings[index].submit?.Invoke(fields[index]);
                };
            }
        }

        private void InutputFieldTextChanged(string txt)
        {
            
        }

        public void Verify() => this.StartCoroutine(ref WaitResultRoutine, BuildWaitResultRoutine());

        public void Verify(InputFieldAdapter inputField)
        {
            if (inputField.characterLimit > 0 && inputField.text.Length != inputField.characterLimit)
                return;
            this.StartCoroutine(ref WaitResultRoutine, BuildWaitResultRoutine());
        }
        IEnumerator BuildWaitResultRoutine(){
            _requestHandler.Begin("");
            yield return BuildWaitResult();
            _requestHandler.End("");
            result = Validator?.Invoke(this);
            _requestHandler.Report(result, result.Content);
        }

        public void Show(VerificationLogic logic)
        {
            if (!isActiveAndEnabled)
                gameObject.SetActive(true);
            settings = logic.inputFieldAdapterSettings;
            for (int i = 0; i < MathF.Min(fields.Length, settings.Length); i++)
            {
                fields[i].gameObject.SetActive(true);
                fields[i].text = "";
                fields[i].placeholder = settings[i].placeHolder;
                fields[i].characterLimit = settings[i].characterLimit;
                fields[i].showPasswordMaskToggle = settings[i].usePasswordMask;
                fields[i].MaskPassword = settings[i].usePasswordMask;
            }
            for (int i = settings.Length; i < fields.Length; i++)
                fields[i].gameObject.SetActive(false);
            foreach (var btn in buttons) btn.gameObject.SetActive(logic.showButtons);
            BuildWaitResult = logic.BuildRoutine;
            Validator = logic.GetResult;

        }

        private void OnDisable()
        {
            Validator = null;
            BuildWaitResult = null;
            settings = null;
            if (WaitResultRoutine != null)
                StopCoroutine(WaitResultRoutine);
            
        }
    }
}