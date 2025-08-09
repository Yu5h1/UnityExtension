using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArgumentNullException = System.ArgumentNullException;

namespace Yu5h1Lib.Serialization
{
    [System.Serializable]
    public class KeyValues<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField,HorizontalScope] private List<KeyValue<TKey, TValue>> items = new List<KeyValue<TKey, TValue>>();

        private bool _isDirty = false;
        private int _lastKnownCount = 0;

        #region refactor 
        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
                if (key == null) throw new ArgumentNullException(nameof(key));

                if (base.TryGetValue(key, out TValue oldValue))
                {
                    if (EqualityComparer<TValue>.Default.Equals(oldValue, value))
                        return;

                    base[key] = value;
                    OnValueChanged(key, oldValue, value);
                }
                else
                {
                    base[key] = value;
                    OnAdded(key, value);
                }
                MarkDirty();
            }
        }
        public new void Add(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            base.Add(key, value);
            OnAdded(key, value);
            MarkDirty();
        }
        public new bool Remove(TKey key)
        {
            if (key == null) return false;

            if (base.TryGetValue(key, out TValue value))
            {
                bool removed = base.Remove(key);
                if (removed)
                {
                    OnRemoved(key, value);
                    MarkDirty();
                }
                return removed;
            }
            return false;
        }
        public new void Clear()
        {
            if (Count > 0)
            {
                var itemsToRemove = new List<KeyValuePair<TKey, TValue>>(this);
                base.Clear();

                foreach (var kvp in itemsToRemove)
                {
                    OnRemoved(kvp.Key, kvp.Value);
                }
                OnCleared();
                MarkDirty();
            }
        }

        public new bool TryAdd(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (!ContainsKey(key))
            {
                base[key] = value;
                OnAdded(key, value);
                MarkDirty();
                return true;
            }
            return false;
        }

        #endregion

        #region 

        protected virtual void OnValueChanged(TKey key, TValue oldValue, TValue newValue) { }
        protected virtual void OnAdded(TKey key, TValue value) { }
        protected virtual void OnRemoved(TKey key, TValue value) { }
        protected virtual void OnCleared() { }

        #endregion

        #region 序列化處理

        public void OnBeforeSerialize()
        {
            // 智能檢測：只有在字典被修改時才同步
            if (_isDirty || Count != _lastKnownCount)
            {
                SyncToSerialized();
                _isDirty = false;
                _lastKnownCount = Count;
            }
        }

        public void OnAfterDeserialize()
        {
            SyncFromSerialized();
            _isDirty = false;
            _lastKnownCount = Count;
        }

        private void SyncToSerialized()
        {
            items.Clear();
            foreach (var kvp in this)
            {
                items.Add(new KeyValue<TKey, TValue>(kvp.Key, kvp.Value));
            }
        }

        private void SyncFromSerialized()
        {
            this.Clear();

            foreach (var item in items)
            {
                if (item.Key == null)
                    continue;
                base[item.Key] = item.Value;
            }
        }

        private void MarkDirty()
        {
            _isDirty = true;
        }

        #endregion

    }
    [System.Serializable]
    public struct KeyValue<TKey, TValue>
    {
        [SerializeField] private TKey key;
        [SerializeField] private TValue value;

        public TKey Key => key;
        public TValue Value => value;

        public KeyValue(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }
        public void SetValue(TValue newValue) => value = newValue;
        public override string ToString() => "[{key}, {value}]";
        public override bool Equals(object obj)
        {
            if (obj is KeyValue<TKey, TValue> other)
            {
                return EqualityComparer<TKey>.Default.Equals(key, other.key) &&
                       EqualityComparer<TValue>.Default.Equals(value, other.value);
            }
            return false;
        }

        public override int GetHashCode() => System.HashCode.Combine(key, value);
    } 
}
