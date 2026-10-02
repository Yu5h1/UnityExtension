using System.Collections.Generic;
using UnityEngine;
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

        /// <summary>Stores one KEY as the DataView's JSON under that PlayerPrefs key.</summary>
        public class PlayerPrefsStorage : IStorage
        {
            public bool TryLoad(string key, out DataView data)
            {
                data = null;
                return PlayerPrefs.HasKey(key) && DataView.TryParseFromJson(PlayerPrefs.GetString(key), out data);
            }

            public void Save(string key, DataView data)
            {
                PlayerPrefs.SetString(key, data.ToJson());
                PlayerPrefs.Save();
            }

            public void Delete(string key)
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        public static IStorage DefaultStorage { get; } = new PlayerPrefsStorage();

        private class Entry
        {
            public DataView view;
            public bool loaded;
            public bool saveOnChanged = true;
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

        public static IStorage GetStorage(string key) => storages.TryGetValue(key, out var storage) ? storage : DefaultStorage;

        private static Entry GetEntry(string key)
        {
            if (entries.TryGetValue(key, out var entry))
                return entry;
            entry = new Entry();
            entry.loaded = GetStorage(key).TryLoad(key, out var data) && data != null;
            entry.view = entry.loaded ? new DataView(data) : new DataView();
            entry.view.Changed += () =>
            {
                if (entry.saveOnChanged)
                    Save(key);
            };
            entries[key] = entry;
            return entry;
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

        public static bool GetSaveOnChanged(string key) => GetEntry(key).saveOnChanged;
        public static void SetSaveOnChanged(string key, bool value) => GetEntry(key).saveOnChanged = value;

        /// <summary>Writes the shared DataView of <paramref name="key"/> through its storage.</summary>
        public static void Save(string key)
        {
            if (entries.TryGetValue(key, out var entry))
                GetStorage(key).Save(key, entry.view);
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
    }
}
