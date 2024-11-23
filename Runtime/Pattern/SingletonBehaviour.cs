using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

using static Yu5h1Lib.GameObjectEx;

namespace Yu5h1Lib
{
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T _instance;
        public static T instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                if (!TryFind(out _instance, true))
                {
                    if ($"Game is not playing Singleton<{typeof(T).Name}> stops creating instance.".printWarningIf(!Application.isPlaying))
                        return null;
                    if (!GameObjectEx.TryInstantiateFromResources(out _instance, typeof(T).Name, null, false))
                        _instance = GameObjectEx.New<T>();
                }

                Init(_instance);
                return _instance;
            }
        }
        public static bool DoesNotExists => _instance;

        protected abstract void Init();

        private static void Init(T component)
        {
            if (component is SingletonBehaviour<T> s)
                s.Init();
        }
        public static void RemoveInstanceCache()
        {
            _instance = null;
        }
    }

}
