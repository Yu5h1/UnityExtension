using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.MVVM;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public abstract class Preferences<T> : SingletonBehaviour<T> where T : Preferences<T>
    {
        public virtual string KEY => GetType().Name;
        [SerializeField,TypeRestriction(typeof(Component))] private List<Object> _bindings;
        public IReadOnlyList<Object> bindings => _bindings;

        [SerializeField] protected DataView defaultSetting;

        [SerializeField, ReadOnly] private DataView _current = null;
        private bool isCurrentLoaded = false;
        public DataView current
        {
            get
            {
                if (isCurrentLoaded)
                    return _current;
                isCurrentLoaded = true;
                if (TryLoadCurrent(out DataView data))
                    _current = new DataView(data);
                else
                {
                    $"Failed to parse preferences from PlayerPrefs with key [{KEY}]".printWarning();
                    _current = defaultSetting == null ? new DataView() : new DataView(defaultSetting);
                }
                _current.Changed += Current_Changed;
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

        /// <summary>Resolved map from each binding Object to its IValuePort. Built during BindAll.</summary>
        private readonly Dictionary<Object, IValuePort> _portMap = new Dictionary<Object, IValuePort>();

        IValuePort ResolvePort(Object obj)
        {
            if (obj is IValuePort port) return port;
            if (obj is IAdapterShell shell && shell.adapter is IValuePort shellPort) return shellPort;
            if (obj is Component c &&
                AdapterFactory<Component>.TryCreate(c, out IAdapter<Component> adapter) &&
                adapter is IValuePort adapterPort)
                return adapterPort;
            return null;
        }

        public bool TryGetValueFromBindings(string key, out string value)
        {
            value = default;
            foreach (var port in _portMap.Values)
            {
                if (port.GetFieldName() == key)
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
        protected virtual void OnDestroy() => UnbindAll();

        private void Current_Changed()
        {
            _changed?.Invoke();
            if (SaveOnChanged)
                SaveToPlayerPrefs();
        }

        public void BindAll()
        {
            _portMap.Clear();
            for (int i = 0; i < _bindings.Count; i++)
            {
                var obj = _bindings[i];
                if (obj == null) continue;
                var port = ResolvePort(obj);
                if (port == null) continue;
                _portMap[obj] = port;
                if (!current.ContainsKey(port.GetFieldName()))
                    current[port.GetFieldName()] = port.GetValue();
            }
            WriteToBindings();
            foreach (var port in _portMap.Values)
                port.BindTo(current);
        }

        public void UnbindAll()
        {
            foreach (var port in _portMap.Values)
                port.Unbind();
            _portMap.Clear();
        }

        public void WriteToBindings()
        {
            foreach (var port in _portMap.Values)
                current.WriteTo(port);
        }

        public void ReadFromBindings()
        {
            foreach (var port in _portMap.Values)
                current.ReadFrom(port);
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
            return DataView.TryParseFromJson(PlayerPrefs.GetString(KEY), out output);
        }
        
        protected virtual void Print()
            => $"Preferences: {KEY}\n{current.Select(d => $"  {d.Key}: {d.Value}").Join('\n')}".print();
    }
}
