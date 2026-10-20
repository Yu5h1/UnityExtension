using System;
using System.Collections.Generic;

namespace Yu5h1Lib.UIToolkit.Acceptance
{
    /// <summary>
    /// Central registration repository for module acceptance demo cases.
    /// Adding another module does not require branches in the coordinator.
    /// </summary>
    public static class UxModuleRegistry
    {
        private static readonly Dictionary<string, IUxModuleDemoCase> cases =
            new Dictionary<string, IUxModuleDemoCase>(StringComparer.OrdinalIgnoreCase);

        public static event Action OnRegistryChanged;

        public static void Register(IUxModuleDemoCase demoCase)
        {
            if (demoCase == null || string.IsNullOrEmpty(demoCase.Id)) return;
            cases[demoCase.Id] = demoCase;
            OnRegistryChanged?.Invoke();
        }

        public static void Unregister(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (cases.Remove(id))
            {
                OnRegistryChanged?.Invoke();
            }
        }

        public static void Clear()
        {
            cases.Clear();
            OnRegistryChanged?.Invoke();
        }

        public static IEnumerable<IUxModuleDemoCase> AllCases => cases.Values;

        public static IUxModuleDemoCase Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            cases.TryGetValue(id, out var demoCase);
            return demoCase;
        }

        public static bool Contains(string id) => !string.IsNullOrEmpty(id) && cases.ContainsKey(id);
    }
}
