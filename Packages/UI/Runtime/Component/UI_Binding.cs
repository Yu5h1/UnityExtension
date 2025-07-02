using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    public abstract class UI_Binding : UIControl
    {
        public abstract string GetFieldName();
        public abstract string GetValue();
        public abstract void SetValue(string value);
    }
    public abstract class UI_Binding<T> : UI_Binding where T : Behaviour
    {
		[SerializeField] private T _target;
		public T target => _target;

        public override string GetFieldName() => target.gameObject.name;

        protected override void OnInitializing()
        {
            base.OnInitializing();
            TryGetComponent(out _target);
        }
    }
}
