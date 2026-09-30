using System;
using System.Collections;
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
        private static readonly string[] BuiltInElementItems = { "Copy", "Paste", "Duplicate Array Element", "Delete Array Element" };

        // Unity adds its items before invoking contextualPropertyMenu, so this list is where they can be replaced.
        private static readonly FieldInfo _menuItemsField =
            typeof(GenericMenu).GetField("m_MenuItems", BindingFlags.Instance | BindingFlags.NonPublic);

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
                AddPasteItem(menu, "Paste Elements", collection, collection.arraySize);
                if (TryGetMenuItems(menu, out var items, out _, out var separatorField))
                    CollapseSeparators(items, separatorField);
            }
            else if (TryGetOwningCollection(property, out var collection, out int index))
            {
                var indices = ResolveTargetIndices(collection, index, out var clearSelection);
                if (!TryReplaceBuiltInItems(menu, collection, index, indices, clearSelection))
                {
                    menu.AddSeparator("");
                    AddElementItems(menu, $"Elements Action ({indices.Count})/", collection, index, indices, clearSelection);
                }
            }
        }

        private static void AddElementItems(GenericMenu menu, string prefix, SerializedProperty collection, int index, List<int> indices, Action clearSelection)
        {
            menu.AddItem(new GUIContent(prefix + "Copy"), false, () => Copy(collection, indices));
            menu.AddItem(new GUIContent(prefix + "Cut"), false, () => Cut(collection, indices, clearSelection));
            AddPasteItem(menu, prefix + "Paste", collection, index);
            menu.AddItem(new GUIContent(prefix + "Duplicate"), false, () => Duplicate(collection, indices));
            menu.AddItem(new GUIContent(prefix + "Delete"), false, () => Delete(collection, indices, clearSelection));
        }

        private static bool TryReplaceBuiltInItems(GenericMenu menu, SerializedProperty collection, int index, List<int> indices, Action clearSelection)
        {
            if (!TryGetMenuItems(menu, out var items, out var contentField, out var separatorField))
                return false;

            int insertAt = -1;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (Array.IndexOf(BuiltInElementItems, (contentField.GetValue(items[i]) as GUIContent)?.text) < 0)
                    continue;
                items.RemoveAt(i);
                insertAt = i;
            }
            if (insertAt < 0)
            {
                menu.AddSeparator("");
                insertAt = items.Count;
            }

            int appendedFrom = items.Count;
            AddElementItems(menu, "", collection, index, indices, clearSelection);
            var added = new List<object>();
            for (int i = appendedFrom; i < items.Count; i++)
                added.Add(items[i]);
            for (int i = items.Count - 1; i >= appendedFrom; i--)
                items.RemoveAt(i);
            for (int i = 0; i < added.Count; i++)
                items.Insert(insertAt + i, added[i]);

            CollapseSeparators(items, separatorField);
            return true;
        }

        private static bool TryGetMenuItems(GenericMenu menu, out IList items, out FieldInfo contentField, out FieldInfo separatorField)
        {
            contentField = separatorField = null;
            items = _menuItemsField?.GetValue(menu) as IList;
            if (items == null || items.Count == 0)
                return false;
            var itemType = items[0].GetType();
            contentField = itemType.GetField("content");
            separatorField = itemType.GetField("separator");
            return contentField != null && separatorField != null;
        }

        private static void CollapseSeparators(IList items, FieldInfo separatorField)
        {
            bool previousIsSeparator = true;
            for (int i = 0; i < items.Count; i++)
            {
                bool isSeparator = (bool)separatorField.GetValue(items[i]);
                if (isSeparator && previousIsSeparator)
                    items.RemoveAt(i--);
                else
                    previousIsSeparator = isSeparator;
            }
            if (items.Count > 0 && (bool)separatorField.GetValue(items[items.Count - 1]))
                items.RemoveAt(items.Count - 1);
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

        private static void AddPasteItem(GenericMenu menu, string label, SerializedProperty collection, int insertAt)
        {
            if (_clipboard.Count == 0 || collection.arrayElementType != _clipboardElementType)
                menu.AddDisabledItem(new GUIContent(label));
            else
                menu.AddItem(new GUIContent(label), false, () => InsertValues(collection, insertAt, _clipboard, "Paste Elements"));
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

        private static bool TryReadValues(SerializedProperty collection, List<int> indices, string action, out List<object> values)
        {
            collection.serializedObject.Update();
            values = new List<object>(indices.Count);
            try
            {
                foreach (var i in indices)
                    values.Add(collection.GetArrayElementAtIndex(i).boxedValue);
                return true;
            }
            catch (Exception e)
            {
                $"{action} Elements failed: {e.Message}".printWarning();
                return false;
            }
        }

        private static bool Copy(SerializedProperty collection, List<int> indices)
        {
            if (!TryReadValues(collection, indices, "Copy", out var values))
                return false;
            _clipboard.Clear();
            _clipboard.AddRange(values);
            _clipboardElementType = collection.arrayElementType;
            return true;
        }

        private static void Duplicate(SerializedProperty collection, List<int> indices)
        {
            if (TryReadValues(collection, indices, "Duplicate", out var values))
                InsertValues(collection, indices[indices.Count - 1] + 1, values, "Duplicate Elements");
        }

        private static void Cut(SerializedProperty collection, List<int> indices, Action clearSelection)
        {
            if (Copy(collection, indices))
                RemoveElements(collection, indices, clearSelection, "Cut Elements");
        }

        private static void Delete(SerializedProperty collection, List<int> indices, Action clearSelection)
        {
            collection.serializedObject.Update();
            RemoveElements(collection, indices, clearSelection, "Delete Elements");
        }

        private static void RemoveElements(SerializedProperty collection, List<int> indices, Action clearSelection, string undoName)
        {
            for (int k = indices.Count - 1; k >= 0; k--)
                collection.DeleteArrayElementAtIndex(indices[k]);

            Undo.SetCurrentGroupName(undoName);
            collection.serializedObject.ApplyModifiedProperties();

            clearSelection?.Invoke();
        }

        private static void InsertValues(SerializedProperty collection, int insertAt, List<object> values, string undoName)
        {
            var serializedObject = collection.serializedObject;
            serializedObject.Update();
            insertAt = Mathf.Clamp(insertAt, 0, collection.arraySize);

            try
            {
                for (int k = 0; k < values.Count; k++)
                {
                    collection.InsertArrayElementAtIndex(insertAt + k);
                    var element = collection.GetArrayElementAtIndex(insertAt + k);
                    element.boxedValue = element.propertyType == SerializedPropertyType.ManagedReference
                        ? CloneManagedReference(values[k])
                        : values[k];
                }
            }
            catch (Exception e)
            {
                $"{undoName} failed: {e.Message}".printWarning();
                serializedObject.Update();
                return;
            }

            Undo.SetCurrentGroupName(undoName);
            serializedObject.ApplyModifiedProperties();
        }

        // Inserting the same managed object twice would make both elements share one reference.
        private static object CloneManagedReference(object value)
            => value == null ? null : JsonUtility.FromJson(JsonUtility.ToJson(value), value.GetType());
    }
}
