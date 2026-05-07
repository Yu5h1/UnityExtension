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
public class ToggleAddon : UI_Adapter<IValuePortAdapter<bool>> 
{
    public UnityEvent<bool> ValueChangedInverse;
    public UnityEvent checkedEvent;
    public UnityEvent uncheckedEvent;

    void Start()
    {
        adapter.ChangedCallback += onValueChangedInverse;
        //ui.onValueChanged.AddListener(onValueChangedInverse);
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
        g.alpha = adapter?.value == true ? 1f : 0f;
    }

}
