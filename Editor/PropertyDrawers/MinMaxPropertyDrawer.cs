using UnityEngine;
using UnityEditor;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.EditorExtension
{
    //[CustomPropertyDrawer(typeof(MinMax))]
    //public class MinMaxPropertyDrawer : PropertyDrawer
    //{
    //    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    //    {
    //        return EditorGUIUtility.singleLineHeight+20;//EditorGUI.GetPropertyHeight(property, label, true);
    //    }
    //    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //    {
    //        var startProp = property.FindPropertyRelative("Min");
    //        var endProp = property.FindPropertyRelative("Max");
    //        float start = startProp.floatValue,end = endProp.floatValue;
    //        var labelPos = position;
    //        GUIStyle style = new GUIStyle();
    //        GUIContent content = new GUIContent(property.displayName);
    //        labelPos.x = (labelPos.width- style.CalcSize(content).x)*0.5f;
    //        EditorGUI.LabelField(labelPos, property.displayName+" : ");
    //        EditorGUI.LabelField(position, start.ToString("0.00"));
    //        labelPos = position;
    //        content.text = end.ToString("0.00");
    //        labelPos.x = position.width - style.CalcSize(content).x-15;
    //        EditorGUI.LabelField(labelPos, content);
    //        position.y += 18;
    //        EditorGUI.MinMaxSlider(position,ref start, ref end, 0, 1);

    //        startProp.floatValue = start;
    //        endProp.floatValue = end;
    //        //EditorGUI.PropertyField(position, property, label, true);
    //        //Rect r = position;
    //        //r.y += 30;
    //        //EditorGUI.LabelField(r, property.name);
    //    }
    //} 
}