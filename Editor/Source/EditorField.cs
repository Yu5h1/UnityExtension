using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    public static class EditorField
    {
        public static T Draw<T>(string label, T value)
        {
            switch (value)
            {
                case int intVal:
                    return (T)(object)EditorGUILayout.IntField(label, intVal);
                case float floatVal:
                    return (T)(object)EditorGUILayout.FloatField(label, floatVal);
                case bool boolVal:
                    return (T)(object)EditorGUILayout.Toggle(label, boolVal);
                case string strVal:
                    return (T)(object)EditorGUILayout.TextField(label, strVal);
                case Vector2 vec2:
                    return (T)(object)EditorGUILayout.Vector2Field(label, vec2);
                case Vector3 vec3:
                    return (T)(object)EditorGUILayout.Vector3Field(label, vec3);
                case Vector4 vec4:
                    return (T)(object)EditorGUILayout.Vector4Field(label, vec4);
                case Color color:
                    return (T)(object)EditorGUILayout.ColorField(label, color);
                case Bounds bounds:
                    return (T)(object)EditorGUILayout.BoundsField(label, bounds);
                case Rect rect:
                    return (T)(object)EditorGUILayout.RectField(label, rect);
                case UnityEngine.Object obj:
                    using (new GUILayout.HorizontalScope())
                    {
                        obj = EditorGUILayout.ObjectField(label, obj, typeof(T), true);
                        return GUILayout.Button("x",GUILayout.Width(22)) ? default(T) : (T)(object)obj;
                    }
                default:
                    if ((typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)) || typeof(T) == typeof(UnityEngine.Object)) && value == null)
                    {
                        return (T)(object)EditorGUILayout.ObjectField(label, null, typeof(T), true);
                    }
                    else
                        EditorGUILayout.LabelField(label, $"(Unsupported Type: {typeof(T).Name})");
                    return value;
            }
        }
    }
}
