//using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Type = System.Type;

namespace Yu5h1Lib.EditorExtension
{
    [CustomEditor(typeof(GenericPresetObject))]
    public class GenericPresetObjectObjectEditor : Editor<GenericPresetObject>
    {
        SerializedProperty valueProp;
        SerializedProperty targetAssemblyProp;
        SerializedProperty targetTypeProp;
        SerializedProperty propertiesProp;

        Object MainAsset => SubAssetUtility.GetMainAsset(target);

        void OnEnable()
        {
            valueProp = serializedObject.FindProperty("value");
            if (valueProp == null) return;
            targetAssemblyProp = valueProp.FindPropertyRelative("targetAssembly");
            targetTypeProp = valueProp.FindPropertyRelative("targetType");
            propertiesProp = valueProp.FindPropertyRelative("properties");
        }

        public override void OnInspectorGUI()
        {
            if (valueProp == null)
            {
                base.OnInspectorGUI();
                return;
            }
            DrawMonoScript();
            serializedObject.Update();

            // 1. Draw targetAssembly
            EditorGUILayout.PropertyField(targetAssemblyProp);

            // 2. Draw targetType (detect change)
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(targetTypeProp);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (propertiesProp.arraySize > 0)
                {
                    if (EditorUtility.DisplayDialog("Type Changed",
                        "Clear existing Properties?", "Clear", "Keep"))
                    {
                        ClearAllProperties();
                    }
                }
            }

            EditorGUILayout.Space();

            // 3. Get target type
            var targetType = GetTargetType();

            if (targetType == null)
            {
                EditorGUILayout.HelpBox("Select a target type first.", MessageType.Info);
            }
            else
            {
                DrawDynamicProperties(targetType);
            }

            serializedObject.ApplyModifiedProperties();
        }

        Type GetTargetType()
        {
            var aqnProp = targetTypeProp.FindPropertyRelative("_assemblyQualifiedName");
            var aqn = aqnProp?.stringValue;

            if (string.IsNullOrEmpty(aqn)) return null;
            return Type.GetType(aqn);
        }

        void DrawDynamicProperties(Type targetType)
        {
            var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => ParameterObjectUtility.IsSupported(p.PropertyType))
                .OrderBy(p => p.Name);

            EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);

            foreach (var prop in properties)
            {
                DrawPropertyToggleField(prop);
            }
        }

        void DrawPropertyToggleField(PropertyInfo propInfo)
        {
            var presetName = targetObject.name;
            var subAssetName = $"{presetName}.{propInfo.Name}";

            var existing = FindExistingParameterObject(subAssetName);
            var isEnabled = existing != null;

            EditorGUILayout.BeginHorizontal();

            // Toggle
            var newEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));

            if (newEnabled != isEnabled)
            {
                if (newEnabled)
                {
                    var po = SubAssetUtility.CreateParameterSubAsset(
                        propInfo.PropertyType, MainAsset, subAssetName);
                    if (po != null)
                        AddToProperties(po);
                }
                else
                {
                    RemoveFromProperties(existing);
                    SubAssetUtility.RemoveSubAsset(existing);
                }
            }

            // Enabled: show ObjectField (Inline system handles the rest)
            // Disabled: greyed out label with type hint
            if (isEnabled && existing != null)
            {
                EditorGUILayout.ObjectField(existing, typeof(ParameterObject), false);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.LabelField(propInfo.Name, $"({propInfo.PropertyType.Name})");
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndHorizontal();
        }

        ParameterObject FindExistingParameterObject(string name)
        {
            for (int i = 0; i < propertiesProp.arraySize; i++)
            {
                var element = propertiesProp.GetArrayElementAtIndex(i);
                var po = element.objectReferenceValue as ParameterObject;
                if (po != null && po.name == name)
                    return po;
            }
            return null;
        }

        void AddToProperties(ParameterObject po)
        {
            propertiesProp.arraySize++;
            var element = propertiesProp.GetArrayElementAtIndex(propertiesProp.arraySize - 1);
            element.objectReferenceValue = po;
            serializedObject.ApplyModifiedProperties();
        }

        void RemoveFromProperties(ParameterObject po)
        {
            for (int i = propertiesProp.arraySize - 1; i >= 0; i--)
            {
                var element = propertiesProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == po)
                {
                    propertiesProp.DeleteArrayElementAtIndex(i);
                    // Unity quirk: first delete sets to null, need second delete to remove slot
                    if (i < propertiesProp.arraySize
                        && propertiesProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        propertiesProp.DeleteArrayElementAtIndex(i);
                    }
                    break;
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        void ClearAllProperties()
        {
            for (int i = propertiesProp.arraySize - 1; i >= 0; i--)
            {
                var element = propertiesProp.GetArrayElementAtIndex(i);
                var po = element.objectReferenceValue as ParameterObject;
                if (po != null)
                {
                    SubAssetUtility.RemoveSubAsset(po);
                }
            }
            propertiesProp.ClearArray();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
