using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;


public class KeyCodeEventManager : SingletonBehaviour<KeyCodeEventManager>
{
    public List<KeyCodeEventHandler> handlers = new List<KeyCodeEventHandler>();
    protected override void OnInstantiated() 
    {
#if ENABLE_INPUT_SYSTEM

#else
        InputHandler.GetKeyDownCallback = Input.GetKeyDown;
        InputHandler.GetKeyCallback = Input.GetKey;
        InputHandler.GetKeyUpCallback = Input.GetKeyUp;
#endif
    }
    protected override void OnInitializing() { }

    private void Update()
    {
        for (int i = handlers.Count - 1; i >= 0; i--)
                handlers[i].Handle();
    }
}
