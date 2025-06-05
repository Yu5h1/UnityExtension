using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib.UI;

public abstract class SelectableAdapter<T> : UIControl where T : Selectable
{
    public T component;
    public bool interactable { get => component.interactable; set => component.interactable = value; }
}
