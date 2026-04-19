using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib;
using Yu5h1Lib.Serialization;
using Yu5h1Lib.UI;

[DisallowMultipleComponent,RequireComponent(typeof(Toggle)),AddonFor(typeof(Toggle))]
public class ToggleAddon : UIControl<Toggle,bool>
{
    public UnityEvent<bool> ValueChangedInverse;
    public UnityEvent checkedEvent;
    public UnityEvent uncheckedEvent;

    public override bool value
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
    public override void AddListener(UnityAction<bool> method) => ui.onValueChanged.AddListener(method);
    public override void RemoveListener(UnityAction<bool> method) => ui.onValueChanged.RemoveListener(method);

    public override bool TryParse(string value, out bool result) => bool.TryParse(value, out result);

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
        g.alpha = value ? 1f : 0f;
    }

}
