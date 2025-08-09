using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib
{
    [DisallowMultipleComponent]
    public class OptionSelector : BaseMonoBehaviour {
        [SerializeField] private OptionSetBase _OptionSet;
        public OptionSetBase optionSet { get => _OptionSet; set => _OptionSet = value; }
        public int Count => optionSet.Count;
        [SerializeField,ReadOnly] private int _current;
        [SerializeField] private Object binding;
        public int current
        {
            get => _current;
            set
            {
                if (_current == value || Count == 0)
                    return;
                value %= Count;
                if (value < 0)
                    value = Count - 1;
                _current = value;

                optionSet.Select(value);
                _selectionChanged?.Invoke(value);
                if (binding is IBindable bindable)
                    bindable.SetValue(optionSet.GetValue());
            }
        }
        protected override void OnInitializing()
        {
            
        }

        [SerializeField] private UnityEvent<int> _selectionChanged;
        public event UnityAction<int> selectionChanged
        {
            add => _selectionChanged.AddListener(value);
            remove => _selectionChanged.RemoveListener(value);
        }
        public void MoveNext()
        {
            current++;
        }
        public void MovePrevious()
            => current = (current - 1) < 0 ? Count - 1 : current - 1;

    }

}