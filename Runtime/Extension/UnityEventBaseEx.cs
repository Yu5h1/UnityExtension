using UnityEngine.Events;

namespace Yu5h1Lib
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static class UnityEventBaseEx
    {
        public static bool IsEmpty(this UnityEventBase e)
            => e == null || e.GetPersistentEventCount() == 0;
    }
}
