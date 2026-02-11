using UnityEngine;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// 為 StringOptionsContextAttribute 繪製並註冊 Context
    /// 當 List/Array 元素是 ScriptableObject 時，註冊 Context 供其子欄位使用
    /// </summary>
    [CustomPropertyDrawer(typeof(StringOptionsContextAttribute))]
    public class StringOptionsContextDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var contextAttr = attribute as StringOptionsContextAttribute;
            if (contextAttr != null && !string.IsNullOrEmpty(contextAttr.ListKey))
            {
                RegisterContext(property, contextAttr.ListKey);
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /// <summary>
        /// 註冊 Context 給 List/Array 中的 ScriptableObject 元素
        /// </summary>
        private void RegisterContext(SerializedProperty property, string listKey)
        {
            // 處理 ObjectReference（ScriptableObject）
            if (property.propertyType == SerializedPropertyType.ObjectReference
                && property.objectReferenceValue != null
                && property.objectReferenceValue is ScriptableObject)
            {
                StringOptionsProvider.SetContext(
                    property.objectReferenceValue.GetInstanceID(),
                    listKey
                );
            }
        }
    }
}
