using UnityEngine;
using UnityEditor;
using System.IO;

namespace MotionGen
{
    [CustomPropertyDrawer(typeof(FilePathAttribute))]
    public class FilePathPropertyDrawer : PropertyDrawer
    {
        private const float ObjectFieldWidth = 150f;
        private const float Spacing = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var attr = (FilePathAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);

            // 計算區域
            float labelWidth = EditorGUIUtility.labelWidth;
            float objectFieldWidth = Mathf.Min(ObjectFieldWidth, (position.width - labelWidth) * 0.4f);
            float textFieldWidth = position.width - labelWidth - objectFieldWidth - Spacing;

            Rect labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            Rect textFieldRect = new Rect(position.x + labelWidth, position.y, textFieldWidth, position.height);
            Rect objectFieldRect = new Rect(textFieldRect.xMax + Spacing, position.y, objectFieldWidth, position.height);

            // 繪製 Label
            EditorGUI.LabelField(labelRect, label);

            // 取得當前路徑
            string currentPath = property.stringValue;

            // 繪製 TextField
            EditorGUI.BeginChangeCheck();
            string newPath = EditorGUI.TextField(textFieldRect, currentPath);
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = newPath;
            }

            // 從路徑取得對應的 Asset
            Object currentAsset = GetAssetFromPath(currentPath, attr.AssetType);

            // 繪製 ObjectField
            EditorGUI.BeginChangeCheck();
            Object selectedAsset = EditorGUI.ObjectField(objectFieldRect, currentAsset, attr.AssetType, false);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedAsset != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(selectedAsset);
                    property.stringValue = ConvertPath(assetPath, attr);
                }
                else
                {
                    // 清空時不改變路徑，除非使用者明確清空
                    // property.stringValue = string.Empty;
                }
            }

            EditorGUI.EndProperty();

            // 顯示路徑狀態提示
            DrawPathStatus(position, currentPath);
        }

        private Object GetAssetFromPath(string path, System.Type assetType)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // 嘗試直接作為 Asset 路徑載入
            if (path.StartsWith("Assets/") || path.StartsWith("Assets\\"))
            {
                return AssetDatabase.LoadAssetAtPath(path, assetType);
            }

            // 嘗試從絕對路徑轉換
            string dataPath = Application.dataPath;
            if (path.StartsWith(dataPath))
            {
                string relativePath = "Assets" + path.Substring(dataPath.Length);
                relativePath = relativePath.Replace('\\', '/');
                return AssetDatabase.LoadAssetAtPath(relativePath, assetType);
            }

            // 嘗試 StreamingAssets 路徑
            string streamingPath = Application.streamingAssetsPath;
            if (path.StartsWith(streamingPath))
            {
                string relativePath = "Assets/StreamingAssets" + path.Substring(streamingPath.Length);
                relativePath = relativePath.Replace('\\', '/');
                return AssetDatabase.LoadAssetAtPath(relativePath, assetType);
            }

            // 嘗試作為 StreamingAssets 相對路徑
            string fullStreamingPath = Path.Combine("Assets/StreamingAssets", path).Replace('\\', '/');
            var asset = AssetDatabase.LoadAssetAtPath(fullStreamingPath, assetType);
            if (asset != null)
                return asset;

            return null;
        }

        private string ConvertPath(string assetPath, FilePathAttribute attr)
        {
            if (string.IsNullOrEmpty(assetPath))
                return string.Empty;

            // 相對路徑模式：直接使用 Asset 路徑
            if (attr.UseRelativePath)
            {
                return assetPath;
            }

            // StreamingAssets 特殊處理
            if (attr.AllowStreamingAssets && assetPath.StartsWith("Assets/StreamingAssets/"))
            {
                // 回傳完整的 StreamingAssets 路徑
                string relativePart = assetPath.Substring("Assets/StreamingAssets/".Length);
                return Path.Combine(Application.streamingAssetsPath, relativePart);
            }

            // 轉換為絕對路徑
            string dataPath = Application.dataPath;
            string fullPath = Path.Combine(dataPath, assetPath.Substring("Assets/".Length));
            return fullPath.Replace('\\', '/');
        }

        private void DrawPathStatus(Rect position, string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            // 在下一行顯示狀態（如果需要的話）
            // 這裡簡化處理，只在路徑無效時透過顏色提示
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
