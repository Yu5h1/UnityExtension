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

        [SerializeField] private UnityEvent _changed;
        public event UnityAction changed
        {
            add => _changed.AddListener(value);
            remove => _changed.RemoveListener(value);
        }
        public bool SaveOnChanged = true;

        /// <summary>Resolved map from each binding Object to its IValuePort. Built during BindAll.</summary>
        private readonly Dictionary<Object, IValuePort> _portMap = new Dictionary<Object, IValuePort>();

        IValuePort ResolvePort(Object obj) => ValuePortResolver.Resolve(obj);

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
            _portMap.Clear();
            var seenFieldNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
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
                if (!seenFieldNames.Add(fieldName))
                    $"BindAll on {name}: field name '{fieldName}' is bound by more than one control; they will overwrite each other.".printWarning();
                else if (obj is Component boundComponent && LooksLikeDefaultName(fieldName, boundComponent))
                    $"BindAll on {name}: '{fieldName}' looks like a Unity default name; renaming the GameObject later will orphan its saved value.".printWarning();
                if (!current.ContainsKey(fieldName))
                    current[fieldName] = defaultSetting != null && defaultSetting.TryGetValue(fieldName, out string fallback)
                        ? fallback
                        : port.GetValue();
            }
            foreach (var port in _portMap.Values)
                port.BindTo(current);
        }

        private static bool LooksLikeDefaultName(string fieldName, Component component)
        {
            var typeName = component.GetType().Name;
            if (fieldName == typeName) return true;
            return fieldName.StartsWith(typeName + " (", System.StringComparison.Ordinal) && fieldName.EndsWith(")");
        }

        public void UnbindAll()
        {
            foreach (var port in _portMap.Values)
                port.Unbind();
            _portMap.Clear();
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
