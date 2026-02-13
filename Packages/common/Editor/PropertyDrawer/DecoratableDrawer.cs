using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// PropertyDrawer for [Decorable] fields.
    /// Checks DecoratorProvider for a registered draw method:
    ///   Path B (Inline SO): uses targetObject.instanceID to look up drawer type
    ///   Path A (normal class): traverses parent fields to find [Decorator] attribute
    /// Falls back to default PropertyField when no decorator is found.
    /// </summary>
    [CustomPropertyDrawer(typeof(DecorableAttribute))]
    public class DecoratableDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DecoratorProvider.DecoratorDraw drawMethod = null;

            // Path B: Inline SO — lookup by instanceID
            var targetObj = property.serializedObject.targetObject;
            if (targetObj is ScriptableObject)
            {
                DecoratorProvider.TryGetDrawMethod(targetObj.GetInstanceID(), out drawMethod);
            }

            // Path A: normal class — find [Decorator] on parent field via reflection
            if (drawMethod == null)
            {
                var decoratorType = FindDecoratorTypeFromParent(property);
                if (decoratorType != null)
                    DecoratorProvider.TryGetDrawMethod(decoratorType, out drawMethod);
            }

            if (drawMethod != null)
                drawMethod(this, position, property, label);
            else
                EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // For now, delegate height calculation to default.
            // Specific drawers that handle arrays will need GetPropertyHeight support
            // via a parallel height delegate in DecoratorProvider if needed.
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /// <summary>
        /// Traverse parent fields to find a [Decorator] attribute.
        /// Works for normal serializable classes (non-SO) where
        /// the property path chain is intact.
        /// </summary>
        private static System.Type FindDecoratorTypeFromParent(SerializedProperty property)
        {
            var parentProp = StringOptionsHelper.GetParentProperty(property);
            if (parentProp == null) return null;

            var parentFieldInfo = StringOptionsHelper.GetFieldInfo(parentProp);
            if (parentFieldInfo == null) return null;

            var decoratorAttr = parentFieldInfo.GetCustomAttribute<DecoratorAttribute>();
            return decoratorAttr?.DrawerType;
        }
    }
}
