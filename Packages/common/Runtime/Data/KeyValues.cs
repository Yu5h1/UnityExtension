using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ArgumentNullException = System.ArgumentNullException;

namespace Yu5h1Lib.Serialization
{
    [System.Serializable]
    public class KeyValues<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<KeyValue<TKey, TValue>> _entries = new List<KeyValue<TKey, TValue>>();

        public IReadOnlyList<KeyValue<TKey, TValue>> Entries => _entries.AsReadOnly();

        #region IDictionary Implementation

        protected virtual bool KeyEquals(TKey a, TKey b) => EqualityComparer<TKey>.Default.Equals(a, b);
        protected virtual bool ValueEquals(TValue a, TValue b) => EqualityComparer<TValue>.Default.Equals(a, b);

        public TValue this[TKey key]
        {
            get
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (KeyEquals(_entries[i].Key, key))
                        return _entries[i].Value;
                }
                throw new KeyNotFoundException($"Key '{key}' was not found.");
            }
            set
            {
                if (key == null) throw new ArgumentNullException(nameof(key));

                for (int i = 0; i < _entries.Count; i++)
                {
                    if (KeyEquals(_entries[i].Key, key))
                    {
                        var oldValue = _entries[i].Value;
                        if (!ValueEquals(oldValue, value))
                        {
                            _entries[i] = new KeyValue<TKey, TValue>(key, value);
                            ItemChanged?.Invoke(key, oldValue, value);
                            Changed?.Invoke();
                        }
                        return;
                    }
                }
                _entries.Add(new KeyValue<TKey, TValue>(key, value));                
                Added?.Invoke(key, value);
                Changed?.Invoke();
            }
        }


        public ICollection<TKey> Keys => _entries.Select(entry => entry.Key).ToList();
        public ICollection<TValue> Values => _entries.Select(entry => entry.Value).ToList();
        public int Count => _entries.Count;
        public bool IsReadOnly => false;

        public event System.Action<TKey, TValue, TValue> ItemChanged;
        public event System.Action<TKey, TValue> Added;
        public event System.Action<TKey, TValue> Removed;
        public event System.Action Cleared;
        public event System.Action Changed;

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
            Added?.Invoke(key, value);
            Changed?.Invoke();
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
                    Removed?.Invoke(key, value);
                    Changed?.Invoke();
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
                    Removed?.Invoke(item.Key, item.Value);
                    Changed?.Invoke();
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
                    Removed?.Invoke(item.Key, item.Value);
                }
                Cleared?.Invoke();
                Changed?.Invoke();
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

 

        #endregion

        #region Unity Serialization

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            // �u���� null key�]reference type �ɡ^
            // ���������ƶ��A���ϥΪ̦b Inspector ���ݨ�ĵ�i�ä�ʭץ�
            if (typeof(TKey).IsClass)
            {
                _entries.RemoveAll(e => e.Key == null);
            }
        }

        #endregion

        #region Validation & Utility Methods

        /// <summary>
        /// �ˬd�O�_�����ƪ� key
        /// </summary>
        public bool HasDuplicateKeys
        {
            get
            {
                var seen = new HashSet<TKey>();
                foreach (var entry in _entries)
                {
                    if (entry.Key != null && !seen.Add(entry.Key))
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// ���o���ƪ� key �C��
        /// </summary>
        public List<TKey> GetDuplicateKeys()
        {
            var seen = new HashSet<TKey>();
            var duplicates = new List<TKey>();

            foreach (var entry in _entries)
            {
                if (entry.Key != null && !seen.Add(entry.Key) && !duplicates.Contains(entry.Key))
                {
                    duplicates.Add(entry.Key);
                }
            }
            return duplicates;
        }

        /// <summary>
        /// �ˬd���w���ު� key �O�_����
        /// </summary>
        public bool IsKeyDuplicateAt(int index)
        {
            if (index < 0 || index >= _entries.Count) return false;

            var targetKey = _entries[index].Key;
            if (targetKey == null) return false;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (i != index && EqualityComparer<TKey>.Default.Equals(_entries[i].Key, targetKey))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// �������ƪ� key�]�O�d�Ĥ@�ӡ^
        /// </summary>
        public void RemoveDuplicates()
        {
            var uniqueEntries = new List<KeyValue<TKey, TValue>>();
            var seenKeys = new HashSet<TKey>();

            foreach (var entry in _entries)
            {
                if (entry.Key != null && seenKeys.Add(entry.Key))
                {
                    uniqueEntries.Add(entry);
                }
            }
            _entries = uniqueEntries;
        }

        /// <summary>
        /// ���ղK�[��ȹ�A�p�G key �w�s�b�h���K�[
        /// </summary>
        public bool TryAdd(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (!ContainsKey(key))
            {
                _entries.Add(new KeyValue<TKey, TValue>(key, value));
                Added?.Invoke(key, value);
                return true;
            }
            return false;
        }

        /// <summary>Replaces the entries with a copy of <paramref name="other"/>'s. Copying from itself keeps them.</summary>
        public void CopyFrom(KeyValues<TKey, TValue> other)
        {
            if (other == null || ReferenceEquals(other, this)) return;

            Clear();
            foreach (var entry in other._entries)
            {
                _entries.Add(new KeyValue<TKey, TValue>(entry.Key, entry.Value));
            }
        }

        /// <summary>Replaces the entries with <paramref name="other"/>'s, read in full before anything is cleared,
        /// so an enumeration over this object (such as <see cref="Entries"/>) is a valid source.</summary>
        public void CopyFrom(IEnumerable<KeyValue<TKey, TValue>> other)
        {
            if (other == null) return;
            var items = other.ToList();
            Clear();
            foreach (var kvp in items)
                this[kvp.Key] = kvp.Value;
        }

        /// <summary>
        /// �w��������Ȥ�k�A�p�G key ���s�b��^�q�{��
        /// </summary>
        public TValue GetValueOrDefault(TKey key, TValue defaultValue = default(TValue))
        {
            return TryGetValue(key, out TValue value) ? value : defaultValue;
        }

        /// <summary>
        /// �ഫ���з� Dictionary�]�۰ʹL�o���ơA�O�d�Ĥ@�ӡ^
        /// </summary>
        public Dictionary<TKey, TValue> ToDictionary()
        {
            var dict = new Dictionary<TKey, TValue>();
            foreach (var entry in _entries)
            {
                if (entry.Key != null && !dict.ContainsKey(entry.Key))
                {
                    dict[entry.Key] = entry.Value;
                }
            }
            return dict;
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

        // Unity �ǦC�ƻݭn�L�Ѽƫغc�l
        public KeyValue() { }

        public KeyValue(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }

        public void SetKey(TKey newKey) => key = newKey;
        public void SetValue(TValue newValue) => value = newValue;

        public void CopyFrom(KeyValue<TKey, TValue> other)
        {
            key = other.key;
            value = other.value;
        }

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