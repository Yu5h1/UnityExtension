// Inspector-sized log section with an external foldout title, toolbar and draggable bottom edge.
using System;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Wraps ReadOnlyLogPanel with reusable section controls. Callers supply the title, text and
    /// toolbar actions; each instance owns its expanded state, height and text viewport.
    /// </summary>
    public sealed class ReadOnlyLogSection
    {
        public const float MinimumHeight = 60;
        public const float MaximumHeight = 800;
        private const int HorizontalPadding = 4;
        private const float ResizeHandleHeight = 7;
        private static readonly int resizeHint = "ReadOnlyLogSection.Resize".GetHashCode();

        private readonly ReadOnlyLogPanel panel = new ReadOnlyLogPanel();
        private GUIStyle sectionLayoutStyle;
        private GUIStyle bodyLayoutStyle;
        private SearchField searchField;
        private float dragStartY;
        private float dragStartHeight;

        public bool Expanded { get; private set; } = true;
        public float Height { get; private set; }
        public int FontSize => panel.FontSize;
        public string SearchText { get; private set; } = string.Empty;

        private static Color BackgroundColor => EditorGUIUtility.isProSkin
            ? new Color(0.235f, 0.235f, 0.235f) : new Color(0.76f, 0.76f, 0.76f);
        private static Color BorderColor => EditorGUIUtility.isProSkin
            ? new Color(0.13f, 0.13f, 0.13f) : new Color(0.45f, 0.45f, 0.45f);

        /// <summary>Sets the initial message viewport height in GUI points, excluding the toolbar.</summary>
        public ReadOnlyLogSection(float height = 160)
        {
            Height = Mathf.Clamp(height, MinimumHeight, MaximumHeight);
        }

        /// <summary>
        /// Draws a foldout above the frame and a fixed toolbar above the scrolling messages.
        /// Uses compact horizontal padding and applies the caller's indent level to the whole section.
        /// Draw toolbar actions with DrawToolbarButton, starting with the caller's Clear
        /// action. Text is read after those actions so their changes appear immediately.
        /// Returns true when display state changes; repaint the host then.
        /// </summary>
        public bool DrawLayout(string title, Func<string> getText, Action drawToolbar)
        {
            if (getText == null)
                throw new ArgumentNullException(nameof(getText));

            EnsureStyles();
            sectionLayoutStyle.padding.left = HorizontalPadding +
                Mathf.RoundToInt(EditorGUI.IndentedRect(new Rect()).x);
            using (new EditorGUILayout.VerticalScope(sectionLayoutStyle))
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
                return DrawSection(title, getText, drawToolbar);
        }

        private bool DrawSection(string title, Func<string> getText, Action drawToolbar)
        {
            bool previousChanged = GUI.changed;
            bool expanded = EditorGUILayout.Foldout(Expanded, title, true);
            GUI.changed = previousChanged;
            bool changed = expanded != Expanded;
            Expanded = expanded;
            if (!Expanded)
                return changed;

            using (new EditorGUILayout.VerticalScope(GUIStyle.none))
            {
                using (var toolbar = new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    drawToolbar?.Invoke();
                    GUILayout.Space(6);
                    Rect searchRect = GUILayoutUtility.GetRect(0, EditorStyles.toolbar.fixedHeight,
                        GUILayout.ExpandWidth(true));
                    searchRect.xMin = Mathf.Max(searchRect.xMin, searchRect.xMax - 220);
                    searchRect.y = toolbar.rect.yMax - EditorGUIUtility.singleLineHeight ;
                    searchRect.height = EditorGUIUtility.singleLineHeight;
                    bool changedBeforeSearch = GUI.changed;
                    string query = searchField.OnToolbarGUI(searchRect, SearchText);
                    GUI.changed = changedBeforeSearch;
                    if (query != SearchText)
                    {
                        SearchText = query;
                        panel.ResetScroll();
                        changed = true;
                    }
                    if (Event.current.type == EventType.Repaint)
                        EditorGUI.DrawRect(new Rect(toolbar.rect.x, toolbar.rect.y,
                            toolbar.rect.width, 1), BorderColor);
                }

                using (var body = new EditorGUILayout.VerticalScope(bodyLayoutStyle))
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        EditorGUI.DrawRect(body.rect, BackgroundColor);
                        DrawBodyFrame(body.rect);
                    }
                    changed |= panel.DrawLayout(FilterText(getText(), SearchText), Height);
                    Rect handle = GUILayoutUtility.GetRect(0, ResizeHandleHeight, GUILayout.ExpandWidth(true));
                    changed |= DrawResizeHandle(handle);
                }
            }
            return changed;
        }

        /// <summary>
        /// Draws an action using the native flat toolbar-button style.
        /// Call inside drawToolbar; the return value retains normal button click semantics.
        /// </summary>
        public bool DrawToolbarButton(string text)
        {
            return GUILayout.Button(text, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
        }

        private void EnsureStyles()
        {
            if (sectionLayoutStyle != null)
                return;
            sectionLayoutStyle = new GUIStyle
            {
                padding = new RectOffset(HorizontalPadding, HorizontalPadding, 0, 0)
            };
            bodyLayoutStyle = new GUIStyle { padding = new RectOffset(1, 1, 0, 1) };
            searchField = new SearchField();
        }

        /// <summary>
        /// Filters complete lines by a literal, case-insensitive substring. Original line endings
        /// are retained, and an empty query returns the unmodified source text.
        /// </summary>
        internal static string FilterText(string text, string query)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
                return text ?? string.Empty;

            var result = new StringBuilder();
            int start = 0;
            while (start < text.Length)
            {
                int end = start;
                while (end < text.Length && text[end] != '\r' && text[end] != '\n')
                    end++;
                int next = end;
                if (next < text.Length && text[next] == '\r')
                    next++;
                if (next < text.Length && text[next] == '\n')
                    next++;
                if (text.IndexOf(query, start, end - start, StringComparison.OrdinalIgnoreCase) >= 0)
                    result.Append(text, start, next - start);
                start = next;
            }
            return result.ToString();
        }

        private static void DrawBodyFrame(Rect position)
        {
            EditorGUI.DrawRect(new Rect(position.x, position.y, 1, position.height), BorderColor);
            EditorGUI.DrawRect(new Rect(position.xMax - 1, position.y, 1, position.height), BorderColor);
            EditorGUI.DrawRect(new Rect(position.x, position.yMax - 1, position.width, 1), BorderColor);
        }

        /// <summary>
        /// Captures left-button dragging in screen coordinates so ancestor scrolling cannot
        /// distort the height delta. Mouse release ends capture outside the grip.
        /// </summary>
        private bool DrawResizeHandle(Rect position)
        {
            int id = GUIUtility.GetControlID(resizeHint, FocusType.Passive);
            Event current = Event.current;
            if (GUI.enabled)
                EditorGUIUtility.AddCursorRect(position, MouseCursor.ResizeVertical, id);
            if (current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(position.center.x - 12, position.center.y, 24, 1), Color.gray);

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (GUI.enabled && current.button == 0 && GUIUtility.hotControl == 0 &&
                        ReadOnlyLogPanel.ContainsVisiblePoint(position, current.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        dragStartY = GUIUtility.GUIToScreenPoint(current.mousePosition).y;
                        dragStartHeight = Height;
                        current.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        Height = Mathf.Clamp(dragStartHeight +
                            GUIUtility.GUIToScreenPoint(current.mousePosition).y - dragStartY,
                            MinimumHeight, MaximumHeight);
                        current.Use();
                        return true;
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && current.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }
                    break;
            }
            return false;
        }
    }
}
