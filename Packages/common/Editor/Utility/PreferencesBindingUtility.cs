using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib.MVVM;

namespace Yu5h1Lib.EditorExtension
{
    public enum PreferencesBindOutcome
    {
        Bound,
        AlreadyBound,
        DuplicateName,
        Unbindable,
    }

    public readonly struct PreferencesBindResult
    {
        public readonly Object Control;
        public readonly PreferencesBindOutcome Outcome;
        public readonly string Message;

        public PreferencesBindResult(Object control, PreferencesBindOutcome outcome, string message)
        {
            Control = control;
            Outcome = outcome;
            Message = message;
        }
    }

    /// <summary>
    /// Editor-side binding logic for a Preferences host's `_bindings` list. Shared by the
    /// `CONTEXT/Component` bind command, <see cref="PreferencesBindingDrawer"/>, and MCP `execute_code`,
    /// so the three entry points cannot drift on what counts as bindable or as a name collision.
    /// </summary>
    public static class PreferencesBindingUtility
    {
        private const string BindingsPropertyName = "_bindings";

        /// <summary>Finds the SerializedObject holding `_bindings`: the host itself, or the first MonoBehaviour on it that has that field.</summary>
        public static bool TryFindBindingsHost(Object host, out SerializedObject serializedHost)
        {
            serializedHost = null;
            if (host == null) return false;
            if (host is GameObject go)
            {
                foreach (var m in go.GetComponents<MonoBehaviour>())
                {
                    if (m == null) continue;
                    var probe = new SerializedObject(m);
                    if (probe.FindProperty(BindingsPropertyName) != null)
                    {
                        serializedHost = probe;
                        return true;
                    }
                }
                return false;
            }
            var so = new SerializedObject(host);
            if (so.FindProperty(BindingsPropertyName) == null)
                return false;
            serializedHost = so;
            return true;
        }

        /// <summary>Resolves a dragged or selected Object to the Component it should bind as: itself if bindable, or the first bindable Component on its GameObject.</summary>
        public static Component ResolveBindableComponent(Object candidate)
        {
            if (candidate is Component direct)
                return ValuePortResolver.Resolve(direct) != null ? direct : null;
            if (!(candidate is GameObject go))
                return null;
            foreach (var c in go.GetComponents<Component>())
                if (c is IValuePort)
                    return c;
            foreach (var c in go.GetComponents<Component>())
                if (c != null && ValuePortResolver.Resolve(c) != null)
                    return c;
            return null;
        }

        /// <summary>Binds each control into host's `_bindings`, skipping a control already present, unbindable, or whose field name collides with an existing entry.</summary>
        public static IReadOnlyList<PreferencesBindResult> BindSelected(Object host, IEnumerable<Object> controls)
        {
            var results = new List<PreferencesBindResult>();
            if (!TryFindBindingsHost(host, out var serializedHost))
            {
                foreach (var control in controls)
                    results.Add(new PreferencesBindResult(control, PreferencesBindOutcome.Unbindable, "Host has no _bindings field."));
                return results;
            }

            var list = serializedHost.FindProperty(BindingsPropertyName);
            var boundControls = new HashSet<Object>();
            var boundNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < list.arraySize; i++)
            {
                var bound = list.GetArrayElementAtIndex(i).objectReferenceValue;
                if (bound == null) continue;
                boundControls.Add(bound);
                if (bound is Component boundComponent)
                    boundNames.Add(boundComponent.gameObject.name);
            }

            foreach (var control in controls)
            {
                if (control == null)
                {
                    results.Add(new PreferencesBindResult(control, PreferencesBindOutcome.Unbindable, "Control is null."));
                    continue;
                }
                var component = ResolveBindableComponent(control);
                if (component == null)
                {
                    results.Add(new PreferencesBindResult(control, PreferencesBindOutcome.Unbindable, $"{control.name} is not an IValuePort and has no registered Adapter."));
                    continue;
                }
                if (boundControls.Contains(component))
                {
                    results.Add(new PreferencesBindResult(control, PreferencesBindOutcome.AlreadyBound, $"{component.name} is already in _bindings."));
                    continue;
                }
                var fieldName = component.gameObject.name;
                if (!boundNames.Add(fieldName))
                {
                    results.Add(new PreferencesBindResult(control, PreferencesBindOutcome.DuplicateName, $"Field name '{fieldName}' is already bound by another control."));
                    continue;
                }
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = component;
                boundControls.Add(component);
                results.Add(new PreferencesBindResult(control, PreferencesBindOutcome.Bound, fieldName));
            }
            serializedHost.ApplyModifiedProperties();
            return results;
        }

        /// <summary>True if another entry in the same `_bindings` array already uses this field name.</summary>
        public static bool IsDuplicateName(SerializedProperty element, Component resolved, out int duplicateIndex)
        {
            duplicateIndex = -1;
            var array = FindParentArrayProperty(element);
            if (array == null) return false;
            var currentIndex = ExtractArrayIndex(element.propertyPath);
            var fieldName = resolved.gameObject.name;
            for (int i = 0; i < array.arraySize; i++)
            {
                if (i == currentIndex) continue;
                var sibling = array.GetArrayElementAtIndex(i).objectReferenceValue as Component;
                if (sibling != null && string.Equals(sibling.gameObject.name, fieldName, System.StringComparison.OrdinalIgnoreCase))
                {
                    duplicateIndex = i;
                    return true;
                }
            }
            return false;
        }

        private static SerializedProperty FindParentArrayProperty(SerializedProperty element)
        {
            var path = element.propertyPath;
            var index = path.LastIndexOf(".Array.data[", System.StringComparison.Ordinal);
            return index < 0 ? null : element.serializedObject.FindProperty(path.Substring(0, index));
        }

        private static int ExtractArrayIndex(string propertyPath)
        {
            var start = propertyPath.LastIndexOf('[');
            var end = propertyPath.LastIndexOf(']');
            if (start < 0 || end < 0 || end <= start) return -1;
            return int.TryParse(propertyPath.Substring(start + 1, end - start - 1), out var index) ? index : -1;
        }
    }
}
