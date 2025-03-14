using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class BehaviourEx
    {
        public static bool IsAvailable(this Behaviour b) => b != null && b.isActiveAndEnabled;
    }
}
