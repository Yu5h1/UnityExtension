using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.EditorExtension
{
    public static class CollectionElementMenu
    {
        private const string ArrayDataToken = ".Array.data[";

        private static readonly List<object> _clipboard = new List<object>();
        private static string _clipboardElementType;

        // Unity keeps every live ReorderableList here; it is the only way to reach a list's selection from a property.
        private static readonly FieldInfo _listInstancesField =
            typeof(ReorderableList).GetField("s_Instances", BindingFlags.Static | BindingFlags.NonPublic);

        // serializedProperty may be a drawing iterator that has since advanced; these keep the path captured when it was assigned.
        private static readonly FieldInfo _listPropertyPathField =
            typeof(ReorderableList).GetField("m_PropertyPath", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _listSerializedObjectField =
            typeof(ReorderableList).GetField("m_SerializedObject", BindingFlags.Instance | BindingFlags.NonPublic);

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }

        private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.serializedObject.isEditingMultipleObjects)
                return;

            if (IsCollection(property))
            {
                var collection = property.Copy();
                menu.AddSeparator("");
                AddPasteItem(menu, collection, collection.arraySize);
            }
            else if (TryGetOwningCollection(property, out var collection, out int index))
            {
                menu.AddSeparator("");
                AddCopyCutItems(menu, collection, index);
                AddPasteItem(menu, collection, index);
            }
        }

        private static bool IsCollection(SerializedProperty property)
            => property.isArray && property.propertyType != SerializedPropertyType.String;

        private static bool TryGetOwningCollection(SerializedProperty element, out SerializedProperty collection, out int index)
        {
            collection = null;
            index = -1;
            var path = element.propertyPath;
            int token = path.LastIndexOf(ArrayDataToken, StringComparison.Ordinal);
            if (token < 0 || !path.EndsWith("]", StringComparison.Ordinal) || !element.TryGetArrayElementIndex(out index))
                return false;
            collection = element.serializedObject.FindProperty(path.Substring(0, token));
            return collection != null;
        }

        private static void AddCopyCutItems(GenericMenu menu, SerializedProperty collection, int clickedIndex)
        {
            var indices = ResolveTargetIndices(collection, clickedIndex, out var clearSelection);
            menu.AddItem(new GUIContent($"Copy Elements ({indices.Count})"), false, () => Copy(collection, indices));
            menu.AddItem(new GUIContent($"Cut Elements ({indices.Count})"), false, () => Cut(collection, indices, clearSelection));
        }

        private static void AddPasteItem(GenericMenu menu, SerializedProperty collection, int insertAt)
        {
            if (_clipboard.Count == 0 || collection.arrayElementType != _clipboardElementType)
            {
                menu.AddDisabledItem(new GUIContent("Paste Elements"));
                return;
            }
            menu.AddItem(new GUIContent($"Paste Elements ({_clipboard.Count})"), false, () => Paste(collection, insertAt));
        }

        private static List<int> ResolveTargetIndices(SerializedProperty collection, int clickedIndex, out Action clearSelection)
        {
            int arraySize = collection.arraySize;
            foreach (var list in FindLists(collection))
            {
                if (list is ReorderableListEnhanced enhanced && enhanced.isFiltering)
                    continue;
                if (list.selectedIndices.Contains(clickedIndex))
                {
                    clearSelection = list.ClearSelection;
                    return Normalize(list.selectedIndices, arraySize);
                }
            }
            foreach (var view in FindListViews(collection))
            {
                var selected = view.selectedIndices.ToList();
                if (selected.Contains(clickedIndex))
                {
                    clearSelection = view.ClearSelection;
                    return Normalize(selected, arraySize);
                }
            }
            clearSelection = null;
            return new List<int> { clickedIndex };
        }

        private static List<int> Normalize(IEnumerable<int> indices, int arraySize)
            => indices.Where(i => i >= 0 && i < arraySize).Distinct().OrderBy(i => i).ToList();

        // Inspectors without a custom IMGUI editor draw collections as UI Toolkit ListViews, which never enter s_Instances.
        private static List<ListView> FindListViews(SerializedProperty collection)
        {
            var window = EditorWindow.mouseOverWindow ?? EditorWindow.focusedWindow;
            if (window == null)
                return new List<ListView>();

            var path = collection.propertyPath;
            int arraySize = collection.arraySize;
            return window.rootVisualElement.Query<ListView>().Where(view =>
                    (view.bindingPath == path || view.GetFirstAncestorOfType<PropertyField>()?.bindingPath == path)
                    && view.itemsSource != null && view.itemsSource.Count == arraySize)
                .ToList();
        }

        private static List<ReorderableList> FindLists(SerializedProperty collection)
        {
            var result = new List<ReorderableList>();
            if (!(_listInstancesField?.GetValue(null) is List<WeakReference<ReorderableList>> instances))
                return result;

            var target = collection.serializedObject.targetObject;
            var path = collection.propertyPath;
            foreach (var reference in instances)
            {
                if (!reference.TryGetTarget(out var list))
                    continue;
                try
                {
                    if (TryGetListBinding(list, out var listTarget, out var listPath) && listTarget == target && listPath == path)
                        result.Add(list);
                }
                catch (Exception)
                {
                    // A list whose SerializedObject was disposed throws on access; it cannot be the one clicked.
                }
            }
            return result;
        }

        private static bool TryGetListBinding(ReorderableList list, out UnityEngine.Object target, out string path)
        {
            if (_listPropertyPathField != null && _listSerializedObjectField != null)
            {
                var serializedObject = _listSerializedObjectField.GetValue(list) as SerializedObject;
                target = serializedObject?.targetObject;
                path = _listPropertyPathField.GetValue(list) as string;
            }
            else
            {
                var property = list.serializedProperty;
                target = property?.serializedObject.targetObject;
                path = property?.propertyPath;
            }
            return target != null && path != null;
        }

        private static bool Copy(SerializedProperty collection, List<int> indices)
        {
            collection.serializedObject.Update();

            var values = new List<object>(indices.Count);
            try
            {
                foreach (var i in indices)
                    values.Add(collection.GetArrayElementAtIndex(i).boxedValue);
            }
            catch (Exception e)
            {
                $"Copy Elements failed: {e.Message}".printWarning();
                return false;
            }

            _clipboard.Clear();
            _clipboard.AddRange(values);
            _clipboardElementType = collection.arrayElementType;
            return true;
        }

        private static void Cut(SerializedProperty collection, List<int> indices, Action clearSelection)
        {
            if (!Copy(collection, indices))
                return;

            var serializedObject = collection.serializedObject;
            for (int k = indices.Count - 1; k >= 0; k--)
                collection.DeleteArrayElementAtIndex(indices[k]);

            Undo.SetCurrentGroupName("Cut Elements");
            serializedObject.ApplyModifiedProperties();

            clearSelection?.Invoke();
        }

        private static void Paste(SerializedProperty collection, int insertAt)
        {
            var serializedObject = collection.serializedObject;
            serializedObject.Update();
            insertAt = Mathf.Clamp(insertAt, 0, collection.arraySize);

            try
            {
                for (int k = 0; k < _clipboard.Count; k++)
                {
                    collection.InsertArrayElementAtIndex(insertAt + k);
                    var element = collection.GetArrayElementAtIndex(insertAt + k);
                    element.boxedValue = element.propertyType == SerializedPropertyType.ManagedReference
                        ? CloneManagedReference(_clipboard[k])
                        : _clipboard[k];
                }
            }
            catch (Exception e)
            {
                $"Paste Elements failed: {e.Message}".printWarning();
                serializedObject.Update();
                return;
            }

            Undo.SetCurrentGroupName("Paste Elements");
            serializedObject.ApplyModifiedProperties();
        }

        // Pasting the same managed object twice would make both elements share one reference.
        private static object CloneManagedReference(object value)
            => value == null ? null : JsonUtility.FromJson(JsonUtility.ToJson(value), value.GetType());
    }
}
