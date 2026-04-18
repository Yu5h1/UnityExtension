using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public abstract class Preferences<T> : SingletonBehaviour<T> where T : Preferences<T>
    {
        public enum InitializeTiming
        {
            None,
            Awake,
            Start,
            Enabled
        }

        public virtual string KEY => GetType().Name;
        [SerializeField,TypeRestriction(typeof(IValuePort))] private List<Object> _bindings;
        public IReadOnlyList<Object> bindings => _bindings;

        [SerializeField] protected DataView defaultSetting;

        [SerializeField, ReadOnly] private DataView _current = new DataView();
        public DataView current => _current;

        [SerializeField] private UnityEvent _DataUpdated;

        public InitializeTiming loadStyle = InitializeTiming.None;

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
            current.CopyFrom(defaultSetting);
            if (current.IsEmpty())
                current.CopyFrom(defaultSetting);
            if (loadStyle == InitializeTiming.Awake)
                LoadFromPlayerPrefs();
        }
        public void Start()
        {
            if (loadStyle == InitializeTiming.Start)
                LoadFromPlayerPrefs();
            BindAll();
            current.Changed += Current_Changed;
        }
        private void OnEnable()
        {
            if (loadStyle == InitializeTiming.Enabled)
                LoadFromPlayerPrefs();
        }
        private void Current_Changed()
        {
            _DataUpdated?.Invoke();
            if (SaveOnChanged)
                SaveToPlayerPrefs();
        }

        public void BindAll()
        { 
            foreach (IBindable port in _bindings)
            {
                port.BindTo(current);
                if (!current.ContainsKey(port.GetFieldName()))
                    current[port.GetFieldName()] = port.GetValue();
            }
        }
        public void UnbindAll()
        {
            foreach (IBindable port in _bindings)
                port.Unbind();
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

        public virtual bool IsValidToSave() => true;

        public virtual void SaveToPlayerPrefs()
        {
            PlayerPrefs.SetString(KEY, current.ToJson());
            PlayerPrefs.Save();
        }
        public virtual bool TryLoadCurrent(out DataView output)
        {
            output = default;
            if (!PlayerPrefs.HasKey(KEY))
                return false;
            DataView.TryParseFromJson(PlayerPrefs.GetString(KEY), out output);
            return true;
        }
        public void LoadFromPlayerPrefs()
        {
            if (!TryLoadCurrent(out DataView data))
            {
                $"Failed to parse preferences from PlayerPrefs with key [{KEY}]".printWarning();
                return;
            }            
            current.CopyFrom(data);
            WriteToBindings();
        }


        public void WriteTo(Object obj) => current.WriteTo(obj);
        public void ReadFrom(Object obj) => current.ReadFrom(obj);

        [System.Obsolete("Use WriteTo instead")]
        public void GetProperty(Object obj) => WriteTo(obj);
        [System.Obsolete("Use ReadFrom instead")]
        public void SetProperty(Object obj) => ReadFrom(obj);
  

    }
}