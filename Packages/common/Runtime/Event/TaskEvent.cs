using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Yu5h1Lib
{
    [System.Serializable]
    public class TaskEvent : EventContainer
    {
        public LifeCycleEvent lifeCycle;
        public BinaryEvent outcome;
        public UnityEvent<bool> completed;

        public void Begin() => lifeCycle.Begin();
        public void End() => lifeCycle.End();
        public void Invoke(bool success) 
        {
            outcome?.Invoke(success);
            completed?.Invoke(success);
        }
    }
    
    [System.Serializable]
    public class TaskEvent<T> : EventContainer
    {
        public LifeCycleEvent<T> lifeCycle;
        [FormerlySerializedAs("_outcome")]
        public BinaryEvent<T> outcome;
        public UnityEvent<bool> completed;

        public void Begin(T t) => lifeCycle?.Begin(t);
        public void End(T t) => lifeCycle?.End(t);
        public void Invoke(bool success, T t)
        {
            outcome?.Invoke(success, t);
            completed?.Invoke(success);
        }
    } 
}

