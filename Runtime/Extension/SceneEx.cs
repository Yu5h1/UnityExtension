using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Yu5h1Lib.Runtime
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static class SceneEx
    {
        public static T Find<T>(this Scene scene,string name) where T : Component {
            var roots = scene.GetRootGameObjects();
            name = name.Replace("\\", "/");
            if (name.Contains("/"))
            {
                var hierarchyParts = name.Split("/");

                if (hierarchyParts.Length == 0)
                    return null;

                if (roots.TryGet(r => r.name == hierarchyParts[0],out GameObject root) )
                {
                    if (hierarchyParts.Length == 1)
                        return root.GetComponent<T>();
                    else if (root.transform.TryFind(hierarchyParts.Skip(1).Join("/"), out Transform target))
                        return target.GetComponent<T>();
                }
            }
            else
            {
                foreach (var item in roots)
                {
                    var result = item.GetComponentInChildren<T>(name);
                    if (result != null) return result;
                }
            }
            return null;
        }
    }
}
