using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    using Mode = TypeRestrictionAttribute.Mode;
    [CustomPropertyDrawer(typeof(TypeRestrictionAttribute))]
    public class TypeRestrictionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var att = (TypeRestrictionAttribute)attribute;

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                Object obj = property.objectReferenceValue;

                var newObject = obj;

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(position, property);
                if (EditorGUI.EndChangeCheck())
                {
                    newObject = property.objectReferenceValue;
                }

                if (newObject == obj)             
                    return;

                if (newObject is GameObject go)
                {
                    // 嘗試在 GameObject 上找符合條件的 Component
                    Component? matched = null;

                    foreach (var t in att.types)
                    {
                        matched = go.GetComponent(t);
                        if (matched != null)
                            break;
                    }

                    if (matched != null)
                        newObject = matched;
                }


                var objType = newObject?.GetType();
                bool isValid = true;
                string message = string.Empty;

                if (objType != null)
                {
                    bool isExactMatch =
                        att.types.Any(t => t == objType);

                    bool isAssignableMatch =
                        att.types.Any(t => t == objType || t.IsAssignableFrom(objType));

                    isValid = att.mode switch
                    {
                        Mode.Include => isAssignableMatch,
                        Mode.Exclude => !isAssignableMatch,
                        Mode.Exact => isExactMatch,
                        _ => true
                    };

                    if (!isValid)
                    {
                        string typeList = string.Join(", ", att.types.Select(t => t.FullName));
                        string typeName = objType.Name;

                        message = att.mode switch
                        {
                            Mode.Include =>
                                $"{typeName} is not supported by this field.\n" +
                                $"Allowed types: {typeList}.",

                            Mode.Exclude =>
                                $"{typeName} is disallowed by this field.\n" +
                                $"Disallowed types: {typeList}.",

                            Mode.Exact =>
                                $"{typeName} is not an exact match.\n" +
                                $"Expected exact type: {typeList}.",

                            _ => string.Empty,
                        };
                    }
                }

                if (!isValid)
                {
                    newObject = null;
                    message?.printWarning();
                }
                if (property.objectReferenceValue != newObject)
                {
                    property.objectReferenceValue = newObject;
                    property.serializedObject.ApplyModifiedProperties();
                }
                
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}
