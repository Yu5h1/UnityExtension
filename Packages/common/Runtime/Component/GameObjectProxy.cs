using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Exposes common GameObject operations as instance methods so they can be wired from
    /// UnityEvent / UnityEvent&lt;GameObject&gt; in the Inspector. Drop one in the scene
    /// (or on any GameObject) and route activation / lifecycle calls through it.
    /// </summary>
    [AddComponentMenu("Yu5h1Lib/GameObject Proxy"),DisallowMultipleComponent]
    public class GameObjectProxy : BaseMonoBehaviour
    {
        protected override void OnInitializing() {}

        // ============ Active state ============

        /// <summary>Flip the GameObject's activeSelf.</summary>
        public void ToggleActive(GameObject obj)
        {
            if (obj != null) obj.SetActive(!obj.activeSelf);
        }

        /// <summary>Activate (SetActive(true)).</summary>
        public void Activate(GameObject obj)
        {
            if (obj != null) obj.SetActive(true);
        }

        /// <summary>Deactivate (SetActive(false)).</summary>
        public void Deactivate(GameObject obj)
        {
            if (obj != null) obj?.SetActive(false);
        }
        

        // ============ Lifecycle ============

        /// <summary>Destroy the GameObject (UnityEngine.Object.Destroy).</summary>
        public void Destroy(GameObject obj)
        {
            // Explicit qualifier — calling unqualified Destroy(obj) would resolve to this
            // instance method (more specific param type) and recurse.
            if (obj != null) UnityEngine.Object.Destroy(obj);
        }

        /// <summary>Destroy after a delay in seconds.</summary>
        public void DestroyAfter(GameObject obj, float seconds)
        {
            if (obj != null) UnityEngine.Object.Destroy(obj, seconds);
        }

        // ============ Hierarchy ============

        /// <summary>Reparent under <paramref name="parent"/>, keeping world pose.</summary>
        public void SetParent(GameObject obj, Transform parent)
        {
            if (obj != null) obj.transform.SetParent(parent, worldPositionStays: true);
        }

        /// <summary>Detach to scene root, keeping world pose.</summary>
        public void Detach(GameObject obj)
        {
            if (obj != null) obj.transform.SetParent(null, worldPositionStays: true);
        }
    }
}
