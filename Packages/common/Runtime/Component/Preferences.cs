using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.MVVM;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public abstract class Preferences<T> : SingletonBehaviour<T>, IPreferences where T : Preferences<T>
    {
        public virtual string KEY => GetType().Name;
        [SerializeField, TypeRestriction(typeof(Component), filter = typeof(ValuePortResolver))] private List<Object> _bindings;
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
                bool loaded = TryLoadCurrent(out DataView data);
                _current = loaded ? new DataView(data) : (defaultSetting == null ? new DataView() : new DataView(defaultSetting));
                _current.Changed += Current_Changed;
                if (loaded && defaultSetting != null)
                    foreach (var pair in defaultSetting)
                        if (!_current.ContainsKey(pair.Key))
                            _current[pair.Key] = pair.Value;
                return _current;
            }
        }
        IDataView IPreferences.current => current;

        public bool TryGetDefault(string key, out string value)
        {
            value = default;
            return defaultSetting != null && defaultSetting.TryGetValue(key, out value);
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

        /// <summary>Every port currently bound through <see cref="BindPort"/>, by field name, whichever path
        /// bound it (uGUI <c>BindAll</c>, UI Toolkit binder, ...). A list per name because conflicting ports
        /// stay bound; unbinding one must not hide the other.</summary>
        private readonly Dictionary<string, List<IValuePort>> _boundPorts =
            new Dictionary<string, List<IValuePort>>(System.StringComparer.OrdinalIgnoreCase);

        IValuePort ResolvePort(Object obj) => ValuePortResolver.Resolve(obj);

        public bool TryGetValueFromBindings(string key, out string value)
        {
            value = default;
            if (_boundPorts.TryGetValue(key, out var ports) && ports.Count > 0)
            {
                value = ports[0].GetValue();
                return true;
            }
            $"Key [{key}] not found in bindings.".printWarning();
            return false;
        }

        public string GetValueFromBindings(string key) => TryGetValueFromBindings(key, out string value) ? value : default;

        protected override void OnInstantiated() {}

        protected override void OnInitializing() {}

        protected virtual void Start()
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
            foreach (var port in _portMap.Values)
                UnbindPort(port);
            _portMap.Clear();
            for (int i = 0; i < _bindings.Count; i++)
            {
                var obj = _bindings[i];
                if (obj == null) continue;
                var port = ResolvePort(obj);
                if (port == null)
                {
                    $"BindAll on {name}: {obj.name} ({obj.GetType().Name}) has no IValuePort and no registered Adapter.".printWarning();
                    continue;
                }
                _portMap[obj] = port;
                var fieldName = port.GetFieldName();
                if (obj is Component boundComponent && LooksLikeDefaultName(fieldName, boundComponent))
                    $"BindAll on {name}: '{fieldName}' looks like a Unity default name; renaming the GameObject later will orphan its saved value.".printWarning();
            }
            foreach (var port in _portMap.Values)
                BindPort(port);
        }

        /// <summary>Fills <paramref name="port"/>'s field from <c>defaultSetting</c> (or its own current
        /// value) the first time it's seen, then binds it. The per-port half of <see cref="BindAll"/>;
        /// a UI Toolkit binding component calls this directly for ports it resolves itself.
        /// Warns when another port already holds the same field name, whichever path bound it.</summary>
        public void BindPort(IValuePort port)
        {
            var fieldName = port.GetFieldName();
            if (!_boundPorts.TryGetValue(fieldName, out var ports))
                _boundPorts[fieldName] = ports = new List<IValuePort>();
            if (!ports.Contains(port))
            {
                if (ports.Count > 0)
                    $"Preferences on {name}: field name '{fieldName}' is bound by more than one control; they will overwrite each other.".printWarning();
                ports.Add(port);
            }
            if (!current.ContainsKey(fieldName))
                current[fieldName] = defaultSetting != null && defaultSetting.TryGetValue(fieldName, out string fallback)
                    ? fallback
                    : port.GetValue();
            port.BindTo(current);
        }

        private static bool LooksLikeDefaultName(string fieldName, Component component)
        {
            var typeName = component.GetType().Name;
            if (fieldName == typeName) return true;
            return fieldName.StartsWith(typeName + " (", System.StringComparison.Ordinal) && fieldName.EndsWith(")");
        }

        /// <summary>Unbinds <paramref name="port"/> and drops it from the conflict registry. Call this
        /// instead of <c>port.Unbind()</c> for anything bound through <see cref="BindPort"/>, or a later
        /// rebind is misreported as a conflict.</summary>
        public void UnbindPort(IValuePort port)
        {
            foreach (var ports in _boundPorts.Values)
                ports.Remove(port);
            port.Unbind();
        }

        public void UnbindAll()
        {
            foreach (var ports in _boundPorts.Values)
                foreach (var port in ports)
                    port.Unbind();
            _boundPorts.Clear();
            _portMap.Clear();
        }

        public void ReadFromBindings()
        {
            foreach (var ports in _boundPorts.Values)
                foreach (var port in ports)
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
