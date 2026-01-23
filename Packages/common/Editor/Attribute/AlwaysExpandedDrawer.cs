using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AlwaysExpandedAttribute))]
public sealed class AlwaysExpandedDrawer : PropertyDrawer
{
    const float VSpace = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var attr = (AlwaysExpandedAttribute)attribute;

        float height = 0f;

        // 外層 label 行
        if (attr.ShowLabel)
            height += EditorGUIUtility.singleLineHeight + attr.LabelSpacing;

        if (!property.hasVisibleChildren)
        {
            height += EditorGUI.GetPropertyHeight(property, includeChildren: true);
            return height;
        }

        // 子欄位總高度
        height += GetChildrenHeight(property);

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (AlwaysExpandedAttribute)attribute;

        EditorGUI.BeginProperty(position, label, property);

        float y = position.y;
        float width = position.width;

        // 外層 label
        if (attr.ShowLabel)
        {
            var labelRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label);
            y += EditorGUIUtility.singleLineHeight + attr.LabelSpacing;
        }

        if (!property.hasVisibleChildren)
        {
            var r = new Rect(position.x, y, width, EditorGUI.GetPropertyHeight(property, true));
            EditorGUI.PropertyField(r, property, GUIContent.none, includeChildren: true);
            EditorGUI.EndProperty();
            return;
        }

        // 畫子欄位（不畫 foldout）
        foreach (var child in IterateChildren(property))
        {
            float h = EditorGUI.GetPropertyHeight(child, includeChildren: true);
            var r = new Rect(position.x, y, width, h);
            EditorGUI.PropertyField(r, child, includeChildren: true);
            y += h + VSpace;
        }

        EditorGUI.EndProperty();
    }

    static float GetChildrenHeight(SerializedProperty parent)
    {
        float total = 0f;

        foreach (var child in IterateChildren(parent))
        {
            total += EditorGUI.GetPropertyHeight(child, includeChildren: true) + VSpace;
        }

        if (total > 0f)
            total -= VSpace; // 移除最後一個間距

        return total;
    }

    static System.Collections.Generic.IEnumerable<SerializedProperty> IterateChildren(SerializedProperty parent)
    {
        var copy = parent.Copy();
        var end = copy.GetEndProperty();

        // 進入第一個子層
        bool enterChildren = true;
        if (!copy.NextVisible(enterChildren))
            yield break;

        while (!SerializedProperty.EqualContents(copy, end))
        {
            yield return copy.Copy();
            if (!copy.NextVisible(false))
                break;
        }
    }
}
