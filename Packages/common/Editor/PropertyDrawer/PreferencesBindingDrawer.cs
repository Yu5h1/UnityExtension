using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [CustomPropertyDrawer(typeof(PreferencesBindingAttribute))]
    public class PreferencesBindingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var previous = property.objectReferenceValue;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, property, label);
            if (!EditorGUI.EndChangeCheck())
                return;

            var assigned = property.objectReferenceValue;
            if (assigned == null || assigned == previous)
                return;

            var resolved = PreferencesBindingUtility.ResolveBindableComponent(assigned);
            if (resolved == null)
            {
                $"{assigned.name} is not an IValuePort and has no registered Adapter; rejected.".printWarning();
                property.objectReferenceValue = previous;
                property.serializedObject.ApplyModifiedProperties();
                return;
            }

            property.objectReferenceValue = resolved;
            property.serializedObject.ApplyModifiedProperties();

            if (PreferencesBindingUtility.IsDuplicateName(property, resolved, out var duplicateIndex))
                $"Field name '{resolved.gameObject.name}' is already bound at index {duplicateIndex}.".printWarning();
        }
    }
}
