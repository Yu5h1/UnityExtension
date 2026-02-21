using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib.UI;

[DisallowMultipleComponent,RequireComponent(typeof(Toggle))]
public class ToggleAddon : UI_Addon<Toggle>
{
    public UnityEvent<bool> ValueChangedInverse;
    public UnityEvent checkedEvent;
    public UnityEvent uncheckedEvent;

    protected override void OnInitializing()
    {
        base.OnInitializing();
        ui.onValueChanged.AddListener(onValueChangedInverse);
        //onValueChangedInverse(ui.isOn);
        //ui.onValueChanged?.Invoke(ui.isOn);
    }
    

    private void onValueChangedInverse(bool IsOn)
    {
        if (IsOn)
            checkedEvent?.Invoke();
        else 
            uncheckedEvent?.Invoke();
        ValueChangedInverse?.Invoke(!IsOn);
    }
    public void ToggleAlpha(CanvasGroup g)
    { 
        g.alpha = ui.isOn ? 1f : 0f;
    }
}
