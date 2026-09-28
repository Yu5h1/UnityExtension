using Yu5h1Lib.MVVM;

namespace Yu5h1Lib
{
    /// <summary>
    /// Non-generic view of <see cref="Preferences{T}"/> so a consumer (e.g. a UI Toolkit binding
    /// component) can reference any concrete subclass through a serialized field without knowing T.
    /// </summary>
    public interface IPreferences
    {
        IDataView current { get; }
    }
}
