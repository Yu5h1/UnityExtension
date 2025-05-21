using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProcessEvent : EventContainer
{
    public LifeCycleEvent lifeCycle;
    public ResultEvent _result;

    public void Begin() => lifeCycle.Begin();
    public void End() => lifeCycle.End();
    public void Report(bool result) => _result.Invoke(result);
}
