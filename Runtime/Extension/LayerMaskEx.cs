
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static class LayerMaskEx
    {
        public static bool Contains(this LayerMask mask, int layer)
            => (mask.value & (1 << layer)) != 0;
        public static bool Contains(this LayerMask mask, string layerName)
            => mask.Contains(LayerMask.NameToLayer(layerName));

        public static T[] FindObjects<T>(this LayerMask layer, bool includeInactive) where T : Component
        {
            if (layer.value == 0)
                return new T[0];
            var results = new List<T>();
            foreach (var obj in GameObject.FindObjectsByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,FindObjectsSortMode.None))
            {
                if (layer.Contains(obj.gameObject.layer))
                    results.Add(obj);
            }
            return results.ToArray();
        }
    }
}
