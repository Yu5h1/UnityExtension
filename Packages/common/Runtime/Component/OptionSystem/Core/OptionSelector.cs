using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib
{
    [DisallowMultipleComponent]
    public class OptionSelector : BaseMonoBehaviour {
        [SerializeField] private OptionSet _OptionSet;
        public OptionSet optionSet { get => _OptionSet; set => _OptionSet = value; }
        public int Count => optionSet.Count;
        [SerializeField] private int _current = -1;
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

                if (skipIndices.Contains(value))
                {
                    var interval = value > _current || value == 0 && _current == Count - 1  ? 1 : -1;
                    if (!TryFindNextValidIndex(value, out int next, interval))
                        return;
                    value = next;
                }
                _current = value;

                optionSet.Select(value);
                _selectionChanged?.Invoke(value);
                if (binding is IValuePort bindable)
                    bindable.SetValue(optionSet.GetValue());
            }
        }
        public int[] skipIndices;
        protected override void OnInitializing()
        {
            
        }

        [SerializeField] private UnityEvent<int> _selectionChanged;
        public event UnityAction<int> selectionChanged
        {
            add => _selectionChanged.AddListener(value);
            remove => _selectionChanged.RemoveListener(value);
        }
        private bool TryFindNextValidIndex(int startIndex,out int result, int interval = 1)
        {
            result = startIndex;
            for (int i = 0; i < Count; i++)
            {
                int index = (startIndex + i + interval) % Count;
                if (!skipIndices.Contains(index))
                {
                    result = index;
                    return true;
                }
            }
            return false;
        }

        public void MoveNext()
        {
            current++;
        }
        public void MovePrevious()
            => current = (current - 1) < 0 ? Count - 1 : current - 1;

    }

}