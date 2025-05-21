using System.Collections;
using System.Collections.Generic;
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
public class LifeCycleEvent<TArg> : EventContainer 
{
    public UnityEvent<TArg> begin;
    public UnityEvent<TArg> end;
    public void Begin(TArg arg) => begin?.Invoke(arg);
    public void End(TArg arg) => end?.Invoke(arg);
}