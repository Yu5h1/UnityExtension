using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class UnityEventEx
    {
        public static void AddSafely<T>(this UnityEvent<T> e,UnityAction<T> action)
        {
            e.RemoveListener(action);
            e.AddListener(action);
        }

    }
}

