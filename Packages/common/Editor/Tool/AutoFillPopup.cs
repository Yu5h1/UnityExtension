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
        private Func<string, string> displayFormatter;
        private Vector2 scrollPosition;
        private int hoverIndex = -1;
        private string searchText = "";
        private bool needsFocus = true;
        private bool useKeyboardHover; // true 時鍵盤優先，滑鼠不搶 hoverIndex

        private const float ITEM_HEIGHT = 20f;
        private const float SEARCH_HEIGHT = 22f;
        private const float MAX_HEIGHT = 200f;

        // 快取 GUIStyle，避免每幀 new
        private static GUIStyle _itemStyle;
        private static GUIStyle _itemHoverStyle;

        public static AutoFillPopup Show(Rect activatorRect, string[] options, Action<string> onSelect,
            string filter = "", Func<string, string> displayFormatter = null,bool screenSpace = true)
        {
            // 關閉已存在的
            var existing = Resources.FindObjectsOfTypeAll<AutoFillPopup>();
            foreach (var w in existing)
                w.Close();

            if (options == null || options.Length == 0)
                return null;

            instance = CreateInstance<AutoFillPopup>();
            instance.allOptions = options;
            instance.onSelect = onSelect;
            instance.displayFormatter = displayFormatter;
            instance.searchText = filter ?? "";
            instance.ApplyFilter();

            // 計算視窗大小
            float width = Mathf.Max(activatorRect.width, 200);
            int displayCount = instance.filteredOptions.Count;
            float height = Mathf.Min(displayCount * ITEM_HEIGHT + SEARCH_HEIGHT + 8, MAX_HEIGHT);

            if (screenSpace)
                activatorRect.position = GUIUtility.GUIToScreenPoint(activatorRect.position);

            instance.ShowAsDropDown(activatorRect, new Vector2(width, height));
            return instance;
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
        }

        private static void EnsureStyles()
        {
            if (_itemStyle == null)
            {
                _itemStyle = new GUIStyle(EditorStyles.label)
                {
                    padding = new RectOffset(6, 6, 2, 2),
                    normal = { textColor = Color.gray * 1.5f }
                };
            }
            if (_itemHoverStyle == null)
            {
                _itemHoverStyle = new GUIStyle(EditorStyles.label)
                {
                    padding = new RectOffset(6, 6, 2, 2),
                    normal = { textColor = Color.white }
                };
            }
        }
        //private int _cursorFixFrames;
        //void FocusWithCursorAtEnd(string controlName)
        //{
        //    EditorGUI.FocusTextInControl(controlName);
        //    _cursorFixFrames = 100; // 連續處理 3 幀
        //}

        private void OnGUI()
        {
            //if (_cursorFixFrames > 0 && Event.current.type == EventType.Repaint)
            //{
            //    var te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            //    if (te != null)
            //    {
            //        te.SelectNone();
            //        te.MoveTextEnd();
            //        te.MoveLineEnd();
            //        te.selectIndex = searchText.Length + 1;
            //        //te. = searchText.Length + 1;
            //        "move cursor".print();
            //    }
            //    _cursorFixFrames--;
            //}

            if (allOptions == null || allOptions.Length == 0)
            {
                Close();
                return;
            }

            EnsureStyles();

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
                        useKeyboardHover = true;
                        ScrollToIndex(hoverIndex);
                        Event.current.Use();
                        Repaint();
                        return;

                    case KeyCode.UpArrow:
                        hoverIndex = Mathf.Max(hoverIndex - 1, 0);
                        useKeyboardHover = true;
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

            // 滑鼠移動時解除鍵盤優先
            if (Event.current.type == EventType.MouseMove)
                useKeyboardHover = false;

            // 搜尋欄
            var searchRect = new Rect(4, 4, position.width - 8, SEARCH_HEIGHT - 4);

            GUI.SetNextControlName("AutoFillSearch");
            var newSearch = EditorGUI.TextField(searchRect, searchText, EditorStyles.toolbarSearchField);

            if (needsFocus)
            {
                EditorGUI.FocusTextInControl("AutoFillSearch");
                //FocusWithCursorAtEnd("AutoFillSearch");
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

        private void DrawItem(int index, string value)
        {
            var rect = EditorGUILayout.GetControlRect(false, ITEM_HEIGHT);

            // 滑鼠 hover 只在非鍵盤模式時更新 hoverIndex
            bool mouseHover = rect.Contains(Event.current.mousePosition);
            if (mouseHover && !useKeyboardHover)
                hoverIndex = index;

            bool isSelected = index == hoverIndex;
            if (isSelected)
                EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.8f, 0.5f));

            // 使用 displayFormatter 顯示短名稱，tooltip 顯示完整值
            string displayText = displayFormatter != null ? displayFormatter(value) : value;
            string tooltip = displayFormatter != null && displayText != value ? value : "";
            var content = new GUIContent(displayText, tooltip);

            if (GUI.Button(rect, content, isSelected ? _itemHoverStyle : _itemStyle))
            {
                SelectItem(value);
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
