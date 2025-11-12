using UnityEngine;
using static Yu5h1Lib.GameObjectUtility;

namespace Yu5h1Lib
{
    public abstract class SingletonBehaviour : BaseMonoBehaviour
    {

    }
    public abstract class SingletonBehaviour<T> : SingletonBehaviour where T : SingletonBehaviour<T>
    {
        protected static T _instance;
        /// <summary>
        /// Check Resources first, and create it from there if it exists; if not, make a new one.;
        /// </summary>
        public static T instance
        {
            get
            {
             
                if (_instance != null)
                    return _instance;
                if (!TryFind(out _instance, true))
                {
                    if ($"[Singleton] Prevented creation of {typeof(T).Name} during application quit.".printWarningIf(ApplicationInfo.WantsToQuit))
                        return null;
                    if ($"Game is not playing Singleton<{typeof(T).Name}> stops creating instance.".printWarningIf(!Application.isPlaying))
                        return null;
                    if (!ResourcesUtility.TryInstantiateFromResources(out _instance, typeof(T).Name, null))
                        _instance = Create<T>();

                    _instance.OnInstantiated();
                }
                return _instance;
            }
        }
        protected abstract void OnInstantiated();

        public static bool Exists() => !Object.Equals(_instance, null);

        public static void RemoveInstanceCache()
        {
            _instance = null;
        }
    }

}
