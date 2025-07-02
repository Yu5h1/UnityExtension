using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib.EditorExtension;

namespace Yu5h1Lib.CommonEditor
{
    public static class GeneralContextualPropertyMenu
    {
        [InitializeOnLoadMethod]
        public static void Register()
        {
            EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }
        private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.propertyPath.Contains("m_MethodName"))
            {
                var persistentCallProperty = GetPersistentCallProperty(property);
                if (persistentCallProperty != null)
                {
                    if (IsScriptableObjectArgument(persistentCallProperty))
                    {
                        menu.AddItem(new GUIContent("Instantiate SO"), false, () =>
                        {
                            InstantiateAndAssignScriptableObject(persistentCallProperty);
                        });
                    }
                }
            }
        }

        private static SerializedProperty GetPersistentCallProperty(SerializedProperty methodNameProperty)
        {
            string path = methodNameProperty.propertyPath;
            string persistentCallPath = path.Replace(".m_MethodName", "");
            return methodNameProperty.serializedObject.FindProperty(persistentCallPath);
        }

        private static bool IsScriptableObjectArgument(SerializedProperty persistentCallProperty)
        {
            var methodNameProp = persistentCallProperty.FindPropertyRelative("m_MethodName");
            if (methodNameProp == null || string.IsNullOrEmpty(methodNameProp.stringValue))
                return false;

            var targetProp = persistentCallProperty.FindPropertyRelative("m_Target");
            if (targetProp == null || targetProp.objectReferenceValue == null)
                return false;

            var callStateProp = persistentCallProperty.FindPropertyRelative("m_CallState");
            if (callStateProp == null || callStateProp.enumValueIndex != 2) // 2 = RuntimeOnly
                return false;

            try
            {
                Type targetType = targetProp.objectReferenceValue.GetType();
                MethodInfo method = targetType.GetMethod(methodNameProp.stringValue);

                if (method != null)
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 1)
                    {
                        Type paramType = parameters[0].ParameterType;
                        return typeof(ScriptableObject).IsAssignableFrom(paramType);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to check method parameter type: {e.Message}");
            }

            return false;
        }

        private static void InstantiateAndAssignScriptableObject(SerializedProperty persistentCallProperty)
        {
            try
            {
                var methodNameProp = persistentCallProperty.FindPropertyRelative("m_MethodName");
                var targetProp = persistentCallProperty.FindPropertyRelative("m_Target");

                if (targetProp.objectReferenceValue == null) return;

                Type targetType = targetProp.objectReferenceValue.GetType();
                MethodInfo method = targetType.GetMethod(methodNameProp.stringValue);

                if (method != null)
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 1)
                    {
                        Type paramType = parameters[0].ParameterType;

                        if (typeof(ScriptableObject).IsAssignableFrom(paramType))
                        {
                            ScriptableObject newSO = ScriptableObject.CreateInstance(paramType);
                            newSO.name = $"{paramType.Name}(Instance)";



                                //string path = EditorUtility.SaveFilePanel(
                                //    "Save ScriptableObject",
                                //    "Assets",
                                //    $"New{paramType.Name}",
                                //    "asset");

                            //if (!string.IsNullOrEmpty(path))
                            //{
                            //path = FileUtil.GetProjectRelativePath(path);

                            //AssetDatabase.CreateAsset(newSO, path);
                            //AssetDatabase.SaveAssets();

                                var argumentProp = persistentCallProperty.FindPropertyRelative("m_Arguments");
                                if (argumentProp != null)
                                {
                                    var objectArgumentProp = argumentProp.FindPropertyRelative("m_ObjectArgument");
                                    if (objectArgumentProp != null)
                                    {
                                        objectArgumentProp.objectReferenceValue = newSO;
                                        persistentCallProperty.serializedObject.ApplyModifiedProperties();

                                        Debug.Log($"Created and assigned {paramType.Name} to UnityEvent argument");
                                    }
                                }
                            //}
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to instantiate ScriptableObject: {e.Message}");
            }
        }

    }
}