using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Yu5h1Lib
{
    [System.Serializable][MovedFrom("OutcomeEvent")]
    public class BinaryEvent : BinaryEventBase<UnityEvent>
    {
        protected override void InvokeMethod(UnityEvent e) => e?.Invoke();
        public override UnityEvent ResetHandler(UnityEvent h) 
        {
            h.RemoveAllListeners();
            return h;
        }
    }

    [System.Serializable,MovedFrom("OutcomeEvent")]
    public class BinaryEvent<TParam> : BinaryEventBase<UnityEvent<TParam>,TParam>
    {
        protected override void InvokeMethod(UnityEvent<TParam> e,TParam p) => e?.Invoke(p);
        public override UnityEvent<TParam> ResetHandler(UnityEvent<TParam> h)
        {
            h.RemoveAllListeners();
            return h;
        }
    } 
}
