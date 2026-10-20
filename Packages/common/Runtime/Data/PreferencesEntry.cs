using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Yu5h1Lib.MVVM;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    /// <summary>
    /// The single entry for persisted settings: one shared in-memory <see cref="DataView"/> per KEY, loaded on
    /// first access and saved by this entry whenever it changes, so a scene host (<see cref="Preferences{T}"/>)
    /// and consumer code read and write the same object whether or not the host exists. How a KEY is stored is
    /// an <see cref="IStorage"/> registered for it; unregistered KEYs use <see cref="DefaultStorage"/>
    /// (one DataView JSON per KEY in PlayerPrefs). Typed reads and writes convert through
    /// <see cref="PlayerPrefsSerializer.Default"/>; the DataView itself only holds strings.
    /// <para>A save that exists but cannot be read (damaged, unknown <see cref="PreferencesFormat"/> version, read
    /// failure) puts that KEY in a load error: an Error is logged, the view stays free of the save's data (a host
    /// fills it with its defaults), nothing is written for the KEY and the original text stays untouched, until
    /// <see cref="Reload(string)"/> reads a fixed save or an explicit reset (<see cref="ClearLoadError(string)"/>,
    /// <see cref="Delete(string)"/>, a host's ResetToDefaults) lets the next write replace it. A KEY without a save
    /// is not an error; it simply uses defaults.</para>
    /// </summary>
    public static class Preferences
    {
        /// <summary>Loads, saves and deletes the whole DataView of one KEY. Synchronous by design.</summary>
        public interface IStorage
        {
            bool TryLoad(string key, out DataView data);
            void Save(string key, DataView data);
            void Delete(string key);
        }

        /// <summary>A storage that can tell "no save" from "a save that cannot be read". The entry uses it when the
        /// storage implements it; a plain <see cref="IStorage"/> can only say whether it found data, so a false
        /// <c>TryLoad</c> counts as no save.</summary>
        public interface IStatusStorage : IStorage
        {
            PreferencesLoadResult Load(string key);
        }

        /// <summary>Stores one KEY as <see cref="PreferencesFormat"/> JSON under that PlayerPrefs key.</summary>
        public class PlayerPrefsStorage : IStatusStorage
        {
            public PreferencesLoadResult Load(string key)
                => PlayerPrefs.HasKey(key) ? PreferencesFormat.Parse(PlayerPrefs.GetString(key)) : PreferencesLoadResult.Missing;

            public bool TryLoad(string key, out DataView data)
            {
                var result = Load(key);
                data = result.Status == PreferencesLoadStatus.Loaded ? result.Data : null;
                return data != null;
            }

            public void Save(string key, DataView data)
            {
                PlayerPrefs.SetString(key, PreferencesFormat.ToJson(data));
                PlayerPrefs.Save();
            }

            public void Delete(string key)
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        public static PlayerPrefsStorage DefaultStorage { get; } = new PlayerPrefsStorage();

        private class Entry
        {
            public DataView view;
            public bool loaded;
            public bool saveOnChanged = true;
            /// <summary>Why the save could not be read; null when the KEY has no load error.</summary>
            public string loadError;
            public bool blockedWarned;
            /// <summary>Changed while saving on change was off and not yet saved: an unsaved draft.</summary>
            public bool dirty;
            /// <summary>Internal edits (default merge, reload) that must not save or count as a draft.</summary>
            public bool suspended;
        }

        private static readonly Dictionary<string, IStorage> storages = new Dictionary<string, IStorage>();
        private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

        /// <summary>Static state survives between Play sessions when domain reload is disabled.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            storages.Clear();
            entries.Clear();
        }

        /// <summary>Registers how <paramref name="key"/> is stored. Takes effect for loads that have not
        /// happened yet and for every later save; register before the first read when the format differs
        /// from <see cref="DefaultStorage"/>.</summary>
        public static void Register(string key, IStorage storage) => storages[key] = storage ?? DefaultStorage;

        /// <summary>Takes back a registration, but only when <paramref name="storage"/> is still the one registered
        /// for <paramref name="key"/>; the KEY then falls back to <see cref="DefaultStorage"/>. A host calls it when
        /// it is destroyed so the entry never calls a destroyed object.</summary>
        public static void Unregister(string key, IStorage storage)
        {
            if (storages.TryGetValue(key, out var current) && ReferenceEquals(current, storage))
                storages.Remove(key);
        }

        public static IStorage GetStorage(string key) => storages.TryGetValue(key, out var storage) ? storage : DefaultStorage;

        private static Entry GetEntry(string key)
        {
            if (entries.TryGetValue(key, out var entry))
                return entry;
            entry = new Entry();
            var result = ReadStorage(key);
            entry.loaded = result.Status == PreferencesLoadStatus.Loaded;
            entry.view = entry.loaded ? result.Data : new DataView();
            if (result.Status == PreferencesLoadStatus.Error)
                SetLoadError(key, entry, result);
            var created = entry;
            entry.view.Changed += () =>
            {
                if (created.suspended || !entries.TryGetValue(key, out var live) || live != created)
                    return;
                if (created.saveOnChanged)
                    Save(key);
                else if (created.loadError == null)
                    created.dirty = true;
            };
            entries[key] = entry;
            return entry;
        }

        private static PreferencesLoadResult ReadStorage(string key)
        {
            try
            {
                var storage = GetStorage(key);
                if (storage is IStatusStorage statusStorage)
                    return statusStorage.Load(key);
                return storage.TryLoad(key, out var data) && data != null
                    ? PreferencesLoadResult.Loaded(data)
                    : PreferencesLoadResult.Missing;
            }
            catch (System.Exception e)
            {
                // A host override or custom storage threw, so it could not hand back the text it read; the best
                // original we can show is whatever PlayerPrefs holds under this KEY.
                var raw = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
                return PreferencesLoadResult.Failed(raw, "reading the save failed: " + e.Message);
            }
        }

        private static void SetLoadError(string key, Entry entry, PreferencesLoadResult result)
        {
            entry.loadError = result.Error;
            entry.blockedWarned = false;
            var raw = result.Raw == null ? "" : (result.Raw.Length > 200 ? result.Raw.Substring(0, 200) + "..." : result.Raw);
            Debug.LogError($"Preferences [{key}]: the save cannot be read ({result.Error}). Using defaults and not writing this KEY until it is reloaded or explicitly reset; the original text is kept. Original: {raw}");
        }

        /// <summary>The shared DataView of <paramref name="key"/>, loaded on first access.</summary>
        public static DataView GetView(string key) => GetEntry(key).view;

        /// <summary>As <see cref="GetView(string)"/>; <paramref name="loaded"/> tells whether storage held
        /// a save for <paramref name="key"/> when it was first loaded.</summary>
        public static DataView GetView(string key, out bool loaded)
        {
            var entry = GetEntry(key);
            loaded = entry.loaded;
            return entry.view;
        }

        /// <summary>Why the save of <paramref name="key"/> could not be read, or null when it was read (or there is
        /// none). While this is set the KEY is not written.</summary>
        public static string GetLoadError(string key) => GetEntry(key).loadError;

        /// <summary>
        /// Reads <paramref name="key"/> from its storage again into the shared view (the same object, so bound
        /// controls and subscribers keep it). Returns true when the save was read or there is none, which clears a
        /// load error; returns false and keeps the error when it still cannot be read. A KEY without a save ends up
        /// empty; a host re-merges its defaults (see its Reload).
        /// </summary>
        public static bool Reload(string key)
        {
            var entry = GetEntry(key);
            var result = ReadStorage(key);
            if (result.Status == PreferencesLoadStatus.Error)
            {
                SetLoadError(key, entry, result);
                return false;
            }
            entry.suspended = true;
            try
            {
                entry.view.Clear();
                if (result.Status == PreferencesLoadStatus.Loaded)
                    foreach (var pair in result.Data)
                        entry.view[pair.Key] = pair.Value;
            }
            finally
            {
                entry.suspended = false;
            }
            entry.loadError = null;
            entry.blockedWarned = false;
            entry.loaded = result.Status == PreferencesLoadStatus.Loaded;
            entry.dirty = false;
            return true;
        }

        /// <summary>Explicit reset of a load error: the unreadable save is given up, so the next write replaces it.
        /// Does nothing for a KEY that is not loaded or has no error.</summary>
        public static void ClearLoadError(string key)
        {
            if (!entries.TryGetValue(key, out var entry))
                return;
            entry.loadError = null;
            entry.blockedWarned = false;
        }

        /// <summary>Deletes the save of <paramref name="key"/> from its storage and clears a load error, so the KEY
        /// goes back to "no save". The shared view keeps its current values until something changes them.</summary>
        public static void Delete(string key)
        {
            GetStorage(key).Delete(key);
            ClearLoadError(key);
            if (entries.TryGetValue(key, out var entry))
            {
                entry.loaded = false;
                entry.dirty = false;
            }
        }

        /// <summary>
        /// Releases <paramref name="key"/>: the next access reads the storage again, as after a restart. Returns
        /// false and logs a warning, releasing nothing, while the KEY holds an unsaved draft (changed while
        /// <see cref="SetSaveOnChanged"/> was off); returns true when released or when it was not loaded.
        /// </summary>
        public static bool Unload(string key)
        {
            if (!entries.TryGetValue(key, out var entry))
                return true;
            if (entry.dirty)
            {
                Debug.LogWarning($"Preferences [{key}]: not unloaded, it has unsaved changes. Save it first.");
                return false;
            }
            entries.Remove(key);
            return true;
        }

        /// <summary>Whether <paramref name="view"/> is still the loaded view of <paramref name="key"/>
        /// (false after <see cref="Unload"/>).</summary>
        internal static bool IsLoadedView(string key, DataView view)
            => entries.TryGetValue(key, out var entry) && ReferenceEquals(entry.view, view);

        /// <summary>Edits made between Begin and End neither save nor count as a draft; for a host merging
        /// defaults into the shared view.</summary>
        internal static void BeginBatch(string key) => GetEntry(key).suspended = true;
        internal static void EndBatch(string key) => GetEntry(key).suspended = false;

        public static bool GetSaveOnChanged(string key) => GetEntry(key).saveOnChanged;
        public static void SetSaveOnChanged(string key, bool value) => GetEntry(key).saveOnChanged = value;

        /// <summary>Writes the shared DataView of <paramref name="key"/> through its storage; does nothing while the
        /// KEY has a load error.</summary>
        public static void Save(string key)
        {
            if (!entries.TryGetValue(key, out var entry))
                return;
            if (entry.loadError != null)
            {
                if (!entry.blockedWarned)
                {
                    entry.blockedWarned = true;
                    Debug.LogWarning($"Preferences [{key}]: not saved, the stored save could not be read ({entry.loadError}). Reload it or reset the KEY explicitly.");
                }
                return;
            }
            GetStorage(key).Save(key, entry.view);
            entry.dirty = false;
        }

        /// <summary>Reads <paramref name="field"/> of <paramref name="key"/> as <typeparamref name="V"/>;
        /// returns <paramref name="fallback"/> when the field is missing or cannot be converted (with a warning).</summary>
        public static V Get<V>(string key, string field, V fallback)
        {
            if (!GetView(key).TryGetValue(field, out var text))
                return fallback;
            if (PlayerPrefsSerializer.Default.TryDeserialize(text, out V value))
                return value;
            $"Preferences [{key}].{field}: '{text}' is not a {typeof(V).Name}; using the fallback.".printWarning();
            return fallback;
        }

        /// <summary>Writes <paramref name="value"/> into <paramref name="field"/> of <paramref name="key"/>; the
        /// entry saves it when SaveOnChanged is on. Bound UI is not updated (binding is one-way after it binds).</summary>
        public static void Set<V>(string key, string field, V value)
            => GetView(key)[field] = PlayerPrefsSerializer.Default.Serialize(value);

        /// <summary>
        /// The members a source ScriptableObject contributes to a host's defaults: serialized fields (public or
        /// <c>[SerializeField]</c>, not static, readonly or <c>[NonSerialized]</c>) declared from the class just
        /// below <see cref="ScriptableObject"/> down to <paramref name="sourceType"/>, in declaration order.
        /// Fields whose type cannot become one string (UnityEngine.Object references, arrays and other
        /// collections) are left out and, when <paramref name="skipped"/> is given, added to it.
        /// </summary>
        public static List<FieldInfo> GetSourceFields(System.Type sourceType, List<FieldInfo> skipped = null)
        {
            var chain = new List<System.Type>();
            for (var type = sourceType; type != null && type != typeof(ScriptableObject); type = type.BaseType)
                chain.Insert(0, type);
            var fields = new List<FieldInfo>();
            foreach (var type in chain)
            {
                var declared = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                System.Array.Sort(declared, (a, b) => a.MetadataToken.CompareTo(b.MetadataToken));
                foreach (var field in declared)
                {
                    if (field.IsInitOnly || field.IsDefined(typeof(System.NonSerializedAttribute), false))
                        continue;
                    if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), false))
                        continue;
                    if (IsConvertibleMemberType(field.FieldType))
                        fields.Add(field);
                    else
                        skipped?.Add(field);
                }
            }
            return fields;
        }

        /// <summary>Implemented by a port whose value type cannot be read from an <c>IValuePort&lt;T&gt;</c> interface.</summary>
        public interface ITypedPort
        {
            System.Type ValueType { get; }
        }

        /// <summary>The value type a port edits: <see cref="ITypedPort.ValueType"/>, else the <c>T</c> of an
        /// implemented <c>IValuePort&lt;T&gt;</c>; <see cref="OptionSet"/> ports and anything else count as string,
        /// since that is what they write.</summary>
        public static System.Type GetPortValueType(IValuePort port)
        {
            if (port is ITypedPort typed)
                return typed.ValueType;
            if (port is OptionSet)
                return typeof(string);
            foreach (var type in port.GetType().GetInterfaces())
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IValuePort<>))
                    return type.GetGenericArguments()[0];
            return typeof(string);
        }

        /// <summary>Whether a port of <paramref name="portType"/> can drive a source member of
        /// <paramref name="memberType"/>: the same type, or a string port for an enum member (a dropdown of names).</summary>
        public static bool IsCompatible(System.Type portType, System.Type memberType)
            => portType == memberType || (portType == typeof(string) && memberType.IsEnum);

        public static string DescribeMismatch(string key, System.Type portType, System.Type memberType)
            => $"binding type {portType.Name} does not match source member '{key}' type {memberType.Name}";

        private static bool IsConvertibleMemberType(System.Type type)
            => !typeof(Object).IsAssignableFrom(type)
            && (type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(type));
    }
}
