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

            if (EditorGUI.EndChangeCheck() && layerValue¡@!= property.intValue)
                property.intValue = layerValue;
        }
        else if (property.propertyType == SerializedPropertyType.String)
        {
            EditorGUI.BeginChangeCheck();

            var currentLayer = LayerMask.NameToLayer(property.stringValue);
            int newValue = EditorGUI.LayerField(position, label, currentLayer);
            if (currentLayer == -1) currentLayer = 0;
            if (EditorGUI.EndChangeCheck() && newValue != currentLayer)
                property.stringValue = LayerMask.LayerToName(newValue);
        }
        else 
            EditorGUI.LabelField(position, label, "Use LayerSelector with int.");
    }
}
