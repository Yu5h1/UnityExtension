using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib;
using static Yu5h1Lib.InputHandler;

public class KeyCodeEventHandler : MonoBehaviour
{
    public KeyCodeEventManager manager => KeyCodeEventManager.instance;
    public KeyCode keyCode;
    public KeyState State;
    public KeyCode[] extras;
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
        if (Evaluate(keyCode) || extras.Any(k => Evaluate(k)))
            Event?.Invoke();
    }
    public bool Evaluate(KeyCode key) =>
             State == KeyState.Down && GetKeyDown(key) ||
             State == KeyState.Hold && GetKey(key) ||
             State == KeyState.Up && GetKeyUp(key);

    private void OnDisable()
    {
        if (ApplicationInfo.WantsToQuit)
            return;
        manager.handlers.Remove(this);
    }
}
