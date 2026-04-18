using UnityEngine;
using static Yu5h1Lib.GameObjectUtility;

namespace Yu5h1Lib
{
    public abstract class SingletonBehaviour : BaseMonoBehaviour 
    {
        public interface IAllowEditMode { }
    }
    public abstract class SingletonBehaviour<T> : SingletonBehaviour where T : SingletonBehaviour<T>
    {
        static bool AllowEditMode => typeof(IAllowEditMode).IsAssignableFrom(typeof(T));
        protected static T _instance;
        /// <summary>
        /// Check Resources first, and create it from there if it exists; if not, make a new one.;
        /// </summary>
        public static T instance
        {
            get
            {
                EnsureInstance(ref _instance, i => i.OnInstantiated());
                return _instance;
            }
        }
        protected abstract void OnInstantiated();

        public static bool Exists() => !Object.Equals(_instance, null);

        public static void RemoveInstanceCache()
        {
            _instance = null;
        }

        public static bool EnsureInstance<T>(ref T instance,System.Action<T> instantiated) where T : Component
        {
            if (instance != null)
                return true;
            if (!TryFind(out instance, includeInactive: true))
            {
                if ($"[Singleton] Prevented creation of {typeof(T).Name} during application quit.".printWarningIf(ApplicationInfo.WantsToQuit))
                    return false;
                if ($"Singleton<{typeof(T).Name}> stops creating instance in Edit Mode.".printWarningIf(!Application.isPlaying && !AllowEditMode))
                    return false;
                if (!ResourcesUtility.TryInstantiateFromResources(out instance, typeof(T).Name, null))
                    instance = Create<T>();
                if (instance != null)
                    instantiated?.Invoke(instance);
            }
            return instance != null;
        }
    }

}
