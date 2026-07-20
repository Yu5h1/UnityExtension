using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [CustomEditor(typeof(InvocationObject))]
    public class InvocationObjectEditor : UnityEditor.Editor
    {
        private SerializedProperty _path;
        private SerializedProperty _targetType;
        private SerializedProperty _methods;

        private void OnEnable()
        {
            _path = serializedObject.FindProperty("_path");
            _targetType = serializedObject.FindProperty("_targetType");
            _methods = serializedObject.FindProperty("_methods");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_path);
            EditorGUILayout.PropertyField(_targetType);
            EditorGUILayout.Space();

            var targetType = GetSerializedType(_targetType);
            if (targetType == null)
            {
                EditorGUILayout.HelpBox("Select a target type first.", MessageType.Info);
            }
            else
            {
                DrawMethods();
                if (GUILayout.Button("Add Method"))
                    ShowMethodMenu(targetType);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMethods()
        {
            EditorGUILayout.LabelField("Methods", EditorStyles.boldLabel);

            var removeIndex = -1;
            for (int i = 0; i < _methods.arraySize; i++)
            {
                var descriptor = _methods.GetArrayElementAtIndex(i);
                var methodName = descriptor.FindPropertyRelative("_methodName");
                var parameters = descriptor.FindPropertyRelative("_parameters");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(GetDescriptorLabel(methodName.stringValue, parameters), EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(i == 0))
                {
                    if (GUILayout.Button("▲", GUILayout.Width(24f)))
                        _methods.MoveArrayElement(i, i - 1);
                }

                using (new EditorGUI.DisabledScope(i >= _methods.arraySize - 1))
                {
                    if (GUILayout.Button("▼", GUILayout.Width(24f)))
                        _methods.MoveArrayElement(i, i + 1);
                }

                if (GUILayout.Button("×", GUILayout.Width(24f)))
                    removeIndex = i;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(parameters, true);
                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
                RemoveMethod(removeIndex);
        }

        private void ShowMethodMenu(Type targetType)
        {
            var methods = MethodOptionUtility.GetSupportedMethods(targetType);
            var menu = new GenericMenu();

            if (methods.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No supported public methods"));
            }
            else
            {
                foreach (var method in methods)
                {
                    var capturedMethod = method;
                    menu.AddItem(
                        new GUIContent(MethodOptionUtility.GetSignature(method)),
                        false,
                        () => AddMethod(capturedMethod));
                }
            }

            menu.ShowAsContext();
        }

        private void AddMethod(MethodInfo method)
        {
            serializedObject.Update();

            var index = _methods.arraySize;
            _methods.arraySize++;

            var descriptor = _methods.GetArrayElementAtIndex(index);
            descriptor.FindPropertyRelative("_methodName").stringValue = method.Name;

            var parameters = descriptor.FindPropertyRelative("_parameters");
            var methodParameters = method.GetParameters();
            parameters.arraySize = methodParameters.Length;

            for (int i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];
                var parameterObject = SubAssetUtility.CreateParameter(
                    parameter.ParameterType,
                    target,
                    $"{target.name}.{method.Name}.{parameter.Name}");

                parameters.GetArrayElementAtIndex(i).objectReferenceValue = parameterObject;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private void RemoveMethod(int index)
        {
            var descriptor = _methods.GetArrayElementAtIndex(index);
            var parameters = descriptor.FindPropertyRelative("_parameters");

            for (int i = parameters.arraySize - 1; i >= 0; i--)
            {
                var parameter = parameters.GetArrayElementAtIndex(i).objectReferenceValue as ParameterObject;
                if (parameter != null)
                    SubAssetUtility.RemoveSubAsset(parameter);
            }

            _methods.DeleteArrayElementAtIndex(index);
        }

        private static string GetDescriptorLabel(string methodName, SerializedProperty parameters)
        {
            var typeNames = new string[parameters.arraySize];
            for (int i = 0; i < parameters.arraySize; i++)
            {
                var parameter = parameters.GetArrayElementAtIndex(i).objectReferenceValue as ParameterObject;
                typeNames[i] = parameter?.DeclaredType?.Name ?? "?";
            }

            return $"{methodName} ({string.Join(", ", typeNames)})";
        }

        private static Type GetSerializedType(SerializedProperty typeProperty)
        {
            var typeName = typeProperty
                ?.FindPropertyRelative("_assemblyQualifiedName")
                ?.stringValue;

            return string.IsNullOrEmpty(typeName) ? null : Type.GetType(typeName);
        }
    }
}
