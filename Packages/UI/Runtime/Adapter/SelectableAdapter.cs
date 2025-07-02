using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib.UI;

public abstract class SelectableAdapter<T> : UIControl where T : Selectable
{
    public T selectable;
    public bool interactable { get => selectable.interactable; set => selectable.interactable = value; }

    //private bool deselectOnDisable = true;
    protected override void OnInitializing()
    {
        base.OnInitializing();
        TryGetComponent(out selectable);
    }

    protected virtual void OnDisable()
    {
        if ($"{name}".printWarningIf(selectable == null))
            return;
        //if (!deselectOnDisable)
        //    return;
        selectable.Deselect();
    }
}
