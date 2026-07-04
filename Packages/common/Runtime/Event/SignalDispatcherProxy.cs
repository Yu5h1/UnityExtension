using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Inspector bridge for sending string signals from prefab-authored UnityEvents.
    /// </summary>
    [AddComponentMenu("Yu5h1Lib/Signal Dispatcher Proxy"), DisallowMultipleComponent]
    public class SignalDispatcherProxy : BaseMonoBehaviour
    {
        protected override void OnInitializing() {}

        public void Dispatch(Object obj)
        {
            if (!(obj is IParameter parameter))
                return;

            SignalDispatcher.instance.Dispatch(parameter.name, obj);
        }
        public void Dispatch(string signal)
        {
            SignalDispatcher.instance.Dispatch(signal, null);
        }
    }
}
