using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    public class BinaryAction : BinaryEventBase<UnityAction>
    {
        public bool clearAfterInvoke = true;
        protected override void InvokeMethod(UnityAction h) => h?.Invoke();
        public override UnityAction ResetHandler(UnityAction h) => h = null;

        public override void Invoke(bool success)
        {
            base.Invoke(success);
            if(clearAfterInvoke)
                Clear();
        }
    }
}
