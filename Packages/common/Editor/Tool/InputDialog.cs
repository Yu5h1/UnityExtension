using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Unified input dialog that replaces both RenamePopup and AutoFillPopup.
    /// - No options: simple text input (rename mode)
    /// - With options: text input + filterable dropdown list (autofill mode)
    /// Supports whitespace tokenization search (AND condition, case-insensitive partial match).
    /// </summary>
    public class InputDialog : PopupWindowContent
    {
        public static InputDialog instance { get; private set; }
        public static event Action<object> confirmed;

        private string[] allOptions;
        private List<string> filteredOptions = new List<string>();
        private Action<string> onApply;
        private Func<string, string> displayFormatter;
        private object target;
        private Vector2 scrollPosition;
        private int hoverIndex = -1;
        private string searchText = "";
        private string _original = "";
        private bool needsFocus = true;
        private bool useKeyboardHover;
        private bool _cancelled;
        private bool applyOnLostFocus;

        private bool HasOptions => allOptions != null && allOptions.Length > 0;

        private const float ITEM_HEIGHT = 20f;
        private const float MAX_HEIGHT = 200f;
        private static bool hasOptions;
        private static float HEIGHT => EditorGUIUtility.singleLineHeight + 2;

        // Cached GUIStyles
        private static GUIStyle _itemStyle;
        private static GUIStyle _itemHoverStyle;
        public static float width = 200f;
        public override Vector2 GetWindowSize()
        {
            return new Vector2(width, HEIGHT);
        }
        #region EditorWindow Members

        public bool wantsMouseMove { get => editorWindow.wantsMouseMove; set => editorWindow.wantsMouseMove = value; }
        public Rect position { get => editorWindow.position; set => editorWindow.position = value; }
        public Vector2 minSize { get => editorWindow.minSize; set => editorWindow.minSize = value; }   
        public Vector2 maxSize { get => editorWindow.maxSize; set => editorWindow.maxSize = value; }

        public void Close() => editorWindow.Close();
        public void Repaint() => editorWindow.Repaint();
        #endregion

        /// <summary>
        /// Show the InputDialog.
        /// </summary>
        /// <param name="activatorRect">Position rect (screen or GUI space)</param>
        /// <param name="onApply">Callback when value is confirmed</param>
        /// <param name="initialText">Initial text in the input field</param>
        /// <param name="options">Optional dropdown options (null = input-only mode)</param>
        /// <param name="displayFormatter">Optional formatter for display text</param>
        /// <param name="target">Optional target for confirmed event</param>
        /// <param name="applyOnLostFocus">If true, apply value when focus is lost (rename behavior)</param>
        /// <param name="ToScreenSpace">If true, activatorRect is in screen space</param>
        public static InputDialog Show(
            Rect activatorRect,
            Action<string> onApply,
            string initialText = "",
            string[] options = null,
            Func<string, string> displayFormatter = null,
            object target = null,
            bool applyOnLostFocus = false,
            bool ToScreenSpace = true,
            bool SetWindowPosition = true)
        {            
            // Close existing instance
            //var existing = Resources.FindObjectsOfTypeAll<InputDialog>();
            //foreach (var w in existing)
            //    w.Close();

            if (onApply == null)
                return null;

            //instance = CreateInstance<InputDialog>();

            instance = new InputDialog();
            instance.onApply = onApply;
            instance.displayFormatter = displayFormatter;
            instance.target = target;
            instance.applyOnLostFocus = applyOnLostFocus;
            instance.searchText = initialText ?? "";
            instance._original = instance.searchText;
            instance._cancelled = false;

            hasOptions = options != null && options.Length > 0;
            instance.allOptions = options;

            if (hasOptions)
            {
                instance.ApplyFilter();
                instance.hoverIndex = -1;
            }

            // Calculate window size
            //width = Mathf.Max(activatorRect.width, 200);
            width = activatorRect.width;

            if (ToScreenSpace)
                activatorRect.position = GUIUtility.GUIToScreenPoint(
                    new Vector2(activatorRect.x, activatorRect.y));

            instance.Show(activatorRect, SetWindowPosition);
          
            return instance;
        }
        private void OnEnable()
        {
            wantsMouseMove = true;
        }
        bool _initialized = false;
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
        public override void OnGUI(Rect bgRect)
        {
            var e = Event.current;
            EnsureStyles();
            if (!_initialized && e.type == EventType.Repaint)
            {
                _initialized = true;
                hoverIndex = filteredOptions.Count > 0 ? 0 : -1;
                scrollPosition = Vector2.zero;
                ResizeWindow();
            }
            // Background
            //var bgRect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(bgRect, new Color(0.22f, 0.22f, 0.22f, 1f));
            DrawBorder(bgRect, new Color(0.1f, 0.1f, 0.1f, 1f));

            // Intercept keyboard events before TextField
            if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    case KeyCode.DownArrow:
                        if (HasOptions && filteredOptions.Count > 0)
                        {
                            scrollPosition = Vector2.zero;
                            ResizeWindow();
                            hoverIndex = Mathf.Min(hoverIndex + 1, filteredOptions.Count - 1);
                            useKeyboardHover = true;
                            ScrollToIndex(hoverIndex);
                            e.Use();
                            Repaint();
                        }
                        return;

                    case KeyCode.UpArrow:
                        if (HasOptions)
                        {
                            hoverIndex = Mathf.Max(hoverIndex - 1, -1);
                            useKeyboardHover = true;
                            if (hoverIndex >= 0)
                                ScrollToIndex(hoverIndex);
                            e.Use();
                            Repaint();
                        }
                        return;

                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        ApplyAndClose();
                        e.Use();
                        return;

                    case KeyCode.Escape:
                        _cancelled = true;
                        Close();
                        e.Use();
                        return;
                    case KeyCode.Delete:
                        hoverIndex = -1;
                        break;
                }
            }

            // Release keyboard hover on mouse move
            if (e.type == EventType.MouseMove)
                useKeyboardHover = false;

            // Input field
            var inputRect = new Rect(hasOptions ? 2 : 0, hasOptions ? 2 : 0, position.width - (hasOptions? 2 : 0) , HEIGHT);

            GUI.SetNextControlName("InputDialogField");
            var newText = HasOptions
                ? EditorGUI.TextField(inputRect, searchText, EditorStyles.toolbarSearchField)
                : EditorGUI.TextField(inputRect, searchText);
            if (needsFocus)
            {
                EditorGUI.FocusTextInControl("InputDialogField");
                // Select all text for rename convenience
                var editor = (TextEditor)GUIUtility.GetStateObject(
                    typeof(TextEditor), GUIUtility.keyboardControl);
                editor?.SelectAll();
                needsFocus = false;
            }

            if (newText != searchText )
            {
                searchText = newText;
                ApplyFilter();
                hoverIndex = filteredOptions.Count > 0 && !searchText.IsEmpty() ? 0 : -1;
                scrollPosition = Vector2.zero;
                ResizeWindow();
                Repaint();
            }
            // Option list (only when options exist)
            if (HasOptions)
            {
                var listRect = new Rect(0, HEIGHT + 2,
                    position.width, position.height - HEIGHT - 2);
                GUILayout.BeginArea(listRect);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                for (int i = 0; i < filteredOptions.Count; i++)
                {
                    DrawItem(i, filteredOptions[i]);
                }

                EditorGUILayout.EndScrollView();
                GUILayout.EndArea();
            }

            if (e.type == EventType.MouseMove)
                Repaint();
        }
        /// <summary>
        /// Whitespace tokenization + AND condition filter.
        /// Each token does case-insensitive partial match.
        /// </summary>
        private void ApplyFilter()
        {
            filteredOptions.Clear();

            if (allOptions.IsEmpty()) return;

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
            float height;
            if (HasOptions)
            {
                int displayCount = Mathf.Max(filteredOptions.Count, 1);
                height = Mathf.Min(displayCount * ITEM_HEIGHT + HEIGHT + 8, MAX_HEIGHT);
            }
            else
            {
                height = HEIGHT;
            }

            var pos = position;
            pos.height = height;
            minSize = new Vector2(pos.width, height);
            maxSize = new Vector2(pos.width, height);
        }

        private void ScrollToIndex(int index)
        {
            float targetY = index * ITEM_HEIGHT;
            float viewHeight = position.height - HEIGHT - 6;

            if (targetY < scrollPosition.y)
                scrollPosition.y = targetY;
            else if (targetY + ITEM_HEIGHT > scrollPosition.y + viewHeight)
                scrollPosition.y = targetY + ITEM_HEIGHT - viewHeight;
        }

        private void DrawItem(int index, string value)
        {
            var rect = EditorGUILayout.GetControlRect(false, ITEM_HEIGHT);

            // Mouse hover updates hoverIndex only when not in keyboard mode
            bool mouseHover = rect.Contains(Event.current.mousePosition);
            if (mouseHover && !useKeyboardHover)
                hoverIndex = index;

            bool isSelected = index == hoverIndex;
            if (isSelected)
                EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.8f, 0.5f));

            // Display formatter for short names, tooltip for full value
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
            searchText = value;
            ApplyAndClose();
        }

        private void ApplyAndClose()
        {
            Apply();
            _cancelled = true; // Prevent double-apply from OnDisable
            Close();
        }
        public override void OnClose()
        {
            base.OnClose();           
            if (!_cancelled && applyOnLostFocus)
                Apply();
            instance = null;
        }
        private void Apply()
        {
            string result;
            if (HasOptions && filteredOptions.IsValid(hoverIndex))
                result = filteredOptions[hoverIndex];
            else
                result = searchText.Trim();

            if (result == _original)
                return;

            _original = result; // Prevent double-apply
            onApply?.Invoke(result);
            confirmed?.Invoke(target);
        }

        private void DrawBorder(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), color);
        }

   
    }
}
