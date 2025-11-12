using UnityEngine;

namespace Yu5h1Lib
{
    public static class ApplicationInfo
    {
        public static bool WantsToQuit { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            WantsToQuit = false;
            Application.wantsToQuit += () =>
            {
                WantsToQuit = true;
                return true;
            };
        }
    }
}
