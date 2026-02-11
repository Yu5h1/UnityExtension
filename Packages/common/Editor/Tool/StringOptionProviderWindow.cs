using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// StringOptions 管理視窗
    /// 可視覺化管理所有註冊的選項清單
    /// 支援持久化儲存、txt 檔案匯入
    /// </summary>
    public class StringOptionProviderWindow : EditorWindow
    {
        private Vector2 listScrollPosition;
        private Vector2 editScrollPosition;
        private string newListKey = "";
        private string newItemsText = "";
        private string searchFilter = "";
        private Object txtFileField = null;

        private const string PREFS_KEY = "StringOptionsProvider_CustomLists";

        [MenuItem("Tools/String Options Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<StringOptionProviderWindow>("String Options");
            window.minSize = new Vector2(500, 400);
        }

        private void OnEnable()
        {
            LoadCustomLists();
        }

        /// <summary>
        /// 載入已儲存的自訂清單
        /// </summary>
        [InitializeOnLoadMethod]
        private static void LoadCustomLists()
        {
            string json = EditorPrefs.GetString(PREFS_KEY, "");
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var data = JsonUtility.FromJson<CustomListsData>(json);
                if (data?.lists == null) return;

                foreach (var list in data.lists)
                {
                    if (list.key.StartsWith('~'))
                        continue;
                    if (!string.IsNullOrEmpty(list.key) && list.items != null)
                    {
                        StringOptionsProvider.Register(list.key, list.items);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StringOptions] 載入自訂清單失敗: {e.Message}");
            }
        }

        private void SaveCustomLists(string key, string[] items)
        {
            string json = EditorPrefs.GetString(PREFS_KEY, "");
            CustomListsData data;

            if (string.IsNullOrEmpty(json))
            {
                data = new CustomListsData { lists = new List<CustomListEntry>() };
            }
            else
            {
                data = JsonUtility.FromJson<CustomListsData>(json);
                if (data == null) data = new CustomListsData { lists = new List<CustomListEntry>() };
                if (data.lists == null) data.lists = new List<CustomListEntry>();
            }

            data.lists.RemoveAll(l => l.key == key);
            data.lists.Add(new CustomListEntry { key = key, items = items });

            EditorPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(data));
        }

        private void DeleteSavedList(string key)
        {
            string json = EditorPrefs.GetString(PREFS_KEY, "");
            if (string.IsNullOrEmpty(json)) return;

            var data = JsonUtility.FromJson<CustomListsData>(json);
            if (data?.lists == null) return;

            data.lists.RemoveAll(l => l.key == key);
            EditorPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(data));
        }

        private bool IsCustomList(string key)
        {
            string json = EditorPrefs.GetString(PREFS_KEY, "");
            if (string.IsNullOrEmpty(json)) return false;

            var data = JsonUtility.FromJson<CustomListsData>(json);
            return data?.lists?.Any(l => l.key == key) ?? false;
        }

        private void OnGUI()
        {
            // 搜尋列
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜尋:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("清除", GUILayout.Width(50)))
                searchFilter = "";
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 已註冊清單
            DrawRegisteredLists();

            EditorGUILayout.Space(10);
            DrawSeparator();

            // 新增/編輯區域
            DrawEditArea();

            DrawSeparator();
            GUILayout.FlexibleSpace();

            // TXT 匯入
            DrawTxtImport();

            EditorGUILayout.Space(10);

            // 底部按鈕
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重新整理"))
                Repaint();
            if (GUILayout.Button("清除所有自訂清單"))
            {
                if (EditorUtility.DisplayDialog("確認", "確定要清除所有自訂清單嗎？（內建清單不受影響）", "確定", "取消"))
                {
                    EditorPrefs.DeleteKey(PREFS_KEY);
                    LoadCustomLists();
                    Repaint();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRegisteredLists()
        {
            EditorGUILayout.LabelField("已註冊的清單", EditorStyles.boldLabel);

            listScrollPosition = EditorGUILayout.BeginScrollView(listScrollPosition, GUILayout.Height(200));

            var allKeys = StringOptionsProvider.GetAllKeys().Where(k => !k.StartsWith('~')).ToArray();

            if (allKeys.Length == 0)
            {
                EditorGUILayout.HelpBox("尚未註冊任何清單", MessageType.Info);
            }
            else
            {
                var filteredKeys = string.IsNullOrEmpty(searchFilter)
                    ? allKeys
                    : allKeys.Where(k => k.ToLower().Contains(searchFilter.ToLower())).ToArray();

                foreach (var key in filteredKeys)
                {
                    DrawListItem(key);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawListItem(string key)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(key, EditorStyles.boldLabel, GUILayout.Width(150));

            var items = StringOptionsProvider.GetOptions(null,key,"");
            EditorGUILayout.LabelField($"({items.Length} 項)", GUILayout.Width(60));

            GUILayout.FlexibleSpace();

            bool isCustom = IsCustomList(key);
            if (!isCustom)
                EditorGUILayout.LabelField("(內建)", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(50));

            if (GUILayout.Button("📋", GUILayout.Width(28)))
            {
                EditorGUIUtility.systemCopyBuffer = key;
                Debug.Log($"已複製: {key}");
            }

            if (GUILayout.Button(isCustom ? "✏️" : "👁", GUILayout.Width(28)))
            {
                newListKey = key;
                newItemsText = string.Join("\n", items);
                GUI.FocusControl(null);
            }

            EditorGUI.BeginDisabledGroup(!isCustom);
            if (GUILayout.Button("🗑", GUILayout.Width(28)))
            {
                if (EditorUtility.DisplayDialog("確認刪除", $"確定要刪除清單 '{key}' 嗎？", "刪除", "取消"))
                {
                    StringOptionsProvider.Unregister(key);
                    DeleteSavedList(key);
                    Repaint();
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // 預覽
            if (items.Length > 0)
            {
                EditorGUI.indentLevel++;
                var preview = string.Join(", ", items.Take(3));
                if (items.Length > 3) preview += "...";
                EditorGUILayout.LabelField(preview, EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawEditArea()
        {
            EditorGUILayout.LabelField("編輯清單", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("清單名稱:", GUILayout.Width(80));
            newListKey = EditorGUILayout.TextField(newListKey);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("項目（每行一個）:");

            bool isEditable = string.IsNullOrEmpty(newListKey) || IsCustomList(newListKey) || !StringOptionsProvider.Contains(newListKey);
            EditorGUI.BeginDisabledGroup(!isEditable);
            editScrollPosition = EditorGUILayout.BeginScrollView(editScrollPosition, GUILayout.ExpandHeight(true));
            newItemsText = EditorGUILayout.TextArea(newItemsText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            bool canSave = isEditable && !string.IsNullOrEmpty(newListKey) && !string.IsNullOrEmpty(newItemsText);
            EditorGUI.BeginDisabledGroup(!canSave);
            if (GUILayout.Button("儲存", GUILayout.Height(25)))
            {
                SaveList();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void SaveList()
        {
            var items = newItemsText
                .Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();

            if (items.Length == 0)
            {
                EditorUtility.DisplayDialog("錯誤", "請至少輸入一個項目", "確定");
                return;
            }

            if (StringOptionsProvider.Contains(newListKey) && !IsCustomList(newListKey))
            {
                EditorUtility.DisplayDialog("錯誤", "無法覆寫內建清單", "確定");
                return;
            }

            if (StringOptionsProvider.Contains(newListKey))
            {
                if (!EditorUtility.DisplayDialog("覆蓋確認", $"清單 '{newListKey}' 已存在，要覆蓋嗎？", "覆蓋", "取消"))
                    return;
            }

            StringOptionsProvider.Register(newListKey, items);
            SaveCustomLists(newListKey, items);

            EditorUtility.DisplayDialog("成功", $"清單 '{newListKey}' 已儲存\n共 {items.Length} 個項目", "確定");
            Repaint();
        }

        private void DrawTxtImport()
        {
            EditorGUILayout.LabelField("從 TXT 匯入", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("拖曳 .txt 檔案，檔名會作為清單名稱", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("TXT 檔案:", GUILayout.Width(80));

            var newTxtFile = EditorGUILayout.ObjectField(txtFileField, typeof(TextAsset), false) as TextAsset;
            if (newTxtFile != txtFileField)
            {
                txtFileField = newTxtFile;
                if (newTxtFile != null)
                    ImportFromTextAsset(newTxtFile);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("選擇檔案", GUILayout.Height(25)))
            {
                string path = EditorUtility.OpenFilePanel("選擇 TXT 檔案", Application.dataPath, "txt");
                if (!string.IsNullOrEmpty(path))
                    ImportFromPath(path);
            }

            EditorGUILayout.EndVertical();
        }

        private void ImportFromTextAsset(TextAsset asset)
        {
            if (asset == null) return;

            var items = ParseItems(asset.text);
            if (items.Length == 0)
            {
                EditorUtility.DisplayDialog("錯誤", "檔案內容為空", "確定");
                txtFileField = null;
                return;
            }

            var key = asset.name;
            if (StringOptionsProvider.Contains(key) && !IsCustomList(key))
            {
                EditorUtility.DisplayDialog("錯誤", $"'{key}' 是內建清單，無法覆寫", "確定");
                txtFileField = null;
                return;
            }

            if (StringOptionsProvider.Contains(key))
            {
                if (!EditorUtility.DisplayDialog("覆蓋確認", $"清單 '{key}' 已存在，要覆蓋嗎？", "覆蓋", "取消"))
                {
                    txtFileField = null;
                    return;
                }
            }

            StringOptionsProvider.Register(key, items);
            SaveCustomLists(key, items);

            txtFileField = null;
            EditorUtility.DisplayDialog("成功", $"已匯入 '{key}'\n共 {items.Length} 個項目", "確定");
            Repaint();
        }

        private void ImportFromPath(string path)
        {
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("錯誤", "檔案不存在", "確定");
                return;
            }

            try
            {
                var content = File.ReadAllText(path);
                var items = ParseItems(content);
                var key = Path.GetFileNameWithoutExtension(path);

                if (items.Length == 0)
                {
                    EditorUtility.DisplayDialog("錯誤", "檔案內容為空", "確定");
                    return;
                }

                if (StringOptionsProvider.Contains(key) && !IsCustomList(key))
                {
                    EditorUtility.DisplayDialog("錯誤", $"'{key}' 是內建清單，無法覆寫", "確定");
                    return;
                }

                if (StringOptionsProvider.Contains(key))
                {
                    if (!EditorUtility.DisplayDialog("覆蓋確認", $"清單 '{key}' 已存在，要覆蓋嗎？", "覆蓋", "取消"))
                        return;
                }

                StringOptionsProvider.Register(key, items);
                SaveCustomLists(key, items);

                EditorUtility.DisplayDialog("成功", $"已匯入 '{key}'\n共 {items.Length} 個項目", "確定");
                Repaint();
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("錯誤", $"讀取失敗:\n{e.Message}", "確定");
            }
        }

        private string[] ParseItems(string content)
        {
            return content
                .Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }

        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUILayout.Space(5);
        }

        [System.Serializable]
        private class CustomListsData
        {
            public List<CustomListEntry> lists;
        }

        [System.Serializable]
        private class CustomListEntry
        {
            public string key;
            public string[] items;
        }       
    }
}
