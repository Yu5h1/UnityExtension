using System;
using UnityEngine;

namespace Yu5h1Lib
{
    public static class AdapterFactoryBootstrap
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void EditorRegisterAll()
        {
            // UnityEditor.TypeCache is faster than AppDomain scan in Editor.
            // GetTypesWithAttribute<T> includes derived attribute types,
            // so PreserveAdapterRegistrationAttribute is also captured here.
            AdapterFactory<Component>.RegisterAll(
                UnityEditor.TypeCache.GetTypesWithAttribute<AdapterRegistrationAttribute>());
        }
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RuntimeRegisterAll()
        {
            // RuntimeTypeCache filters out System/Unity assemblies and caches results.
            // Faster and more targeted than AppDomain.GetAssemblies().
            AdapterFactory<Component>.RegisterAll(
                RuntimeTypeCache.GetTypesWithAttribute<AdapterRegistrationAttribute>());
        }
#endif
        public static bool TryGetRawComponent<TOps, TComponnent>(this MonoBehaviour behaviour, ref TComponnent _component) where TOps : class, IAdapter<Component>
    where TComponnent : Component
        {
            if (_component != null)
                return true;
            foreach (var type in AdapterFactory<Component>.Adapters[typeof(TOps)])
                if (behaviour.TryGetComponent(type, out Component found))
                {
                    try
                    {
                        _component = (TComponnent)found;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        throw e;
                    }
                    return true;
                }

            return false;
        }
    }
}
