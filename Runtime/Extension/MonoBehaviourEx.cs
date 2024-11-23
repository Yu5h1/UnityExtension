using System.Collections;
using UnityEngine;
using Yu5h1Lib;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
public static class MonoBehaviourEx
{
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
}
