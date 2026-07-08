using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib.EditorExtension
{
    [CustomPropertyDrawer(typeof(ArgumentInfo))]
    public class ArgumentInfoDrawer : PropertyDrawer
    {
        private const float VSpace = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            var y = line.yMax + VSpace;
            DrawProperty(ref y, position, property, "_targetName", "Target Name");
            DrawProperty(ref y, position, property, "_methodName", "Method Name");
            DrawProperty(ref y, position, property, "_listenerMode", "Listener Mode");

            var modeProperty = property.FindPropertyRelative("_listenerMode");
            var mode = GetListenerMode(modeProperty);

            switch (mode)
            {
                case PersistentListenerMode.Object:
                    DrawProperty(ref y, position, property, "_argumentAssemblyTypeName", "Argument Type");
                    DrawObjectArgument(ref y, position, property);
                    break;

                case PersistentListenerMode.Int:
                    DrawProperty(ref y, position, property, "_intArgument", "Int Argument");
                    break;

                case PersistentListenerMode.Float:
                    DrawProperty(ref y, position, property, "_floatArgument", "Float Argument");
                    break;

                case PersistentListenerMode.String:
                    DrawProperty(ref y, position, property, "_stringArgument", "String Argument");
                    break;

                case PersistentListenerMode.Bool:
                    DrawProperty(ref y, position, property, "_boolArgument", "Bool Argument");
                    break;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
                return height;

            height += VSpace;
            height += GetPropertyHeight(property, "_targetName");
            height += GetPropertyHeight(property, "_methodName");
            height += GetPropertyHeight(property, "_listenerMode");

            var mode = GetListenerMode(property.FindPropertyRelative("_listenerMode"));
            switch (mode)
            {
                case PersistentListenerMode.Object:
                    height += GetPropertyHeight(property, "_argumentAssemblyTypeName");
                    height += GetPropertyHeight(property, "_objectArgument");
                    break;

                case PersistentListenerMode.Int:
                    height += GetPropertyHeight(property, "_intArgument");
                    break;

                case PersistentListenerMode.Float:
                    height += GetPropertyHeight(property, "_floatArgument");
                    break;

                case PersistentListenerMode.String:
                    height += GetPropertyHeight(property, "_stringArgument");
                    break;

                case PersistentListenerMode.Bool:
                    height += GetPropertyHeight(property, "_boolArgument");
                    break;
            }

            return height - VSpace;
        }

        private static void DrawProperty(
            ref float y,
            Rect position,
            SerializedProperty parent,
            string propertyName,
            string label)
        {
            var property = parent.FindPropertyRelative(propertyName);
            if (property == null)
                return;

            var height = EditorGUI.GetPropertyHeight(property, true);
            var rect = new Rect(position.x, y, position.width, height);
            EditorGUI.PropertyField(rect, property, new GUIContent(label), true);
            y += height + VSpace;
        }

        private static void DrawObjectArgument(ref float y, Rect position, SerializedProperty parent)
        {
            var objectProperty = parent.FindPropertyRelative("_objectArgument");
            if (objectProperty == null)
                return;

            var objectType = GetObjectArgumentType(parent);
            var label = new GUIContent($"Object Argument ({objectType.Name})");
            var height = EditorGUI.GetPropertyHeight(objectProperty, true);
            var rect = new Rect(position.x, y, position.width, height);
            var allowSceneObjects = !EditorUtility.IsPersistent(parent.serializedObject.targetObject);

            objectProperty.objectReferenceValue = EditorGUI.ObjectField(
                rect,
                label,
                objectProperty.objectReferenceValue,
                objectType,
                allowSceneObjects);

            y += height + VSpace;
        }

        private static float GetPropertyHeight(SerializedProperty parent, string propertyName)
        {
            var property = parent.FindPropertyRelative(propertyName);
            return property != null
                ? EditorGUI.GetPropertyHeight(property, true) + VSpace
                : 0f;
        }

        private static PersistentListenerMode GetListenerMode(SerializedProperty property)
        {
            if (property == null ||
                property.enumValueIndex < 0 ||
                property.enumValueIndex >= property.enumNames.Length)
                return default(PersistentListenerMode);

            var enumName = property.enumNames[property.enumValueIndex];
            return Enum.TryParse(enumName, out PersistentListenerMode mode)
                ? mode
                : default(PersistentListenerMode);
        }

        private static Type GetObjectArgumentType(SerializedProperty parent)
        {
            var typeNameProperty = parent.FindPropertyRelative("_argumentAssemblyTypeName");
            var typeName = typeNameProperty?.stringValue;
            var type = string.IsNullOrEmpty(typeName)
                ? null
                : Type.GetType(typeName);

            return type != null && typeof(UnityEngine.Object).IsAssignableFrom(type)
                ? type
                : typeof(UnityEngine.Object);
        }
    }
}
