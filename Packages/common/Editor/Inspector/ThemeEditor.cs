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

        // ¥¿½T§R°£ ObjectReference array element
        array.DeleteArrayElementAtIndex(index);
        if (index < array.arraySize && array.GetArrayElementAtIndex(index).objectReferenceValue == null)
        {
            array.DeleteArrayElementAtIndex(index);
        }

        serializedObject.ApplyModifiedProperties();
        

    }

    private void ItemsList_added(ReorderableList list)
    {
        var type = itemsList.elementType;
        if (!typeof(ScriptableObject).IsAssignableFrom(type))
            return;
        GenericMenu menu = new GenericMenu();
        var types = itemsList.elementType.GetDerivedTypes().Where(t => !t.IsAbstract);
        if (!type.IsAbstract)
            menu.AddTypeItem(itemsList.serializedProperty, type,"");
        foreach (var derivedType in types)
            menu.AddTypeItem(itemsList.serializedProperty, derivedType, "");
        menu.ShowAsContext();
    }
 
    public override void OnInspectorGUI()
    {
        serializedObject.TryDrawScriptField();

        EditorGUI.BeginChangeCheck();
        itemsList.DoLayoutList();
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}