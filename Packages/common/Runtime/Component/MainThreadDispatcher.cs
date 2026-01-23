using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;


namespace Yu5h1Lib
{
    /// <summary>
    /// Helper to execute actions on Unity's main thread
    /// WebSocket callbacks run on background threads, need this for Unity API calls
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher instance;
        private readonly Queue<Action> executionQueue = new Queue<Action>();
        private static readonly object lockObject = new object();

        private static MainThreadDispatcher GetInstance()
        {
            if (instance != null)
                return instance;
            var go = new GameObject("MainThreadDispatcher");
            instance = go.AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(go);
            Debug.Log("✅ MainThreadDispatcher initialized");
            return instance;
        }

        public static MainThreadDispatcher Instance => GetInstance();

        void Update()
        {
            lock (lockObject)
            {
                while (executionQueue.Count > 0)
                {
                    try
                    {
                        executionQueue.Dequeue()?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ MainThreadDispatcher error: {ex.Message}");
                    }
                }
            }
        }

        public static void Enqueue(Action action)
        {
            lock (lockObject)
            {
                instance.executionQueue.Enqueue(action);
            }
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class MainThreadDispatcherExtensions
    {
        public static void InvokeOnMainThread(this UnityEvent e)
            => MainThreadDispatcher.Enqueue(e.Invoke);

        public static void InvokeOnMainThread<T>(this UnityEvent<T> e, T arg)
            => MainThreadDispatcher.Enqueue(() => e.Invoke(arg));

        public static void InvokeOnMainThread<T1, T2>(this UnityEvent<T1, T2> e, T1 arg1, T2 arg2)
            => MainThreadDispatcher.Enqueue(() => e.Invoke(arg1, arg2));

        public static void StartCoroutineOnMainThread(this MonoBehaviour mb, System.Collections.IEnumerator coroutine)
            => MainThreadDispatcher.Enqueue(() => mb.StartCoroutine(coroutine));
        public static void StopCoroutineOnMainThread(this MonoBehaviour mb, Coroutine coroutine)
            => MainThreadDispatcher.Enqueue(() => mb.StopCoroutine(coroutine));
    }
}