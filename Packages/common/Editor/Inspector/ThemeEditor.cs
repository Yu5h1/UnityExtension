using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Yu5h1Lib.EditorExtension;
using Yu5h1Lib;
using UnityEditorInternal;
using System.Reflection;
using System.Linq;

[CustomEditor(typeof(Theme))]
public class ThemeEditor : Editor<Theme>
{
    [InitializeOnLoadMethod]
    static void Initialize(){
        InputDialog.confirmed += InputDialog_confirmed;
    }

    private static void InputDialog_confirmed(object obj)
    {
        if (obj is not Theme theme)
            return;
        OnThemeChanged(theme);
    }

    ReorderableListEnhanced itemsList;
    private void OnEnable()
    {
        ReorderableListEnhanced.TryCreate(serializedObject, "_items", out itemsList);
        itemsList.added += ItemsList_added;
        itemsList.removed += ItemsList_removed;
        reorderableLists["_items"] = itemsList;
    }

    private void ItemsList_removed(ReorderableList list)
    {
        if (list.index < 0)
            return;

        Undo.RegisterCompleteObjectUndo(targetObject, "Remove Items");

        var array = itemsList.serializedProperty;

        // 取得所有選取的 index，從大到小排序，避免刪除時 index 偏移
        var selectedIndices = list.selectedIndices.Count > 0
            ? new List<int>(list.selectedIndices)
            : new List<int> { list.index };

        selectedIndices.Sort((a, b) => b.CompareTo(a)); // 由大到小

        foreach (int index in selectedIndices)
        {
            if (index < 0 || index >= array.arraySize)
                continue;

            var element = array.GetArrayElementAtIndex(index);
            var obj = element.objectReferenceValue;

            if (obj is ParameterObject pobj && AssetDatabase.IsSubAsset(pobj))
            {
                SubAssetTransaction.Remove(pobj);
            }

            array.DeleteArrayElementAtIndex(index);
        }

        serializedObject.ApplyModifiedProperties();
        NotifyThemeChanged();
    }

    private void ItemsList_added(ReorderableList list)
    {
        var type = itemsList.elementType;
        if (!typeof(ScriptableObject).IsAssignableFrom(type))
            return;
        GenericMenu menu = new GenericMenu();
        var types = itemsList.elementType.GetDerivedTypes().Where(t => !t.IsAbstract);
        if (!type.IsAbstract)
            menu.AddTypeItem(itemsList.serializedProperty, type,"", NotifyThemeChanged);
        foreach (var derivedType in types)
            menu.AddTypeItem(itemsList.serializedProperty, derivedType, "", NotifyThemeChanged);
        menu.ShowAsContext();
    }
    void NotifyThemeChanged() { OnThemeChanged(targetObject); }

    static void OnThemeChanged(Theme source) 
    {
        var guids = AssetDatabase.FindAssets("t:Theme");
        foreach (var guid in guids) 
        { 
            var p = AssetDatabase.GUIDToAssetPath(guid); 
            var theme = AssetDatabase.LoadAssetAtPath<Theme>(p);
            if (theme == null || theme.schema != source)
                continue;
            OnSchemaChanged(theme);
        }
    }
    static void OnSchemaChanged(Theme profile)
    {        
        if (profile.schema == null) return;

        var schemaKeys = profile.schema.items
            .Where(item => item )
            .Select(item => item.name)
            .ToHashSet();

        var myKeys = profile.items
            .Where(item => item )
            .Select(item => item.name)
            .ToHashSet();

        // �h�l���R��
        var extras = myKeys.Except(schemaKeys).ToList();

        // �ʤ֪�
        var missings = schemaKeys.Except(myKeys).ToList();

        if (extras.Count > 0 || missings.Count > 0)
        {
            // ���t��G�����ۦP�� extra �M missing �� Rename
            var renames = new List<(ParameterObject oldItem, ParameterObject schemaItem)>();
            var extrasToRemove = new List<string>(extras);
            var missingsToAdd = new List<string>(missings);

            foreach (var extraKey in extras)
            {
                var extraItem = profile.items.First(i => i?.name == extraKey);

                // �������ۦP�� missing
                foreach (var missingKey in missingsToAdd.ToList())
                {
                    var schemaItem = profile.schema.items.First(i => i.name == missingKey);

                    if (extraItem.GetType() == schemaItem.GetType())
                    {
                        // �t�令�\ �� Rename
                        renames.Add((extraItem, schemaItem));
                        extrasToRemove.Remove(extraKey);
                        missingsToAdd.Remove(missingKey);
                        break;
                    }
                }
            }

            // �B�z Rename�]�O�d�ȡA��W�^
            foreach (var (oldItem, schemaItem) in renames)
            {
                oldItem.name = schemaItem.name;
            }

            // �B�z�R��
            foreach (var extra in extrasToRemove)
            {
                var item = profile.items.First(i => i.name == extra);
                profile.items.Remove(item);
                AssetDatabase.RemoveObjectFromAsset(item);
                Object.DestroyImmediate(item, true);
            }

            // �B�z�s�W
            foreach (var key in missingsToAdd)
            {
                var schemaItem = profile.schema.items.First(item => item.name == key);
                var copy = Object.Instantiate(schemaItem);
                copy.name = schemaItem.name;
                AssetDatabase.AddObjectToAsset(copy, profile);
                profile.items.Add(copy);
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }
    }
}