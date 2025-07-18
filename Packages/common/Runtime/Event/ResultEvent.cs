using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class ResultEvent : EventContainer
{
    public UnityEvent success;
    public UnityEvent failure;
    public void Invoke(bool result)
        => (result ? success : failure)?.Invoke();
}

[System.Serializable]
public class ResultEvent<T> : EventContainer
{
    public UnityEvent<T> success;
    public UnityEvent<T> failure;
    public UnityEvent<bool> finished;
    public void Invoke(bool result,T t)
    {
        (result ? success : failure)?.Invoke(t);
        finished?.Invoke(result);
    }
}
