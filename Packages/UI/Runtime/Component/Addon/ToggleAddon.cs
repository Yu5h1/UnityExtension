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

    public bool isOn
    { 
        get
        {
            if ("Toggle is null.".printWarningIf(ui == null))
                return false;
            return ui.isOn;
        }
        set
        { 
            if ("Toggle is null.".printWarningIf(ui == null))
                return;
            if (ui.isOn == value)
                return;
            ui.isOn = value;
        }
    }

    void Start()
    {
        ui.onValueChanged.AddListener(onValueChangedInverse);
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
        g.alpha = isOn ? 1f : 0f;
    }

    public string GetFieldName() => gameObject.name;

    public string GetValue() => isOn.ToString();
    public void SetValue(string value) => isOn = bool.TryParse(value, out bool result) ? result : false;

    public void SetValue(Object Ibindable)
    {
        if (Ibindable is IValuePort valuePort)
            SetValue(valuePort.GetValue());
    }
}
