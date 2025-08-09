using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class TaskEvent : EventContainer
{
    public LifeCycleEvent lifeCycle;
    public ResultEvent _result;

    public void Begin() => lifeCycle.Begin();
    public void End() => lifeCycle.End();
    public void Report(bool result) => _result.Invoke(result);
}

[System.Serializable]
public class TaskEvent<T> : EventContainer
{
    public bool triggerSharedEvent = true;
    public LifeCycleEvent<T> lifeCycle;
    public ResultEvent<T> _result;

    public void Begin(T t) => lifeCycle?.Begin(t);
    public void End(T t) => lifeCycle?.End(t);
    public void Report(bool result, T t) => _result?.Invoke(result, t);
}

