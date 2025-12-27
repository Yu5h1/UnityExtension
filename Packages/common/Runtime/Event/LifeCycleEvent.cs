using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    [System.Serializable]
    public class LifeCycleEvent : RouteBase
    {
        public UnityEvent begin;
        public UnityEvent end;
        public void Begin() => begin?.Invoke();
        public void End() => end?.Invoke();
    }
    [System.Serializable]
    public class LifeCycleEvent<T> : RouteBase
    {
        public UnityEvent<T> begin;
        public UnityEvent<T> end;
        public void Begin(T arg) => begin?.Invoke(arg);
        public void End(T arg) => end?.Invoke(arg);
    }

    public class LifeCycleAction<T> : RouteBase
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
}