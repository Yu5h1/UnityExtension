// Advances the shared Core tween runners with Unity's scaled and unscaled clocks.
using UnityEngine;

namespace Yu5h1Lib.Motion
{
    [AddComponentMenu("")]
    internal sealed class UnityTweenRunner : MonoBehaviour
    {
        private static UnityTweenRunner _instance;
        private readonly ReferenceRunner _scaled = new ReferenceRunner();
        private readonly ReferenceRunner _unscaled = new ReferenceRunner();
        private readonly Timer.ISource _scaledClock = Timer.Scaled;
        private readonly Timer.ISource _unscaledClock = Timer.Unscaled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => _instance = null;

        internal static ReferenceRunner GetRunner(bool unscaled)
        {
            if (!_instance)
            {
                var host = new GameObject("Motion Tween Runner");
                DontDestroyOnLoad(host);
                _instance = host.AddComponent<UnityTweenRunner>();
            }

            return unscaled ? _instance._unscaled : _instance._scaled;
        }

        private void Update()
        {
            _scaled.Tick(_scaledClock.deltaTime);
            _unscaled.Tick(_unscaledClock.deltaTime);
        }
    }
}
