using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib.UI;

public class TMP_InputFieldBinding : UI_Binding<TMP_InputFieldAdapter>
{
    public override string GetValue() => target.text;
    public override void SetValue(string value) => target.SetTextWithoutNotify(value);
}
