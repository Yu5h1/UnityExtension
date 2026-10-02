using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Yu5h1Lib.MVVM;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public abstract class Preferences<T> : SingletonBehaviour<T>, IPreferences where T : Preferences<T>
    {
        public virtual string KEY => GetType().Name;
        [SerializeField, TypeRestriction(typeof(Component), filter = typeof(ValuePortResolver))] private List<Object> _bindings;
        public IReadOnlyList<Object> bindings => _bindings;

        /// <summary>The ScriptableObject type this host accepts as <c>_source</c>; null (the default) means the
        /// host takes no source and keeps only inline defaults. A consumer's host overrides it to opt a
        /// package's settings asset in; the package itself never references Preferences.</summary>
        public virtual System.Type SourceType => null;

        [SerializeField, PreferenceSource] private ScriptableObject _source;

        /// <summary>The assigned source when <see cref="SourceType"/> accepts it, otherwise null.</summary>
        public ScriptableObject source
        {
            get
            {
                if (_source == null || SourceType == null)
                    return null;
                if (SourceType.IsInstanceOfType(_source))
                    return _source;
                $"Preferences on {name}: source '{_source.name}' is not a {SourceType.Name}; ignored.".printWarning();
                return null;
            }
        }

        [SerializeField, FormerlySerializedAs("defaultSetting")] private DataView _defaultSetting;

        /// <summary>Defaults for fields missing from the save. With a <see cref="source"/>, its members come
        /// first (see <see cref="PrepareSourceMembers"/>) followed by any loose keys.</summary>
        protected DataView defaultSetting
        {
            get => _defaultSetting;
            set => _defaultSetting = value;
        }

        private bool sourcePrepared;

        /// <summary>
        /// Rebuilds <see cref="defaultSetting"/> from <see cref="source"/>: its members first, in declaration
        /// order, with the source's values converted to strings (added when missing, overwritten when present),
        /// then the existing keys the source does not declare, kept in their order. Members can only be added,
        /// never dropped, because they are C# fields. Runs from <c>OnInitializing</c> and from
        /// <see cref="BaseMonoBehaviour.Init()"/>; in Edit Mode it records Undo and marks the host dirty, and
        /// only when something changed.
        /// </summary>
        public void PrepareSourceMembers()
        {
            sourcePrepared = true;
            var src = source;
            if (src == null)
                return;
            var prepared = new DataView();
            var members = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var field in Preferences.GetSourceFields(src.GetType()))
            {
                string text;
                try
                {
                    text = PlayerPrefsSerializer.Default.Serialize(field.GetValue(src), field.FieldType);
                }
                catch (System.Exception e)
                {
                    $"Preferences on {name}: source member '{field.Name}' ({field.FieldType.Name}) cannot be converted, skipped. {e.Message}".printWarning();
                    continue;
                }
                prepared[field.Name] = text;
                members.Add(field.Name);
            }
            if (_defaultSetting != null)
                foreach (var pair in _defaultSetting)
                    if (!members.Contains(pair.Key))
                        prepared[pair.Key] = pair.Value;
            if (_defaultSetting != null && _defaultSetting.SequenceEqual(prepared))
                return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RecordObject(this, "Prepare Preference Members");
#endif
            _defaultSetting = prepared;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        [SerializeField, ReadOnly] private DataView _current = null;
        private bool isCurrentLoaded = false;

        /// <summary>The shared entry's DataView for <see cref="KEY"/> (<see cref="Preferences.GetView(string)"/>),
        /// with <see cref="defaultSetting"/> merged into fields the save lacks. The merge saves once when a save
        /// already existed, and not at all when there was none.</summary>
        public DataView current
        {
            get
            {
                if (isCurrentLoaded)
                    return _current;
                isCurrentLoaded = true;
                if (!sourcePrepared)
                    PrepareSourceMembers();
                Preferences.Register(KEY, new HostStorage(this));
                var view = Preferences.GetView(KEY, out bool loaded);
                Preferences.SetSaveOnChanged(KEY, false);
                bool merged = false;
                if (defaultSetting != null)
                    foreach (var pair in defaultSetting)
                        if (!view.ContainsKey(pair.Key))
                        {
                            view[pair.Key] = pair.Value;
                            merged = true;
                        }
                _current = view;
                Preferences.SetSaveOnChanged(KEY, _saveOnChanged);
                if (loaded && merged && _saveOnChanged)
                    Preferences.Save(KEY);
                _current.Changed += Current_Changed;
                return _current;
            }
        }
        IDataView IPreferences.current => current;

        public bool TryGetDefault(string key, out string value)
        {
            value = default;
            return defaultSetting != null && defaultSetting.TryGetValue(key, out value);
        }

        [SerializeField] private UnityEvent _changed = new UnityEvent();
        public event UnityAction changed
        {
            add => _changed.AddListener(value);
            remove => _changed.RemoveListener(value);
        }

        [SerializeField, FormerlySerializedAs("SaveOnChanged")] private bool _saveOnChanged = true;

        /// <summary>Whether the shared entry saves this KEY on every change.</summary>
        public bool SaveOnChanged
        {
            get => _saveOnChanged;
            set
            {
                _saveOnChanged = value;
                if (isCurrentLoaded)
                    Preferences.SetSaveOnChanged(KEY, value);
            }
        }

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

        protected override void OnInitializing() => PrepareSourceMembers();

        protected virtual void Start()
        {
            BindAll();
        }
        protected virtual void OnDestroy()
        {
            UnbindAll();
            if (_current != null)
                _current.Changed -= Current_Changed;
        }

        private void Current_Changed() => _changed?.Invoke();

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

        /// <summary>
        /// Puts every key that has a default (<see cref="defaultSetting"/>, source members included) back to it
        /// and saves once; keys without a default are left alone. Bound ports are rebound so the UI shows the
        /// restored values. Also how an Editor change to the source becomes visible over an existing local save.
        /// </summary>
        [ContextMenu(nameof(ResetToDefaults))]
        public void ResetToDefaults()
        {
            var view = current;
            if (defaultSetting == null)
                return;
            Preferences.SetSaveOnChanged(KEY, false);
            foreach (var pair in defaultSetting)
                view[pair.Key] = pair.Value;
            Preferences.SetSaveOnChanged(KEY, _saveOnChanged);
            if (_saveOnChanged)
                Preferences.Save(KEY);
            foreach (var ports in _boundPorts.Values)
                foreach (var port in ports.ToArray())
                    port.BindTo(view);
        }



        public virtual bool IsValidToSave() => true;

        /// <summary>How this host's KEY is saved; the shared entry calls it whenever the DataView changes.
        /// Override together with <see cref="TryLoadCurrent"/> for a custom storage format.</summary>
        public virtual void SaveToPlayerPrefs() => Preferences.DefaultStorage.Save(KEY, current);

        /// <summary>How this host's KEY is loaded; the shared entry calls it on first access.</summary>
        public virtual bool TryLoadCurrent(out DataView output) => Preferences.DefaultStorage.TryLoad(KEY, out output);

        /// <summary>Routes the shared entry's load/save of this KEY through the host's virtual methods, so
        /// a host that overrides them (e.g. VCP UserDataView) keeps its format without registering storage itself.</summary>
        private class HostStorage : Preferences.IStorage
        {
            private readonly Preferences<T> host;
            public HostStorage(Preferences<T> host) => this.host = host;
            public bool TryLoad(string key, out DataView data) => host.TryLoadCurrent(out data);
            public void Save(string key, DataView data) => host.SaveToPlayerPrefs();
            public void Delete(string key) => Preferences.DefaultStorage.Delete(key);
        }
        
        protected virtual void Print()
            => $"Preferences: {KEY}\n{current.Select(d => $"  {d.Key}: {d.Value}").Join('\n')}".print();
    }
}
