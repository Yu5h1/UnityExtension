using UnityEngine;
using Yu5h1Lib;

namespace Yu5h1Lib.MVVM
{
    /// <summary>
    /// Resolves the <see cref="IValuePort"/> for an arbitrary Object: itself, its adapter shell, or a
    /// factory-built Adapter. Shared by <see cref="Preferences{T}"/>'s runtime binding and the Editor
    /// binding tools, so both sides agree on what counts as bindable.
    /// </summary>
    public static class ValuePortResolver
    {
        public static IValuePort Resolve(Object obj)
        {
            if (obj is IValuePort port) return port;
            if (obj is IAdapterShell shell && shell.adapter is IValuePort shellPort) return shellPort;
            if (obj is Component c &&
                AdapterFactory<Component>.TryCreate(c, out IAdapter<Component> adapter) &&
                adapter is IValuePort adapterPort)
                return adapterPort;
            return null;
        }
    }
}
