using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.MVVM;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public abstract class OptionSet : ValuePort
    {
        [SerializeField] protected OptionSelector selector;
        public abstract int Count { get; }
        public void Select(int index) => TrySelect(index);
        public bool TrySelect(int index)
        {
            if (!CanSelect(index))
                return false;
            OnSelected(index);
            return true;
        }
        protected abstract void OnSelected(int index);
        public abstract bool CanSelect(int index);
        public abstract bool TryGetItemText(int index,out string text);
        public void print(ValuePort port) => $"OptionSet: {gameObject.name} Value: {port.GetValue()}".print();
    }
    public abstract class OptionSet<TValue> : OptionSet , IValuePort<TValue>
    {
        [ReadOnly]public string CurrentItemDisplayName;
        public TValue current
        { 
            get => Items.IsValid(selector.current) ? Items[selector.current] : default;
            set
            {
                if (EqualityComparer<TValue>.Default.Equals(current, value)) 
                    return;
                int index = Items.IndexOf(value);
                if (!Items.IsValid(index))
                    return;
                selector.current = index;
            }
        }
        [SerializeField, Inline(UseEnhancedReorderableList = true)] protected List<TValue> _Items;
        public virtual List<TValue> Items { get => _Items; protected set => _Items = value; }

        [SerializeField] private UnityEvent<TValue> _OptionChanged;
        private UnityAction _optionChanged;
        public event UnityAction<TValue> optionChanged
        {
            add => _OptionChanged.AddListener(value);
            remove => _OptionChanged.RemoveListener(value);
        }

        public override event UnityAction ChangedCallback
        {
            add => _optionChanged += value;
            remove => _optionChanged -= value;
        }
        public override int Count => Items.Count;

        #region ValuePort
        public TValue value { get => current; set => current = value; }
        TValue IGetter<TValue>.Get() => current;
        public void Set(TValue value) => this.value = value;
        public override string GetValue() => ToString(current);
        #endregion
        protected override void OnInitializing() { }

        public override bool CanSelect(int index) => Items.IsValid(index);

        protected override void OnSelected(int index)
        {
            _OptionChanged?.Invoke(Items[index]);
            _optionChanged?.Invoke();
            if (TryGetItemText(index, out string text))
                CurrentItemDisplayName = text;
        }
        public void InvokeOptionChanged() => Select(selector.current);

        public virtual string ToString(TValue item)
        {            
            switch (item)
            {
                case string str: return str;
                case Object obj: return obj.name;
            }
            return item?.ToString() ?? string.Empty;
        }
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
        public override bool TryGetItemText(int index, out string text)
        {
            text = string.Empty;
            if ($"{gameObject.name} index ({index}) is invalid.".printWarningIf(!Items.IsValid(index)))
                return false;

            text = ToString(Items[index]);
            return true;
        }

    }
}
