using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Yu5h1Lib;

namespace  Yu5h1Lib.UI
{
    public class UI_Selection : UI_Selection<string>
    {
        private void Start() { }
    }

    public abstract class UI_Selection<T> : UIControl {
        [SerializeField] private List<T> _Items;
        public List<T> Items => _Items;
        private int _current;
        public int current
        {
            get => _current;
            private set
            {
                if (_current == value)
                    return;
                _current = value;

                current %= Items.Count;
                if (current < 0)
                    current = Items.Count - 1;

                _selectionChanged?.Invoke(Items[current]);
            }
        }
        public T CurrentItem => Items.IsValid(current) ? Items[current] : default(T);

        [SerializeField] private UnityEvent<T> _selectionChanged;
        public event UnityAction<T> selectionChanged
        {
            add => _selectionChanged.AddListener(value);
            remove => _selectionChanged.RemoveListener(value);
        }

        public void SetCurrent(T item)
        {
            int index = Items.IndexOf(item);
            if (index >= 0)
                current = index;
        }
        public void MoveNext()
        {
            current++;
        }
        public void MovePrevious()
        {
            current = (current - 1) < 0 ? Items.Count - 1 : current - 1;
        }
    }

}