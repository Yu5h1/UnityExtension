using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib.UI;

public class UI_SelectionBinding : UI_Binding<UI_Selection>
{
    public override string GetValue() => target.CurrentItem;

    public override void SetValue(string value) => target.SetCurrent(value);

    protected override void OnInitializing()
    {
        base.OnInitializing();
    }
}
