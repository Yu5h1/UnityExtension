using UnityEngine;
using UnityEngine.Serialization;

namespace Yu5h1Lib
{
    [System.Serializable]
    public abstract class BinaryEventBase<THandler> : EventContainer
    {
        [SerializeField] protected THandler _true;
        [SerializeField] protected THandler _false;

        protected abstract void InvokeMethod(THandler h);
        public abstract THandler ResetHandler(THandler h);

        public virtual void Invoke(bool success) => InvokeMethod(success ? _true : _false);

        public virtual void Clear()
        {
            _true = ResetHandler(_true);
            _false = ResetHandler(_false);
        }
    }
    [System.Serializable]
    public abstract class BinaryEventBase<THandler,TParam> : BinaryEventBase<THandler>
    {
        protected override void InvokeMethod(THandler h)
            => throw new System.NotSupportedException("Use InvokeMethod(bool, TParam) instead.");
        public override void Invoke(bool success)
            => throw new System.NotSupportedException("Use Invoke(bool, TParam) instead.");
        protected abstract void InvokeMethod(THandler h,TParam p);
        public virtual void Invoke(bool success, TParam p) => InvokeMethod(success ? _true : _false,p);
    }
}
