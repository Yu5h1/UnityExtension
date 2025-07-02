using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib.UI
{
    [DisallowMultipleComponent]
    public class UI_Verification : UIControl
    {
        public class Result {
            public bool Value;
            public string Message = "Not Implemented ! ";
            public static implicit operator bool(Result r) => r != null && r.Value;

        }

        [SerializeField] private InputFieldAdapter[] _fields;
        public InputFieldAdapter[] fields { get => _fields; private set => _fields = value; }


        [SerializeField] private ProcessEvent<string> _processEvent;

        public int CodeLength { get; private set; }
        public Result result { get; private set; }

        public Func<UI_Verification,Result> Validator;

        Coroutine WaitResultRoutine;

         
        IEnumerator WaitResult;

        private void Start()
        {            
            if (_fields.Length > 0)
            {
                fields[0].textChanged += InputField_textChanged;
                fields[0].submit += Submit;
                //fields[0].endEdit += Submit;
            }
        }
        public void Submit() => Submit(fields[0].text);
        private void Submit(string text)
        {
            this.StartCoroutine(ref WaitResultRoutine, BuildWaitResultRoutine());
        }

        private void InputField_textChanged(string text)
        {
            if (CodeLength == 0 || text.Length != CodeLength)
                return;
            this.StartCoroutine(ref WaitResultRoutine, BuildWaitResultRoutine());
        }

        private void OnEnable()
        {

        }
        public void Verify(string[] placeholders, int codeLength, Func<UI_Verification, Result> getResult, IEnumerator waitResult = null)
        {
            for (int i = 0; i < MathF.Min(fields.Length, placeholders.Length); i++)
            {
                fields[i].gameObject.SetActive(true);
                fields[i].placeholder = placeholders[i];
            }
            for (int i = placeholders.Length; i < fields.Length; i++)
                fields[i].gameObject.SetActive(false);
            CodeLength = codeLength;
            WaitResult = waitResult;
            Validator = getResult;
        }
        IEnumerator BuildWaitResultRoutine(){
            _processEvent.Begin("");
            yield return WaitResult;
            _processEvent.End("");
            result = Validator?.Invoke(this);
            _processEvent.Report(result, result.Message);
        }

        public void Verify(UI_VerificationLogic result)
        {
            if (!isActiveAndEnabled)
                gameObject.SetActive(true);
            result.Shown(this);
            Verify(result.placeholders, result.codeLength, result.GetResult, result.BuildRoutine());
        }

        private void OnDisable()
        {
            if (WaitResultRoutine != null)
                StopCoroutine(WaitResultRoutine);
        }
    }
}