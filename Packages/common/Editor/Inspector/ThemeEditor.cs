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
    static void Initinalize(){

        RenamePopup.confirmed += RenamePopup_confirmed;
    }

    private static void RenamePopup_confirmed(object obj)
    {
        //$"RenamePopup_confirmed {obj}".print();
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
    }

    private void ItemsList_removed(ReorderableList list)
    {
        if (list.index < 0)
            return;

        Undo.RegisterCompleteObjectUndo(targetObject, "Remove Item");

        var array = itemsList.serializedProperty;
        int index = list.index;

        var element = array.GetArrayElementAtIndex(index);
        var obj = element.objectReferenceValue;

        if (obj is ParameterObject pobj && AssetDatabase.IsSubAsset(pobj))
        {
            SubAssetTransaction.Remove(pobj);
        }

        // 正確刪除 ObjectReference array element
        array.DeleteArrayElementAtIndex(index);
        if (index < array.arraySize && array.GetArrayElementAtIndex(index).objectReferenceValue == null)
        {
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

    //public override void OnInspectorGUI()
    //{
    //    serializedObject.TryDrawScriptField();

    //    EditorGUI.BeginChangeCheck();
    //    itemsList.DoLayoutList();
    //    if (EditorGUI.EndChangeCheck())
    //    {
    //        serializedObject.ApplyModifiedProperties();
    //    }


    //}
    public override void DrawProperty(SerializedProperty property)
    {
        if (property.displayName == "Items")
        {
            EditorGUI.BeginChangeCheck();
            itemsList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
        else base.DrawProperty(property);
    }

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
        $"{profile.name}OnSchemaChanged".print();
        if (profile.schema == null) return;

        var schemaKeys = profile.schema.items
            .Where(item => item )
            .Select(item => item.name)
            .ToHashSet();

        var myKeys = profile.items
            .Where(item => item )
            .Select(item => item.name)
            .ToHashSet();

        // 多餘的刪掉
        var extras = myKeys.Except(schemaKeys).ToList();

        // 缺少的
        var missings = schemaKeys.Except(myKeys).ToList();

        if (extras.Count > 0 || missings.Count > 0)
        {
            // 先配對：類型相同的 extra 和 missing → Rename
            var renames = new List<(ParameterObject oldItem, ParameterObject schemaItem)>();
            var extrasToRemove = new List<string>(extras);
            var missingsToAdd = new List<string>(missings);

            foreach (var extraKey in extras)
            {
                var extraItem = profile.items.First(i => i?.name == extraKey);

                // 找類型相同的 missing
                foreach (var missingKey in missingsToAdd.ToList())
                {
                    var schemaItem = profile.schema.items.First(i => i.name == missingKey);

                    if (extraItem.GetType() == schemaItem.GetType())
                    {
                        // 配對成功 → Rename
                        renames.Add((extraItem, schemaItem));
                        extrasToRemove.Remove(extraKey);
                        missingsToAdd.Remove(missingKey);
                        break;
                    }
                }
            }

            // 處理 Rename（保留值，改名）
            foreach (var (oldItem, schemaItem) in renames)
            {
                oldItem.name = schemaItem.name;
            }

            // 處理刪除
            foreach (var extra in extrasToRemove)
            {
                var item = profile.items.First(i => i.name == extra);
                profile.items.Remove(item);
                AssetDatabase.RemoveObjectFromAsset(item);
                Object.DestroyImmediate(item, true);
            }

            // 處理新增
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