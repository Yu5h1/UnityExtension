using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Inspector bridge for sending string signals from prefab-authored UnityEvents.
    /// </summary>
    public class BroadcasterProxy : BaseMonoBehaviour
    {
        protected override void OnInitializing() {}

        public void Dispatch(string signal)
        {
            Broadcaster.instance.Dispatch(signal);
        }
    }
}
