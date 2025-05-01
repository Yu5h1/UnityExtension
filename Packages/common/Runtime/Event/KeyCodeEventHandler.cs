using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib;



public class KeyCodeEventHandler : MonoBehaviour
{
    public KeyCodeEventManager manager => KeyCodeEventManager.instance;
    public KeyCode keyCode;
    public KeyState State;
    public UnityEvent Event;

    private void OnEnable()
    {
        if (manager.handlers.Contains(this))
            return;
        manager.handlers.Add(this);
    }
    public void Handle()
    {

        if (!isActiveAndEnabled)
            return;
        if ( State == KeyState.Down && Input.GetKeyDown(keyCode) ||
             State == KeyState.Hold && Input.GetKey(keyCode) ||
             State == KeyState.Up && Input.GetKeyUp(keyCode))
            Event?.Invoke();
    }
    private void OnDisable()
    {
        if (ApplicationInfo.WantsToQuit)
            return;
        manager.handlers.Remove(this);
    }
}
