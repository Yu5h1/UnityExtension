using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
public static class MonoBehaviourEx
{
    #region Find

    public static bool TryFindGameObjectWithTag(this MonoBehaviour b, string tag, out GameObject found)
        => found = GameObject.FindGameObjectWithTag(tag);
    
    public static GameObject[] FindGameObjectsWithTag(this MonoBehaviour b,params string[] tags)
    {
        var results = new List<GameObject>();
        foreach (string tag in tags)
            results.AddRange(GameObject.FindGameObjectsWithTag(tag));
        return results.ToArray();
    }

    #endregion
    #region Coroutine
    /// <summary>
    /// stop Reference coroutine if not null
    /// </summary>
    /// <param name="b"></param>
    /// <param name="coroutine"></param>
    /// <param name="enumerator"></param>
    public static void StartCoroutine(this MonoBehaviour b, ref Coroutine coroutine, IEnumerator enumerator)
    {
        if (coroutine != null)
            b.StopCoroutine(coroutine);
        coroutine = b.StartCoroutine(enumerator);
    }
    #endregion
    #region Creation
    /// <summary>
    /// get component if null
    /// </summary>
    public static T GetComponent<T>(this MonoBehaviour b,ref T component) where T : Component
    {
        if (!component)
            component = b.GetComponent<T>();
        return component;
    }
    public static T GetOrCreate<T>(this MonoBehaviour b, string childName, ref T result) where T : Component
    {
        if (result != null)
            return result;
        if (b.transform.TryGetComponentInChildren(childName, out result))
            return result;
        result = new GameObject(childName).AddComponent<T>();
        result.transform.parent = b.transform;
        result.transform.localPosition = Vector3.zero;
        result.transform.SetAsFirstSibling();

        return result;
    } 
    #endregion
}
