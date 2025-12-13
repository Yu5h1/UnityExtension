using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

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