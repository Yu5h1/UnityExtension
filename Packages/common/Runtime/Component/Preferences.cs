using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public abstract class Preferences<T> : SingletonBehaviour<T> where T : Preferences<T>
    {
        public string KEY => GetType().Name;
        [SerializeField,TypeRestriction(typeof(IValuePort))] private List<Object> _bindings;
        public IReadOnlyList<Object> bindings => _bindings;

        [SerializeField] protected DataView defaultSetting;

        [SerializeField, ReadOnly] private DataView _current = new DataView();
        public DataView current => _current;

        [SerializeField] private UnityEvent _DataUpdated;

        public bool LoadFromPlayerPrefsOnAwake = false;
        public bool SaveOnChanged = false;

        public bool TryGetValueFromBindings(string key,out string value)
        {
            value = default;
            foreach (var obj in bindings)
            {
                if (obj is IValuePort port && port.GetFieldName() == key)
                {
                    value = port.GetValue();
                    return true;
                }
            }
            $"Key [{key}] not found in bindings.".printWarning();
            return false;
        }

        public string GetValueFromBindings(string key) => TryGetValueFromBindings(key, out string value) ? value : default;

        protected override void OnInstantiated() { }

        protected override void OnInitializing()
        {
            if (current.IsEmpty())
                current.CopyFrom(defaultSetting);
            if (LoadFromPlayerPrefsOnAwake)
                LoadFromPlayerPrefs();
        }
        public void WriteToBindings()
        { 
            foreach (var obj in _bindings)
                WriteTo(obj);
        }
        public void ReadFromBindings()
        {
            foreach (var obj in _bindings)
                ReadFrom(obj);
        }

        public virtual void SaveToPlayerPrefs()
        {
            PlayerPrefs.SetString(GetType().Name, current.ToJson());
            PlayerPrefs.Save();
        }
        public virtual bool LoadFromPlayerPrefs()
        {
            if (!PlayerPrefs.HasKey(KEY))
                return false;
            if (DataView.TryParseFromJson(PlayerPrefs.GetString(KEY), out DataView data))
            { 
                current.CopyFrom(data);
                WriteToBindings();
                return true;
            }
            else
            {
                $"Failed to parse preferences from PlayerPrefs with key [{KEY}]".printWarning();
                return false;
            }
        }


        public void WriteTo(Object obj) => current.WriteTo(obj);
        public void ReadFrom(Object obj)
        {
            if (current.TryReadFrom(obj))
            {
                if (SaveOnChanged)
                    SaveToPlayerPrefs();
                _DataUpdated?.Invoke();

            }
        }

        [System.Obsolete("Use WriteTo instead")]
        public void GetProperty(Object obj) => WriteTo(obj);
        [System.Obsolete("Use ReadFrom instead")]
        public void SetProperty(Object obj) => ReadFrom(obj);
  

    }
}