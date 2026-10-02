using Yu5h1Lib.MVVM;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    /// <summary>
    /// Non-generic view of <see cref="Preferences{T}"/> so a consumer (e.g. a UI Toolkit binding
    /// component) can reference any concrete subclass through a serialized field without knowing T.
    /// </summary>
    public interface IPreferences
    {
        /// <summary>Identifies the saved data this host reads and writes; hosts sharing a KEY share one save.</summary>
        string KEY { get; }

        /// <summary>The ScriptableObject type this host accepts as its defaults source; null when it takes none.</summary>
        System.Type SourceType { get; }

        /// <summary>The type of the source member named <paramref name="key"/> (case-insensitive); false when
        /// there is no accepted source or it declares no such member.</summary>
        bool TryGetSourceMemberType(string key, out System.Type type);

        IDataView current { get; }

        /// <summary>Reads the saved data from storage, bypassing <see cref="current"/>.</summary>
        bool TryLoadCurrent(out DataView output);

        bool TryGetDefault(string key, out string value);

        /// <summary>Fills the port's field from <c>defaultSetting</c> (or its own current value) the
        /// first time it's seen, then binds it — the per-port half of what <c>BindAll</c> loops over.</summary>
        void BindPort(IValuePort port);

        /// <summary>Unbinds a port bound through <see cref="BindPort"/> and releases its field name, so
        /// rebinding later is not reported as a conflict.</summary>
        void UnbindPort(IValuePort port);
    }
}
