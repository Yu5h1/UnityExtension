using System;
using System.Collections.Generic;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>
    /// Central registration repository for UGUI module acceptance demo cases.
    /// Adding another module does not require branches in the coordinator.
    /// </summary>
    public static class UxUiModuleRegistry
    {
        private static readonly Dictionary<string, (IUxUiModuleDemoCase DemoCase, string Description)> cases =
            new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> order = new();

        public static event Action OnRegistryChanged;

        public static void Register(IUxUiModuleDemoCase demoCase) => Register(demoCase, null);

        public static void Register(IUxUiModuleDemoCase demoCase, string description)
        {
            if (demoCase == null || string.IsNullOrEmpty(demoCase.Id))
            {
                return;
            }
            if (!cases.ContainsKey(demoCase.Id))
                order.Add(demoCase.Id);
            cases[demoCase.Id] = (demoCase, description);
            OnRegistryChanged?.Invoke();
        }

        public static void Unregister(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }
            if (cases.Remove(id))
            {
                order.RemoveAll(entry => string.Equals(entry, id, StringComparison.OrdinalIgnoreCase));
                OnRegistryChanged?.Invoke();
            }
        }

        public static void Clear()
        {
            cases.Clear();
            order.Clear();
            OnRegistryChanged?.Invoke();
        }

        public static IEnumerable<IUxUiModuleDemoCase> AllCases
        {
            get
            {
                foreach (var id in order)
                    yield return cases[id].DemoCase;
            }
        }

        internal static string GetDescription(string id) =>
            cases.TryGetValue(id, out var entry) ? entry.Description : null;

        public static IUxUiModuleDemoCase Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            return cases.TryGetValue(id, out var entry) ? entry.DemoCase : null;
        }

        public static bool Contains(string id) => !string.IsNullOrEmpty(id) && cases.ContainsKey(id);
    }
}
