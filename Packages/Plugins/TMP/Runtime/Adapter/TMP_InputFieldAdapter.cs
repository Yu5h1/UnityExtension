using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Yu5h1Lib.UI;

public class TMP_InputFieldAdapter : InputFieldAdapter<InputField, TMP_InputField>
{
    protected override string GetText(InputField t0) => t0.text;
    protected override string GetText(TMP_InputField t1) => t1.text;
    protected override void SetText(InputField t0, string val) => t0.text = val;
    protected override void SetText(TMP_InputField t1, string val) => t1.text = val;
    protected override void SetTextWithoutNotify(InputField t0, string val) => t0.SetTextWithoutNotify(val);
    protected override void SetTextWithoutNotify(TMP_InputField t1, string val) => t1.SetTextWithoutNotify(val);

    public override string GetPlaceholderText(InputField t0)          => t0.GetPlaceholderText();
    public override void SetPlaceholderText(InputField t0, string value) => t0.SetPlaceholderText(value);

    public override string GetPlaceholderText(TMP_InputField t1) => t1.GetPlaceholderText();
    public override void SetPlaceholderText(TMP_InputField t1, string value) => t1.SetPlaceholderText(value);


    protected override UnityEvent<string> GetSubmitEvent(InputField t0) => t0.onSubmit;
    protected override UnityEvent<string> GetSubmitEvent(TMP_InputField t1) => t1.onSubmit;
    protected override UnityEvent<string> GetValueChangedEvent(InputField t0) => t0.onValueChanged;
    protected override UnityEvent<string> GetValueChangedEvent(TMP_InputField t1) => t1.onValueChanged;

    protected override UnityEvent<string> GetEndEditEvent(InputField t0) => t0.onEndEdit;
    protected override UnityEvent<string> GetEndEditEvent(TMP_InputField t1) => t1.onEndEdit;
    //protected override UnityEvent<string> GetValueChangedEvent(InputField t0) => t0.onValueChanged;
    //protected override UnityEvent<string> GetValueChangedEvent(TMP_InputField t1) => t1.onValueChanged;

    protected override void TogglePasswordVisible(InputField t0)
    {
        t0.inputType = t0.inputType == InputField.InputType.Password ? InputField.InputType.Standard : InputField.InputType.Password;
        t0.ForceLabelUpdate();
    }

    protected override void TogglePasswordVisible(TMP_InputField t1)
    {
        t1.inputType = t1.inputType == TMP_InputField.InputType.Password ? TMP_InputField.InputType.Standard : TMP_InputField.InputType.Password;
        t1.ForceLabelUpdate();
    }

    public void SetText(InputFieldAdapter adapter)
    {
        text = adapter.text;
    }


}
