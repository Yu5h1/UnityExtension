// Reusable IMGUI text display; callers own the log content and this instance owns its viewport.
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Displays selectable, read-only text with per-instance wheel zoom and a vertical scrollbar.
    /// Keep one instance per visible panel and draw it consistently on every OnGUI event.
    /// </summary>
    public sealed class ReadOnlyLogPanel
    {
        public const int DefaultFontSize = 12;
        public const int MinimumFontSize = 8;
        public const int MaximumFontSize = 32;

        private static readonly Func<Rect> getVisibleRect = CreateVisibleRectGetter();
        private readonly GUIContent content = new GUIContent();
        private GUIStyle textStyle;
        private Vector2 scrollPosition;

        public int FontSize { get; private set; } = DefaultFontSize;

        internal void ResetScroll()
        {
            scrollPosition = Vector2.zero;
        }

        /// <summary>
        /// Unity exposes ancestor clipping only through internal GUIClip. Bind once and leave
        /// font-control input untouched if unavailable; selection and scrollbar use public APIs.
        /// </summary>
        private static Func<Rect> CreateVisibleRectGetter()
        {
            Type clipType = typeof(GUI).Assembly.GetType("UnityEngine.GUIClip");
            MethodInfo getter = clipType?.GetProperty("visibleRect",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetGetMethod(true);
            if (getter == null || getter.ReturnType != typeof(Rect))
                return null;
            try
            {
                return (Func<Rect>)Delegate.CreateDelegate(typeof(Func<Rect>), getter);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (MemberAccessException)
            {
                return null;
            }
        }

        internal static bool ContainsVisiblePoint(Rect position, Vector2 point)
        {
            return position.Contains(point) && getVisibleRect != null && getVisibleRect().Contains(point);
        }

        /// <summary>
        /// Reserves a full-width layout area. Returns true when the font changes; repaint the host then.
        /// </summary>
        public bool DrawLayout(string text, float height)
        {
            Rect position = GUILayoutUtility.GetRect(0, Mathf.Max(1, height), GUILayout.ExpandWidth(true));
            return Draw(position, text);
        }

        /// <summary>
        /// Draws inside the supplied viewport. The wheel scrolls normally; Ctrl+wheel zooms and
        /// is consumed even at the font limits. Ctrl+middle-button restores the default font size.
        /// Outside input is left to the host.
        /// Returns true when the font changes; the host should repaint without dirtying its data.
        /// </summary>
        public bool Draw(Rect position, string text)
        {
            if (textStyle == null)
                textStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    richText = false,
                    wordWrap = true,
                    clipping = TextClipping.Clip
                };

            Event current = Event.current;
            bool fontInput = current.type == EventType.ScrollWheel ||
                (current.type == EventType.MouseDown && current.button == 2);
            bool handleFontInput = GUI.enabled && current.control && fontInput &&
                ContainsVisiblePoint(position, current.mousePosition);
            int previousFontSize = FontSize;
            if (handleFontInput)
            {
                if (current.type == EventType.MouseDown)
                {
                    FontSize = DefaultFontSize;
                    current.Use();
                }
                else if (current.delta.y != 0)
                    FontSize = Mathf.Clamp(FontSize + (current.delta.y < 0 ? 1 : -1),
                        MinimumFontSize, MaximumFontSize);
            }

            textStyle.fontSize = FontSize;
            content.text = text ?? string.Empty;
            GUIStyle scrollbar = GUI.skin.verticalScrollbar;
            float width = Mathf.Max(1, position.width - scrollbar.fixedWidth - scrollbar.margin.left);
            float height = Mathf.Max(position.height, Mathf.Ceil(textStyle.CalcHeight(content, width)));
            Rect textRect = new Rect(0, 0, width, height);
            scrollPosition.y = Mathf.Clamp(scrollPosition.y, 0, Mathf.Max(0, height - position.height));
            scrollPosition = GUI.BeginScrollView(position, scrollPosition, textRect,
                false, true, GUIStyle.none, scrollbar);
            try
            {
                using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
                    EditorGUI.SelectableLabel(textRect, content.text, textStyle);
            }
            finally
            {
                GUI.EndScrollView(!handleFontInput);
            }

            if (handleFontInput && current.type == EventType.ScrollWheel)
                current.Use();
            return previousFontSize != FontSize;
        }
    }
}
