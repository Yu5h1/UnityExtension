using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib;
using Yu5h1Lib.UI;

[DisallowMultipleComponent,RequireComponent(typeof(Toggle))]
public class ToggleAddon : UIControl<Toggle>,IValuePort
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

    public string GetFieldName() => gameObject.name;

    public string GetValue() => ui.isOn.ToString();
    public void SetValue(string value) => ui.isOn = bool.TryParse(value, out bool result) && result;

    public void SetValue(Object Ibindable)
    {
        if (Ibindable is IValuePort valuePort)
            SetValue(valuePort.GetValue());
    }
}
