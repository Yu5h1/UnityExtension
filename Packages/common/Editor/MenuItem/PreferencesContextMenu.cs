using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Adds "綁定選取的控件" to the CONTEXT menu of any Component; it only shows when that component
    /// has a `_bindings` field. Binds the objects currently selected in the Hierarchy, other than the
    /// host itself, via <see cref="PreferencesBindingUtility.BindSelected"/>.
    /// </summary>
    public static class PreferencesContextMenu
    {
        private const string MenuPath = "CONTEXT/Component/綁定選取的控件";

        [MenuItem(MenuPath, true)]
        private static bool Validate(MenuCommand command)
            => PreferencesBindingUtility.TryFindBindingsHost(command.context, out _);

        [MenuItem(MenuPath)]
        private static void Execute(MenuCommand command)
        {
            var host = command.context;
            var hostGameObject = (host as Component)?.gameObject;
            var controls = Selection.objects.Where(o => o != host && o != hostGameObject).ToArray();
            if (controls.Length == 0)
            {
                "No controls selected.".printWarning();
                return;
            }

            var results = PreferencesBindingUtility.BindSelected(host, controls);
            var report = new StringBuilder();
            foreach (var result in results)
                report.AppendLine($"{result.Outcome} {result.Control?.name}: {result.Message}");
            report.ToString().print();
        }
    }
}
