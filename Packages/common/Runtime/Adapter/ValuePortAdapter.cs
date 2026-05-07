using System;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Common;
using Yu5h1Lib.MVVM;

namespace Yu5h1Lib
{
    public interface IValuePortAdapter<TValue> : UValuePort<TValue>, IAdapter<Component> { }
    public abstract class ValuePortAdapter<T,TValue> : OpsBase<T>, IValuePortAdapter<TValue> where T : Component
    {
        public abstract TValue value { get; set; }

        protected ValuePortAdapter(T component) : base(component) {}

        public virtual string GetFieldName() => Raw.gameObject.name;
        public void SetValue(IValuePort Ibindable) => SetValue(Ibindable.GetValue());

        public virtual string GetValue() => value.ToString();
        public abstract void SetValue(string value);
        public abstract event UnityAction<TValue> ChangedCallback;

        private UnityAction<TValue> ReadFromThis;
        public void BindTo(IDataView dataview)
        {
            Unbind();
            ReadFromThis = _ => dataview.ReadFrom(this);
            ChangedCallback += ReadFromThis;
        }
        public void Unbind()
        {
            if (ReadFromThis == null) return;
            ChangedCallback -= ReadFromThis;
            ReadFromThis = null;
        }
        public void Set(TValue value) => this.value = value;
        public TValue Get() => value;
    }
}
