using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class LifeCycleEvent : EventContainer
{
    public UnityEvent begin;
    public UnityEvent end;
    public void Begin() => begin?.Invoke();
    public void End() => end?.Invoke();
}
[System.Serializable]
public class LifeCycleEvent<T> : EventContainer 
{
    public UnityEvent<T> begin;
    public UnityEvent<T> end;
    public void Begin(T arg) => begin?.Invoke(arg);
    public void End(T arg) => end?.Invoke(arg);


}

public class LifeCycleAction<T>: ActionContainer
{
    public UnityAction<T> begin;
    public UnityAction<T> end;
    public void Begin(T arg) => begin?.Invoke(arg);
    public void End(T arg) => end?.Invoke(arg);
    
}

//[EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
//public static class LifeCycleEventEx
//{
//    public static void InvokeBegin(this LifeCycleEvent e)
//    {
//        if (e == null)
//            return;
//        e.begin?.Invoke();
//    }
//}