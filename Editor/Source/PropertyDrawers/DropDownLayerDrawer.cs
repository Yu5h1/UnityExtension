using UnityEditor;
using UnityEngine;



[CustomPropertyDrawer(typeof(DropDownLayerAttribute))]
public class DropDownLayerDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.Integer)
        {
            EditorGUI.BeginChangeCheck();

            int layerValue = EditorGUI.LayerField(position, label, property.intValue);

            if (EditorGUI.EndChangeCheck())
            {
                property.intValue = layerValue;
            }
        }
        else
        {
            EditorGUI.LabelField(position, label, "Use LayerSelector with int.");
        }
    }
}
