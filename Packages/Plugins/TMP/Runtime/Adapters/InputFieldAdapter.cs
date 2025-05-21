using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Yu5h1Lib.UI;

public class InputFieldAdapter : InputAdapter<InputField, TMP_InputField>
{


    protected override UnityEvent<string> GetSubmitEvent(InputField t0) => t0.onSubmit;
    protected override UnityEvent<string> GetSubmitEvent(TMP_InputField t1) => t1.onSubmit;

    protected override void TogglePasswordVisible(InputField t0) 
        => t0.contentType = t0.contentType == InputField.ContentType.Password ? InputField.ContentType.Standard : InputField.ContentType.Password;

    protected override void TogglePasswordVisible(TMP_InputField t1)
        => t1.contentType = t1.contentType ==  TMP_InputField.ContentType.Password ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
}
