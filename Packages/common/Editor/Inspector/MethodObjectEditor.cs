using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [CustomEditor(typeof(MethodObject))]
    public class MethodObjectEditor : Editor<MethodObject>
    {
        private SerializedProperty _targetType;
        private SerializedProperty _parameters;

        private void OnEnable()
        {
            _targetType = serializedObject.FindProperty("_targetType");
            _parameters = serializedObject.FindProperty("_parameters");
        }

        public override void OnInspectorGUI()
        {
            DrawMonoScript();
            serializedObject.Update();

            EditorGUILayout.PropertyField(_targetType);
            var targetType = GetSerializedType(_targetType);

            if (targetType == null)
            {
                EditorGUILayout.HelpBox("Select a target type first.", MessageType.Info);
            }
            else
            {
                DrawMethodSelector(targetType);
                EditorGUILayout.PropertyField(_parameters, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMethodSelector(Type targetType)
        {
            var methods = MethodOptionUtility.GetSupportedMethods(targetType);
            var labels = new[] { "<Select Method>" }
                .Concat(methods.Select(MethodOptionUtility.GetSignature))
                .ToArray();

            var currentIndex = FindCurrentMethod(methods) + 1;
            var selectedIndex = EditorGUILayout.Popup("Method", currentIndex, labels);
            if (selectedIndex == currentIndex)
                return;

            if (selectedIndex == 0)
                ClearMethod();
            else
                SetMethod(methods[selectedIndex - 1]);
        }

        private int FindCurrentMethod(System.Collections.Generic.IReadOnlyList<MethodInfo> methods)
        {
            var parameterTypes = GetParameterTypes(_parameters);
            for (int i = 0; i < methods.Count; i++)
            {
                var method = methods[i];
                var methodParameters = method.GetParameters();
                if (method.Name != target.name || methodParameters.Length != parameterTypes.Length)
                    continue;

                var matches = true;
                for (int parameterIndex = 0; parameterIndex < methodParameters.Length; parameterIndex++)
                {
                    if (methodParameters[parameterIndex].ParameterType != parameterTypes[parameterIndex])
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    return i;
            }

            return -1;
        }

        private void SetMethod(MethodInfo method)
        {
            ClearParameters();

            Undo.RecordObject(target, "Set Method");
            target.name = method.Name;

            var methodParameters = method.GetParameters();
            _parameters.arraySize = methodParameters.Length;
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];
                var parameterObject = SubAssetUtility.CreateParameter(
                    parameter.ParameterType,
                    target,
                    $"{method.Name}.{parameter.Name}");

                _parameters.GetArrayElementAtIndex(i).objectReferenceValue = parameterObject;
            }

            EditorUtility.SetDirty(target);
        }

        private void ClearMethod()
        {
            ClearParameters();
            Undo.RecordObject(target, "Clear Method");
            target.name = string.Empty;
            EditorUtility.SetDirty(target);
        }

        private void ClearParameters()
        {
            for (int i = _parameters.arraySize - 1; i >= 0; i--)
            {
                var parameter = _parameters.GetArrayElementAtIndex(i).objectReferenceValue as ParameterObject;
                if (parameter != null)
                    SubAssetUtility.RemoveSubAsset(parameter);
            }

            _parameters.ClearArray();
        }

        private static Type GetSerializedType(SerializedProperty typeProperty)
        {
            var typeName = typeProperty
                ?.FindPropertyRelative("_assemblyQualifiedName")
                ?.stringValue;

            return string.IsNullOrEmpty(typeName) ? null : Type.GetType(typeName);
        }

        private static Type[] GetParameterTypes(SerializedProperty parameters)
        {
            var types = new Type[parameters.arraySize];
            for (int i = 0; i < parameters.arraySize; i++)
            {
                var parameter = parameters.GetArrayElementAtIndex(i).objectReferenceValue as ParameterObject;
                types[i] = parameter?.DeclaredType;
            }
            return types;
        }
    }
}
