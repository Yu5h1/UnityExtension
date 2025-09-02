using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using ArgumentNullException = System.ArgumentNullException;

namespace Yu5h1Lib.Serialization
{
    [System.Serializable]
    public class KeyValues<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField, HorizontalScope]
        private List<KeyValue<TKey, TValue>> _entries = new List<KeyValue<TKey, TValue>>();

        public IReadOnlyList<KeyValue<TKey, TValue>> Entries => _entries.AsReadOnly();

        #region IDictionary Implementation

        public TValue this[TKey key]
        {
            get
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (EqualityComparer<TKey>.Default.Equals(_entries[i].Key, key))
                        return _entries[i].Value;
                }
                throw new KeyNotFoundException($"Key '{key}' was not found.");
            }
            set
            {
                if (key == null) throw new ArgumentNullException(nameof(key));

                for (int i = 0; i < _entries.Count; i++)
                {
                    if (EqualityComparer<TKey>.Default.Equals(_entries[i].Key, key))
                    {
                        var oldValue = _entries[i].Value;
                        if (!EqualityComparer<TValue>.Default.Equals(oldValue, value))
                        {
                            _entries[i] = new KeyValue<TKey, TValue>(key, value);
                            OnValueChanged(key, oldValue, value);
                        }
                        return;
                    }
                }
                _entries.Add(new KeyValue<TKey, TValue>(key, value));
                OnAdded(key, value);
            }
        }

        public ICollection<TKey> Keys => _entries.Select(entry => entry.Key).ToList();
        public ICollection<TValue> Values => _entries.Select(entry => entry.Value).ToList();
        public int Count => _entries.Count;
        public bool IsReadOnly => false;


        public KeyValues()
        {
            _entries = new List<KeyValue<TKey, TValue>>();
        }

        public KeyValues(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            _entries = new List<KeyValue<TKey, TValue>>();

            if (collection != null)
            {
                var seenKeys = new HashSet<TKey>();
                foreach (var kvp in collection)
                {
                    if (kvp.Key != null && seenKeys.Add(kvp.Key))
                    {
                        _entries.Add(new KeyValue<TKey, TValue>(kvp.Key, kvp.Value));
                    }
                }
            }
        }

        public KeyValues(IDictionary<TKey, TValue> dictionary)
        {
            _entries = new List<KeyValue<TKey, TValue>>();

            if (dictionary != null)
            {
                foreach (var kvp in dictionary)
                {
                    if (kvp.Key != null)
                    {
                        _entries.Add(new KeyValue<TKey, TValue>(kvp.Key, kvp.Value));
                    }
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (ContainsKey(key)) throw new System.ArgumentException($"Key '{key}' already exists.");

            _entries.Add(new KeyValue<TKey, TValue>(key, value));
            OnAdded(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public bool Remove(TKey key)
        {
            if (key == null) return false;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (EqualityComparer<TKey>.Default.Equals(_entries[i].Key, key))
                {
                    var value = _entries[i].Value;
                    _entries.RemoveAt(i);
                    OnRemoved(key, value);
                    return true;
                }
            }
            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (EqualityComparer<TKey>.Default.Equals(entry.Key, item.Key) &&
                    EqualityComparer<TValue>.Default.Equals(entry.Value, item.Value))
                {
                    _entries.RemoveAt(i);
                    OnRemoved(item.Key, item.Value);
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            if (_entries.Count > 0)
            {
                var itemsToRemove = new List<KeyValue<TKey, TValue>>(_entries);
                _entries.Clear();

                foreach (var item in itemsToRemove)
                {
                    OnRemoved(item.Key, item.Value);
                }
                OnCleared();
            }
        }

        public bool ContainsKey(TKey key)
        {
            if (key == null) return false;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (EqualityComparer<TKey>.Default.Equals(_entries[i].Key, key))
                    return true;
            }
            return false;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (EqualityComparer<TKey>.Default.Equals(entry.Key, item.Key) &&
                    EqualityComparer<TValue>.Default.Equals(entry.Value, item.Value))
                    return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key != null)
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (EqualityComparer<TKey>.Default.Equals(_entries[i].Key, key))
                    {
                        value = _entries[i].Value;
                        return true;
                    }
                }
            }

            value = default(TValue);
            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length) throw new System.ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < _entries.Count) throw new System.ArgumentException("Array is too small.");

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                array[arrayIndex + i] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
            }
        }

        #endregion

        #region Enumerators

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var entry in _entries)
            {
                yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Event Callbacks

        protected virtual void OnValueChanged(TKey key, TValue oldValue, TValue newValue) { }
        protected virtual void OnAdded(TKey key, TValue value) { }
        protected virtual void OnRemoved(TKey key, TValue value) { }
        protected virtual void OnCleared() { }

        #endregion

        #region Unity Serialization

        public void OnBeforeSerialize()
        {
            // List 已經是序列化狀態，不需要額外操作
        }

        public void OnAfterDeserialize()
        {
            // 檢查並移除重複的 key（防止手動編輯 Inspector 造成的問題）
            var uniqueEntries = new List<KeyValue<TKey, TValue>>();
            var seenKeys = new HashSet<TKey>();

            foreach (var entry in _entries)
            {
                if (entry.Key != null && seenKeys.Add(entry.Key))
                {
                    uniqueEntries.Add(entry);
                }
            }

            if (uniqueEntries.Count != _entries.Count)
            {
                _entries = uniqueEntries;
                Debug.LogWarning("KeyValues: Duplicate keys found and removed during deserialization.");
            }
        }

        #endregion

        #region Additional Utility Methods

        /// <summary>
        /// 嘗試添加鍵值對，如果 key 已存在則不添加
        /// </summary>
        public bool TryAdd(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (!ContainsKey(key))
            {
                _entries.Add(new KeyValue<TKey, TValue>(key, value));
                OnAdded(key, value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 從另一個 KeyValues 複製數據
        /// </summary>
        public void CopyFrom(KeyValues<TKey, TValue> other)
        {
            if (other == null) return;

            Clear();
            foreach (var entry in other._entries)
            {
                _entries.Add(new KeyValue<TKey, TValue>(entry.Key, entry.Value));
            }
        }
        public void CopyFrom(IEnumerable<KeyValue<TKey, TValue>> other)
        {
            if (other == null) return;
            Clear();
            foreach (var kvp in other)
                this[kvp.Key] = kvp.Value;
        }
        /// <summary>
        /// 安全的獲取值方法，如果 key 不存在返回默認值
        /// </summary>
        public TValue GetValueOrDefault(TKey key, TValue defaultValue = default(TValue))
        {
            return TryGetValue(key, out TValue value) ? value : defaultValue;
        }

        #endregion

    }
    [System.Serializable]
    public class KeyValue<TKey, TValue>
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
        public void CopyFrom(KeyValue<TKey, TValue> other)
        {
            key = other.key;
            value = other.value;
        }
        public void SetValue(TValue newValue) => value = newValue;
        public override string ToString() => $"{{\"{key}\", {value}}}";
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
