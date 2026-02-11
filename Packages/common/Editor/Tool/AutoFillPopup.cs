using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// AutoFill 的浮動建議選單
    /// 不影響 Inspector layout，類似 IDE 的 autocomplete
    /// 支援 whitespace tokenization 搜尋（AND 條件，case-insensitive partial match）
    /// 例如 "sys coll gener" 會匹配 "System.Collections.Generic"
    /// </summary>
    public class AutoFillPopup : EditorWindow
    {
        public static AutoFillPopup instance { get; private set; }
        private string[] allOptions;
        private List<string> filteredOptions = new List<string>();
        private Action<string> onSelect;
        private Vector2 scrollPosition;
        private int hoverIndex = -1;
        private string searchText = "";
        private bool needsFocus = true;

        private const float ITEM_HEIGHT = 20f;
        private const float SEARCH_HEIGHT = 22f;
        private const float MAX_HEIGHT = 200f;

        public static void Show(Rect activatorRect, string[] options, Action<string> onSelect, string filter = "")
        {
            // 關閉已存在的
            var existing = Resources.FindObjectsOfTypeAll<AutoFillPopup>();
            foreach (var w in existing)
                w.Close();

            if (options == null || options.Length == 0)
                return;

            instance = CreateInstance<AutoFillPopup>();
            instance.allOptions = options;
            instance.onSelect = onSelect;
            instance.searchText = filter ?? "";
            instance.ApplyFilter();

            // 計算視窗大小
            float width = Mathf.Max(activatorRect.width, 200);
            int displayCount = instance.filteredOptions.Count;
            float height = Mathf.Min(displayCount * ITEM_HEIGHT + SEARCH_HEIGHT + 8, MAX_HEIGHT);

            var screenPos = GUIUtility.GUIToScreenPoint(new Vector2(activatorRect.x, activatorRect.y));
            var screenRect = new Rect(screenPos.x, screenPos.y, activatorRect.width, activatorRect.height);
            instance.ShowAsDropDown(screenRect, new Vector2(width, height));
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            if (allOptions == null || allOptions.Length == 0)
            {
                Close();
                return;
            }

            // 背景
            var bgRect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(bgRect, new Color(0.22f, 0.22f, 0.22f, 1f));
            DrawBorder(bgRect, new Color(0.1f, 0.1f, 0.1f, 1f));

            // 在 TextField 之前攔截鍵盤事件，避免被搜尋欄吃掉
            if (Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.DownArrow:
                        if (filteredOptions.Count > 0)
                            hoverIndex = Mathf.Min(hoverIndex + 1, filteredOptions.Count - 1);
                        ScrollToIndex(hoverIndex);
                        Event.current.Use();
                        Repaint();
                        return;

                    case KeyCode.UpArrow:
                        hoverIndex = Mathf.Max(hoverIndex - 1, 0);
                        ScrollToIndex(hoverIndex);
                        Event.current.Use();
                        Repaint();
                        return;

                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        if (hoverIndex >= 0 && hoverIndex < filteredOptions.Count)
                        {
                            SelectItem(filteredOptions[hoverIndex]);
                        }
                        Event.current.Use();
                        return;

                    case KeyCode.Escape:
                        Close();
                        Event.current.Use();
                        return;
                }
            }

            // 搜尋欄
            var searchRect = new Rect(4, 4, position.width - 8, SEARCH_HEIGHT - 4);

            GUI.SetNextControlName("AutoFillSearch");
            var newSearch = EditorGUI.TextField(searchRect, searchText, EditorStyles.toolbarSearchField);

            if (needsFocus)
            {
                EditorGUI.FocusTextInControl("AutoFillSearch");
                needsFocus = false;
            }

            if (newSearch != searchText)
            {
                searchText = newSearch;
                ApplyFilter();
                hoverIndex = filteredOptions.Count > 0 ? 0 : -1;
                scrollPosition = Vector2.zero;
                ResizeWindow();
                Repaint();
            }

            // 選項列表
            var listRect = new Rect(0, SEARCH_HEIGHT + 2, position.width, position.height - SEARCH_HEIGHT - 2);
            GUILayout.BeginArea(listRect);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < filteredOptions.Count; i++)
            {
                DrawItem(i, filteredOptions[i]);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            if (Event.current.type == EventType.MouseMove)
            {
                Repaint();
            }
        }

        /// <summary>
        /// Whitespace tokenization + AND 條件篩選
        /// 每個 token 做 case-insensitive partial match
        /// </summary>
        private void ApplyFilter()
        {
            filteredOptions.Clear();

            if (string.IsNullOrEmpty(searchText) || string.IsNullOrWhiteSpace(searchText))
            {
                filteredOptions.AddRange(allOptions);
                return;
            }

            var tokens = searchText.ToLower().Split(
                new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
            {
                filteredOptions.AddRange(allOptions);
                return;
            }

            foreach (var option in allOptions)
            {
                var lowerOption = option.ToLower();
                bool allMatch = true;

                foreach (var token in tokens)
                {
                    if (!lowerOption.Contains(token))
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                    filteredOptions.Add(option);
            }
        }

        private void ResizeWindow()
        {
            int displayCount = Mathf.Max(filteredOptions.Count, 1);
            float height = Mathf.Min(displayCount * ITEM_HEIGHT + SEARCH_HEIGHT + 8, MAX_HEIGHT);
            var pos = position;
            pos.height = height;
            minSize = new Vector2(pos.width, height);
            maxSize = new Vector2(pos.width, height);
        }

        private void ScrollToIndex(int index)
        {
            float targetY = index * ITEM_HEIGHT;
            float viewHeight = position.height - SEARCH_HEIGHT - 6;

            if (targetY < scrollPosition.y)
                scrollPosition.y = targetY;
            else if (targetY + ITEM_HEIGHT > scrollPosition.y + viewHeight)
                scrollPosition.y = targetY + ITEM_HEIGHT - viewHeight;
        }

        private void DrawItem(int index, string text)
        {
            var rect = EditorGUILayout.GetControlRect(false, ITEM_HEIGHT);

            bool isHover = rect.Contains(Event.current.mousePosition) || index == hoverIndex;
            if (isHover)
            {
                hoverIndex = index;
                EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.8f, 0.5f));
            }

            var style = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(6, 6, 2, 2),
                normal = { textColor = isHover ? Color.white : Color.gray * 1.5f }
            };

            if (GUI.Button(rect, text, style))
            {
                SelectItem(text);
            }
        }

        private void SelectItem(string value)
        {
            onSelect?.Invoke(value);
            Close();
        }

        private void DrawBorder(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), color);
        }
        private void OnDisable()
        {
            instance = null;
        }
        private void OnLostFocus()
        {
            Close();
        }
    }
}
