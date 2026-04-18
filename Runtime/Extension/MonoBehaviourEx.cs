using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static Yu5h1Lib.GameObjectUtility;

namespace Yu5h1Lib
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static class MonoBehaviourEx
    {
        #region Find

        public static bool TryFindGameObjectWithTag(this MonoBehaviour b, string tag, out GameObject found)
            => found = GameObject.FindGameObjectWithTag(tag);

        public static GameObject[] FindGameObjectsWithTag(this MonoBehaviour b, params string[] tags)
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
        private static IEnumerator DelayFrames(UnityAction action, int frames)
        {
            for (int i = 0; i < frames; i++)
                yield return null;
            action.Invoke();
        }
        private static IEnumerator DelaySeconds(UnityAction action, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            action.Invoke();
        }
        public static Coroutine DelayInvoke(this MonoBehaviour b, UnityAction action, int frames)
            => action == null ? null : b.StartCoroutine(DelayFrames(action, frames));
        public static void DelayInvoke(this MonoBehaviour b, ref Coroutine coroutine, UnityAction action, int frames)
            => b.StartCoroutine(ref coroutine, DelayFrames(action, frames));
        public static Coroutine DelayInvoke(this MonoBehaviour b, UnityAction action, float seconds)
            => action == null ? null : b.StartCoroutine(DelaySeconds(action, seconds));
        public static void DelayInvoke(this MonoBehaviour b, ref Coroutine coroutine, UnityAction action, float seconds)
            => b.StartCoroutine(ref coroutine, DelaySeconds(action, seconds));
        #endregion
        #region Creation
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
}
