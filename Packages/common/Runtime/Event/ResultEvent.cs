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
