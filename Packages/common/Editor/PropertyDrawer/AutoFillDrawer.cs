using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// 為帶有 AutoFillAttribute 的 string 欄位提供自動完成建議
    /// 按 ↓ 鍵顯示 AutoFillPopup，搜尋篩選由 Popup 內部處理
    /// </summary>
    [CustomPropertyDrawer(typeof(AutoFillAttribute))]
    public class AutoFillDrawer : PropertyDrawer
    {
        AutoFillAttribute autoFillatt => (AutoFillAttribute)attribute;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            var _previousText = property.stringValue;
            bool textChanged = false;
            // PropertyField（保留原生右鍵選單、Prefab Override 等功能）
            // 記住 PropertyField 內部產生的 controlID，用來判斷鍵盤焦點
            var idBefore = GUIUtility.GetControlID(FocusType.Passive);
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, property, label);
            if (EditorGUI.EndChangeCheck())
            {
                textChanged = true;
            }
            var idAfter = GUIUtility.GetControlID(FocusType.Passive);

            // 計算彈出位置
            var popupRect = position;
            popupRect.y -= EditorGUIUtility.singleLineHeight;
            popupRect.x += EditorGUIUtility.labelWidth;
            popupRect.width -= EditorGUIUtility.labelWidth;

            // 檢查鍵盤焦點是否在這個 PropertyField 上
            // Unity 的 TextField controlID 會介於 idBefore 和 idAfter 之間
            var focused = GUIUtility.keyboardControl;
            bool hasFocus = focused > idBefore && focused < idAfter;

            // 按 ↓ 鍵顯示全部選項（僅在此欄位有焦點時）
            if (textChanged)
            {
                ShowPopup(popupRect, property);
            }
            else if (hasFocus )
            {
                if (Event.current.type == EventType.KeyDown)
                {
                    if (Event.current.keyCode == KeyCode.DownArrow)
                    {
                        ShowPopup(popupRect, property);
                        Event.current.Use();
                    }
                }
            }
        }

        private void ShowPopup(Rect popupRect, SerializedProperty property)
        {
            var listKey = StringOptionsHelper.ResolveListKey(property, autoFillatt.ListKey);

            if (string.IsNullOrEmpty(listKey))
                return;

            var items = StringOptionsProvider.GetOptions(
                property.serializedObject.targetObject, listKey, property.propertyPath);

            if (items == null || items.Length == 0)
                return;

            var formatter = StringOptionsProvider.GetDisplayFormatter(listKey);
            AutoFillPopup.Show(popupRect, items, selected =>
            {
                property.stringValue = selected;
                property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }, property.stringValue, formatter);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
