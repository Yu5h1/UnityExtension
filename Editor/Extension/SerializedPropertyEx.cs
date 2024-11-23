using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    public static class SerializedPropertyEx
    {
        public static object GetValue(this SerializedProperty property)
        {            
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            string propertyPath = property.propertyPath.Replace(".Array.data", "");
            object targetobject = property.serializedObject.targetObject;
            var targetType = property.serializedObject.targetObject.GetType();
            if (propertyPath.Contains("."))
            {
                FieldInfo curfield = null;
                var hierarchys = propertyPath.Split('.');
                foreach (var hierarchylevel in hierarchys)
                {
                    if (hierarchylevel.Contains("["))
                    {
                        var arrayParts = hierarchylevel.Split('[');
                        curfield = targetType.GetField(arrayParts[0], bindingFlags);
                        //int index = int.Parse(arrayParts[1].Remove(']'));
                        //object[] arrayobj = (object[])curfield.GetValue(targetobject);
                        //targetobject = arrayobj[index];
                        Debug.Log(hierarchylevel);
                        //targetobject = curfield.GetValue(targetobject);
                        
                        targetobject = null;
                        targetType = curfield.FieldType.GetElementType();

                        
                    }
                    else
                    {
                        curfield = targetType.GetField(hierarchylevel, bindingFlags);
                        targetobject = curfield.GetValue(targetobject);
                        targetType = curfield.FieldType;
                    }
                }
                return targetobject;
            }
            return targetType.GetField(property.propertyPath, bindingFlags).GetValue(targetobject);
        }
        public static T GetValue<T>(this SerializedProperty property)
        { return (T)property.GetValue(); }
         public static Type GetEnumType(this SerializedProperty property)
        { return property.GetValue().GetType(); }

        public static void SetValue(this SerializedProperty property, object value)
        {            
            if (property.isArray && value.GetType().IsArray) {
                if (property.arrayElementType == value.GetType().GetElementType().Name)
                {                    
                    var ArrayValue = value as object[];
                    property.arraySize = ArrayValue.Length;
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        property.GetArrayElementAtIndex(i).SetValue(ArrayValue[i]);
                    }
                }
                else
                {
                    Debug.LogWarning(property.arrayElementType + " can not convert to " + value.GetType().GetElementType().Name);
                }
            }
            else if (property.propertyType == SerializedPropertyType.Generic)
            {
                var refProperties = value.GetType().GetProperties();
                property.PropertiesAction(true, p =>
                {
                    string[] splitNames = p.propertyPath.Split('.');
                    string curPropName = splitNames[splitNames.Length - 1];
                    foreach (var reflectProp in refProperties)
                    {
                        if (curPropName == reflectProp.Name)
                        {
                            SetValue(p, reflectProp.GetValue(value, null));
                            break;
                        }
                    }
                });
            }
            else if (value is int) { property.intValue = (int)value; }
            else if (value is bool) { property.boolValue = (bool)value; }
            else if (value is float) { property.floatValue = (float)value; }
            else if (value is string) { property.stringValue = (string)value; }
            else if (value is long) { property.longValue = (long)value; }
            else if (value is Vector2) { property.vector2Value = (Vector2)value; }
            else if (value is Vector3) { property.vector3Value = (Vector3)value; }
            else if (value is Vector4) { property.vector4Value = (Vector4)value; }
            else if (value is Rect) { property.rectValue = (Rect)value; }
            else if (value is Quaternion) { property.quaternionValue = (Quaternion)value; }
            else if (value is Color) { property.colorValue = (Color)value; }
            else if (value is Bounds) { property.boundsValue = (Bounds)value; }
            else if (value is AnimationCurve) { property.animationCurveValue = (AnimationCurve)value; }
            else if (value is UnityEngine.Object) { property.objectReferenceValue = (UnityEngine.Object)value; }
            
        }
        public static void PropertiesAction(this SerializedProperty prop, bool enterChildren, System.Action<SerializedProperty> action)
        {
            while (prop.NextVisible(enterChildren)) { action(prop); }
        }
        public static void logPropertiesName(this SerializedProperty property, bool enterChildren) {
            property.PropertiesAction(enterChildren, prop => {
                Debug.Log(prop.name);
            });
        }
        public static string FindString(this SerializedProperty property,string name)
        {
            return property.FindPropertyRelative(name).stringValue;
        }
        public static void ResetValue(this SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Generic:
                    break;
                case SerializedPropertyType.Integer:
                    property.intValue = 0;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = false;
                    break;
                case SerializedPropertyType.Float:
                    if (property.propertyPath.ToLower().Contains("scale")) property.floatValue = 1;
                    else property.floatValue = 0;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = "";
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = Color.black;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    break;
                case SerializedPropertyType.LayerMask:
                    break;
                case SerializedPropertyType.Enum:
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = Vector2.zero;
                    break;
                case SerializedPropertyType.Vector3:
                    if (property.propertyPath.ToLower().Contains("scale")) property.vector3Value = Vector3.one;
                    else property.vector3Value = Vector3.zero;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = Vector4.zero;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = Rect.zero;
                    break;
                case SerializedPropertyType.ArraySize:
                    break;
                case SerializedPropertyType.Character:
                    break;
                case SerializedPropertyType.AnimationCurve:
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = default(Bounds);
                    break;
                case SerializedPropertyType.Gradient:
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = Quaternion.identity;
                    break;
                case SerializedPropertyType.ExposedReference:
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = Vector2Int.zero;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = Vector3Int.zero;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = default(RectInt);
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = default(BoundsInt);
                    break;
                case SerializedPropertyType.ManagedReference:
                    break;
            }
        }
                
    }
}
