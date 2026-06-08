using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Yu5h1Lib.Common;
using Yu5h1Lib.MVVM;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib
{
    [DisallowMultipleComponent]
    public class OptionSelector : BaseMonoBehaviour
    {
        [SerializeField] private OptionSet _OptionSet;
        public OptionSet optionSet { get => _OptionSet; set => _OptionSet = value; }
        public int Count => optionSet.Count;
        [SerializeField] private int _current = -1;
        [SerializeField] private Object binding;
        [SerializeField, FormerlySerializedAs("skipIndices")] private int[] _excludeIndices;
        public int[] excludeIndices { get => _excludeIndices; private set => _excludeIndices = value; }

        private bool _syncing;
        public int current
        {
            get => _current;
            set
            {
                if (_syncing || Count == 0)
                    return;

                _syncing = true;
                try
                {
                    value %= Count;
                    if (value < 0)
                        value = Count - 1;

                    if (excludeIndices.Contains(value))
                    {
                        var interval = value > _current || (value == 0 && _current == Count - 1) ? 1 : -1;
                        if (!TryFindNextValidIndex(value, out int next, interval))
                            return;
                        value = next;
                    }

                    if (_current == value)
                        return;

                    _current = value;

                    optionSet.Select(value);
                    _selectionChanged?.Invoke(value);
                    if (binding is IValuePort port)
                        port.SetValue(optionSet.GetValue());
                }
                finally
                {
                    _syncing = false;
                }
            }
        }



        protected override void OnInitializing() {}

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
                int index = (startIndex + interval + i) % Count;
                if (index < 0) index += Count;

                if (!excludeIndices.Contains(index))
                {
                    result = index;
                    return true;
                }
            }
            return false;
        }
        public void Select(bool value) => current = value ? 1 : 0;
        public void Select(int index) => current = index;
        [ContextMenu(nameof(MoveNext))]
        public void MoveNext() => current++;
        [ContextMenu(nameof(MovePrevious))]
        public void MovePrevious() => current--;


        /// <summary>
        /// Pick a random index excluding the current one and any in <see cref="excludeIndices"/>.
        /// No-op when there is no valid candidate.
        /// </summary>
        [ContextMenu(nameof(RandomCurrent))]
        public void RandomCurrent()
        {
            if (Count == 0) return;
            // _current = -1 (uninitialized) is harmlessly ignored — out of [0, Count) range.
            if (RandomEx.TryRandomInt(0, Count, out int idx, excludeIndices, _current))
                current = idx;
        }

    }

}