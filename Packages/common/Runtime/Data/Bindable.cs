using UnityEngine;

namespace Yu5h1Lib
{

    public abstract class Bindable : BaseMonoBehaviour, IBindable
    {
        public virtual string GetFieldName() => gameObject.name;
        public abstract string GetValue();
        public abstract void SetValue(string value);        
        public void SetValue(Object bindable)
        { 
            if (bindable is IBindable Ibindable)
                SetValue(Ibindable.GetValue());
        }
    }
    public interface IBindable
    {
        string GetFieldName();
        string GetValue();
        void SetValue(string value);
        void SetValue(Object Ibindable);
    }
}
