using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Yu5h1Lib.UI;
using Yu5h1Lib;

public abstract class UI_Aide<T> : UIControl where T : UIBehaviour
{
    [SerializeField,ReadOnly]
    private T _ui;
    public T ui => _ui;
    protected override void OnInitializing()
    {
        base.OnInitializing();
        _ui = this.GetComponent(ref _ui);
    }
}
