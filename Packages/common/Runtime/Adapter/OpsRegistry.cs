using System.Reflection;
using UnityEngine;

namespace Yu5h1Lib.Common
{
    public static class OpsRegistry
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void RegisterAll()
        {
            
            foreach (var OpsType in RuntimeTypeCache.GetTypesWithAttribute<OpsRegistrationAttribute>())
            {
                var attrs = OpsType.GetCustomAttributes<OpsRegistrationAttribute>();
                foreach (var attr in attrs)
                {
                    OpsFactory.Register( attr.OpsInterface, attr.ComponentType, OpsType);
                }
            }
        }
    }
}
