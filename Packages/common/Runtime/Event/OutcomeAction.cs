using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    public class OutcomeAction : OutcomeRouter<UnityAction>
    {
        public bool clearAfterInvoke = true;
        public UnityAction<bool> completed;
        protected override void InvokeMethod(UnityAction h) => h?.Invoke();
        public override UnityAction ResetHandler(UnityAction h) => h = null;

        public override void Invoke(bool success)
        {
            base.Invoke(success);
            completed?.Invoke(success);
            if(clearAfterInvoke)
                Clear();
        }
        public override void Clear()
        {
            base.Clear();
            completed = null;
        }
    }
}
