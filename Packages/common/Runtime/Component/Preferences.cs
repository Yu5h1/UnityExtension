using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public abstract class Preferences<T> : SingletonBehaviour<T> where T : Preferences<T>
    {
        public virtual string KEY => GetType().Name;
        [SerializeField,TypeRestriction(typeof(IBindable))] private List<Object> _bindings;
        public IReadOnlyList<Object> bindings => _bindings;

        [SerializeField] protected DataView defaultSetting;

        [SerializeField, ReadOnly] private DataView _current = null;
        public DataView current
        { 
            get
            {
                if (_current.IsEmpty())
                {
                    if (TryLoadCurrent(out DataView data))
                    {
                        _current = new DataView(data);
                    }
                    else
                    {
                        $"Failed to parse preferences from PlayerPrefs with key [{KEY}]".printWarning();
                        _current = defaultSetting == null ? new DataView() : new DataView(defaultSetting);
                    }
                    WriteToBindings();
                    _current.Changed += Current_Changed;
                }
                return _current;
            }
        }

        [SerializeField] private UnityEvent _changed;
        public event UnityAction changed
        {
            add => _changed.AddListener(value);
            remove => _changed.RemoveListener(value);
        }


        public bool SaveOnChanged = true;

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

        protected override void OnInstantiated() {}

        protected override void OnInitializing()
        {
            BindAll();
        }
        private void Current_Changed()
        {
            _changed?.Invoke();
            if (SaveOnChanged)
                SaveToPlayerPrefs();
        }

        public void BindAll()
        {
            foreach (IBindable port in _bindings)
            {
                if (!current.ContainsKey(port.GetFieldName()))
                    current[port.GetFieldName()] = port.GetValue();
                port.BindTo(current);
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
        public void WriteTo(Object obj) => current.WriteTo(obj);
        public void ReadFrom(Object obj) => current.ReadFrom(obj);

        protected virtual void Print()
            => $"Preferences: {KEY}\n{current.Select(d => $"  {d.Key}: {d.Value}").Join('\n')}".print();
    }
}