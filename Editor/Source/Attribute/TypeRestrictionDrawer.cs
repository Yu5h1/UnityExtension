using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [CustomPropertyDrawer(typeof(TypeRestrictionAttribute))]
    public class TypeRestrictionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var typeRestriction = (TypeRestrictionAttribute)attribute;

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                Object obj = property.objectReferenceValue;

                Object newObject = EditorGUI.ObjectField(position, label, obj, typeof(Object), typeRestriction.allowSceneObjects);

                if (newObject != obj)
                {
                    var objType = newObject?.GetType();
                    if (objType != null && !typeRestriction.allowedTypes.Any(t => t == objType || t.IsAssignableFrom(objType) ))
                        $"{objType} was not allowed".printWarning();
                    else
                        property.objectReferenceValue = newObject;
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}
