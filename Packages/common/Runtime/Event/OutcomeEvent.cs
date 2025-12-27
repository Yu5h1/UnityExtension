using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Yu5h1Lib
{
    [System.Serializable]
    public class OutcomeEvent : OutcomeRouter<UnityEvent>
    {
        [FormerlySerializedAs("finished")]
        public UnityEvent<bool> completed;
        protected override void InvokeMethod(UnityEvent e) => e?.Invoke();
        public override UnityEvent ResetHandler(UnityEvent h) 
        {
            h.RemoveAllListeners();
            return h;
        }
        public override void Invoke(bool success)
        {
            base.Invoke(success);
            completed?.Invoke(success);
        }
        public override void Clear()
        {
            base.Clear();
            completed.RemoveAllListeners();
        }
    }

    [System.Serializable,MovedFrom("ResultEvent")]
    public class OutcomeEvent<TParam> : OutcomeRouter<UnityEvent<TParam>,TParam>
    {
   
        [FormerlySerializedAs("finished")]
        public UnityEvent<bool> completed;

        protected override void InvokeMethod(UnityEvent<TParam> e,TParam p) => e?.Invoke(p);
        public override UnityEvent<TParam> ResetHandler(UnityEvent<TParam> h)
        {
            h.RemoveAllListeners();
            return h;
        }

        public override void Invoke(bool result, TParam p)
        {
            base.Invoke(result, p);
            completed?.Invoke(result);
        }
        public override void Clear()
        {
            base.Clear();
            completed.RemoveAllListeners();
        }
    } 
}
