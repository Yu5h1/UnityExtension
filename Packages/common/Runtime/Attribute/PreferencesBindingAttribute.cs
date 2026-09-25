using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Marks a Preferences `_bindings` list. Its drawer accepts only a control that is itself an
    /// <see cref="Yu5h1Lib.MVVM.IValuePort"/> or has a registered <see cref="AdapterFactory{TBase}"/> adapter,
    /// rejects anything else, and flags a field name already used by a sibling entry.
    /// </summary>
    public class PreferencesBindingAttribute : PropertyAttribute { }
}
