using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public abstract class OptionSet : ValuePort, IValuePort
    {

        [SerializeField] protected OptionSelector selector;
        public abstract int Count { get; }
        public abstract void Select(int index);
        public abstract string GetItemText(int index);
        public void print(ValuePort port) => $"OptionSet: {gameObject.name} Value: {port.GetValue()}".print();
    }
    public abstract class OptionSet<T> : OptionSet , IValuePort
    {
        [ReadOnly]public string CurrentItemDisplayName;
        public T current
        { 
            get => Items.IsValid(selector.current) ? Items[selector.current] : default;
            set
            {
                if (EqualityComparer<T>.Default.Equals(current, value)) 
                    return;
                int index = Items.IndexOf(value);
                if (!Items.IsValid(index))
                    return;
                selector.current = index;
            }
        }
        [SerializeField, Inline(UseEnhancedReorderableList = true)] protected List<T> _Items;
        public virtual List<T> Items { get => _Items; protected set => _Items = value; }

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
            OnSelected(index,Items[index]);
            _OptionChanged?.Invoke(Items[index]);
            CurrentItemDisplayName = GetItemText(index);
        }
        protected virtual void OnSelected(int index,T current) { }
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

        public override void SetValue(string value,System.StringComparison comparison)
        {
            if ($"Selector is unassigned.".printWarningIf(selector == null))
                return;
            if ($"{gameObject.name} set empty Value  ".printWarningIf(value.IsEmpty()))
                return;
            int index = Items.FindIndex(item => ToString(item).Equals(value, comparison));
            if (index >= 0)
                current = Items[index];
            else
                Debug.LogWarning($"Value '{value}' not found in option set.");
        }       
        public override string GetItemText(int index) => ToString(Items[index]);
    }
    public abstract class OptionSetValue<TValue> : OptionSet<TValue>, IValuePort<TValue>
    {
        public TValue value { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public void AddListener(UnityAction<TValue> method) => optionChanged += method;
        public void RemoveListener(UnityAction<TValue> method) => optionChanged -= method;
        public abstract bool TryParse(string value, out TValue result);

        private UnityAction<TValue> ReadFromThis;
        public void BindTo(DataView dataview)
        {
            Unbind();
            ReadFromThis = _ => dataview.ReadFrom(this);
            AddListener(ReadFromThis);
        }
        public void Unbind()
        {
            if (ReadFromThis == null) return;
            RemoveListener(ReadFromThis);
            ReadFromThis = null;
        }
    }
}
