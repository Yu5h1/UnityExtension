using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    public abstract class OptionSet : Bindable, IBindable
    {
        [SerializeField] protected OptionSelector selector;
        public abstract int Count { get; }
        public abstract void Select(int index);
        public abstract string GetItemName(int index);
    }
    public abstract class OptionSet<T> : OptionSet , IBindable
    {
        public T current
        { 
            get => Items.IsValid(selector.current) ? Items[selector.current] : default(T);
            set
            {
                if (current.Equals(value))
                    return;
                int index = Items.IndexOf(value);
                if (Items.IsValid(index))
                    selector.current = index;
            }
        } 
        [SerializeField] protected List<T> _Items;
        public virtual List<T> Items { get => _Items; private set => _Items = value; }

        [SerializeField] private UnityEvent<T> _OptionChanged;
        public event UnityAction<T> optionChanged
        {
            add => _OptionChanged.AddListener(value);
            remove => _OptionChanged.RemoveListener(value);
        }

        public override int Count => Items.Count;

        protected override void OnInitializing() { }

        public override void Select(int index)
        {
            if (!Items.IsValid(index))
                return;
            _OptionChanged?.Invoke(Items[index]);
            OnSelected(Items[index]);
        }
        protected virtual void OnSelected(T current) {}
        public void InvokeOptionChanged() => Select(selector.current);

        public virtual string ToString(T item)
        {
            switch (item)
            {
                case string str: return str;
                case Object obj: return obj.name;
            }
            return item?.ToString() ?? string.Empty;
        }

        public override string GetValue() => ToString(current);
        public override void SetValue(string value) 
        {
            if (value.IsEmpty())
                return;
            int index = Items.FindIndex(item => ToString(item) == value);
            if (index >= 0)
                selector.current = index;
            else
                Debug.LogWarning($"Value '{value}' not found in option set.");
        }
        public override string GetItemName(int index) => ToString(Items[index]);
    } 
}
