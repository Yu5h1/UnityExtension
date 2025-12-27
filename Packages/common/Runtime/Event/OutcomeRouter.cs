using UnityEngine;
using UnityEngine.Serialization;

namespace Yu5h1Lib
{
    [System.Serializable]
    public abstract class OutcomeRouter<THandler> : RouteBase
    {
        [FormerlySerializedAs("success")]
        public THandler succeeded;
        [FormerlySerializedAs("failure")]
        public THandler failed;

        protected abstract void InvokeMethod(THandler h);
        public abstract THandler ResetHandler(THandler h);

        public virtual void Invoke(bool success) => InvokeMethod(success ? succeeded : failed);

        public virtual void Clear()
        {
            succeeded = ResetHandler(succeeded);
            failed = ResetHandler(failed);
        }
    }
    [System.Serializable]
    public abstract class OutcomeRouter<THandler,TParam> : OutcomeRouter<THandler>
    {
        protected override void InvokeMethod(THandler h)
            => throw new System.NotSupportedException("Use InvokeMethod(bool, TParam) instead.");
        public override void Invoke(bool success)
            => throw new System.NotSupportedException("Use Invoke(bool, TParam) instead.");
        protected abstract void InvokeMethod(THandler h,TParam p);
        public virtual void Invoke(bool success, TParam p) => InvokeMethod(success ? succeeded : failed,p);
    }
}
