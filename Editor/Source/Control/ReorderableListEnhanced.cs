//using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    
    public class ReorderableListEnhanced : ReorderableList
    {

        public SerializedObject serializedObject => serializedProperty.serializedObject;
        public Object target => serializedObject.targetObject;
        public event ReorderableList.AddCallbackDelegate added
        {
            add => onAddCallback += value;
            remove => onAddCallback -= value;
        }
        public event ReorderableList.RemoveCallbackDelegate removed
        {
            add => onRemoveCallback += value;
            remove => onRemoveCallback -= value;
        }
        public event ReorderableList.ReorderCallbackDelegate reordered
        {
            add => onReorderCallback += value;
            remove => onReorderCallback -= value;
        }
        public event ReorderableList.ElementCallbackDelegate drawElement
        {
            add => drawElementCallback = value;
            remove => drawElementCallback -= value;
        }
        private System.Action<Rect> _headerDrawed = default;
        public event System.Action<Rect> HeaderDrawed
        {
            add => _headerDrawed += value;
            remove => _headerDrawed -= value;
        }
        private System.Type _elementType = null;
        public System.Type elementType
        {
            get
            {
                if (_elementType == null)
                    serializedProperty.TryGetElementType(out _elementType);
                return _elementType;
            }
        }

        public static Dictionary<int, (string arrayPath, System.Func<string> provider)> FilterTextProvider = new Dictionary<int, (string arrayPath, System.Func<string> provider)>();

        /// <summary>
        /// External filter text provider set by InlineDrawContext.
        /// When set, this list uses the external filter instead of showing its own filter field.
        /// </summary>
        public System.Func<string> filterProvider;


        #region Filter
        private static readonly Dictionary<string, string> _filterCache = new Dictionary<string, string>();
        private string FilterCacheKey => $"{target.GetInstanceID()}_{serializedProperty.propertyPath}";

        private string _filterText = "";
        public string GetFilterText() => _filterText;
        public string FilterText
        {
            get => GetFilterText();
            set
            {
                if (_filterText == value)
                    return;
                _filterText = value;
                _filterCache[FilterCacheKey] = value;
                UpdateFilteredIndices();
            }
        }
        private List<int> _filteredIndices = new List<int>();
        private bool _originalDraggable;
        private int _lastKnownArraySize = -1;

        /// <summary>
        /// Delegate to get the display label for an element, used for filtering.
        /// Parameters: (SerializedProperty element, int index)
        /// </summary>
        public System.Func<SerializedProperty, int, string> elementLabelGetter { get; set; }

        /// <summary>
        /// Whether the list is currently being filtered.
        /// </summary>
        public bool isFiltering => allowFilter && !string.IsNullOrEmpty(_filterText);

        /// <summary>
        /// Number of elements matching the current filter.
        /// </summary>
        public int filteredCount => _filteredIndices.Count;
        public bool showFilterField = true;
        public bool allowFilter = true;
        #endregion

        public ReorderableListEnhanced(SerializedObject serializedObject, SerializedProperty elements
            ,bool draggable,bool displayHeader) :
            base(serializedObject, elements,draggable,displayHeader,true,true)
        {            
            multiSelect = true;
            _originalDraggable = draggable;
            drawElementCallback = DrawElement;
            elementHeightCallback = GetElementHeight;

            if (_filterCache.TryGetValue(FilterCacheKey, out var cached))
                _filterText = cached;
            UpdateFilteredIndices();
        }

        public void MarkAsFilterProvider()
        {
            FilterTextProvider[target.GetInstanceID()] = (serializedProperty.propertyPath, GetFilterText);
        }
        public void UnmarkAsFilterProvider()
        {
            FilterTextProvider.Remove(target.GetInstanceID());
        }

        protected virtual float GetElementHeight(int index)
            => EditorGUI.GetPropertyHeight(serializedProperty.GetArrayElementAtIndex(index), true);

        public static bool TryCreate<T>(SerializedObject serializedObject,string propertyName,
            out T reorderableList, bool draggable = true, bool displayHeader = false) where T : ReorderableList
        {
            reorderableList = null;
            var arrayProp = serializedObject.FindProperty(propertyName);
            if (arrayProp == null || !arrayProp.isArray)
                return false;
            reorderableList = new ReorderableListEnhanced(serializedObject, arrayProp, draggable,displayHeader) as T;

            return true;
        }

        private Rect _fixedheaderRect;
        public Rect fixedHeaderRect => _fixedheaderRect;

        /// <summary>
        /// Calculates the total height needed for this enhanced list.
        /// </summary>
        public float GetTotalHeight()
        {
            float height = EditorGUIUtility.singleLineHeight; // foldout header

            if (serializedProperty.isExpanded)
            {
                height += EditorGUIUtility.standardVerticalSpacing;

                if (isFiltering)
                {
                    if (_filteredIndices.Count == 0)
                        height += EditorGUIUtility.singleLineHeight * 2;
                    else
                        foreach (var index in _filteredIndices)
                            height += GetElementHeight(index) + EditorGUIUtility.standardVerticalSpacing;
                }
                else
                {
                    height += base.GetHeight();
                }
            }

            return height;
        }
        private bool _showNativeDropProxy;

        private void UpdateNativeDropProxyState(Rect headRect, Event e)
        {
            bool overHeader = headRect.Contains(e.mousePosition);

            switch (e.type)
            {
                case EventType.DragUpdated:
                    _showNativeDropProxy = overHeader;
                    break;

                case EventType.DragPerform:
                    // 保持 _showNativeDropProxy = true，
                    // 讓 GroupScope 內的 PropertyField(…, true) 處理 drop。
                    // Unity 會在 DragPerform 後自動發送 DragExited 來清除狀態。
                    break;

                case EventType.DragExited:
                    _showNativeDropProxy = false;
                    break;

                case EventType.MouseDown:
                case EventType.MouseDrag:
                case EventType.MouseUp:
                case EventType.KeyDown:
                    if (!overHeader)
                        _showNativeDropProxy = false;
                    break;
            }
        }

        /// <summary>
        /// Rect-based drawing. Draws the full enhanced list (header + body) within the given rect.
        /// </summary>
        public new void DoList(Rect rect)
        {
            var e = Event.current;

   
            // Header line
            var headRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            _fixedheaderRect = headRect;

            UpdateNativeDropProxyState(headRect, e);

            if (_showNativeDropProxy)
            {
                float fullPropHeight = EditorGUI.GetPropertyHeight(serializedProperty, true);

                // 只給 header 可視區
                using (new GUI.GroupScope(headRect))
                {
                    // Group 內是 local 座標，左上角是 (0,0)
                    var fullRect = new Rect(0f, 0f, headRect.width, fullPropHeight);

                    EditorGUI.PropertyField(fullRect, serializedProperty, true);
                }
            }
            else
            {
                // 1. Foldout
                var foldoutTriangleRect = new Rect(headRect.x, headRect.y, 15, headRect.height);

                serializedProperty.isExpanded = EditorGUI.Foldout(foldoutTriangleRect, serializedProperty.isExpanded, GUIContent.none);

                // Remain right click menu functionality by drawing default header label (without foldout) on top of foldout triangle
                EditorGUI.PropertyField(headRect, serializedProperty, false);

                // 3. Size field (between label and filter)
                var sizeWidth = 48f;
                var sizeRect = new Rect(_fixedheaderRect.xMax - sizeWidth, headRect.y + 1, sizeWidth, headRect.height);

                EditorGUI.BeginChangeCheck();
                int newSize = EditorGUI.DelayedIntField(sizeRect, serializedProperty.arraySize);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedProperty.arraySize = Mathf.Max(0, newSize);
                    serializedProperty.serializedObject.ApplyModifiedProperties();
                    UpdateFilteredIndices();
                }
            }

            Rect filterRect = default;
            // Filter search field (right side)
            if (filterProvider != null)
            {
                FilterText = filterProvider();
                if (!FilterText.IsEmpty() && !serializedProperty.isExpanded)
                    serializedProperty.isExpanded = true;
            }
            else if (showFilterField)
            {
                var filterWidth = Mathf.Min(150f, headRect.width * 0.35f);
                // - 48 - 1 is 'Count' width
                filterRect = new Rect(_fixedheaderRect.xMax - filterWidth - 1 - 48 -1 , headRect.y + 2, filterWidth, headRect.height);

                int controlId = GUIUtility.GetControlID(FocusType.Keyboard, filterRect);
                EditorGUI.BeginChangeCheck();
                string newText = EditorGUI.TextField(filterRect, _filterText, EditorStyles.toolbarSearchField);
                if (EditorGUI.EndChangeCheck())
                    FilterText = newText;

                _fixedheaderRect.xMax -= filterWidth + 1;
            }
            _headerDrawed?.Invoke(_fixedheaderRect);

            if (e.type == EventType.MouseDown && e.button == 0 && _fixedheaderRect.Contains(Event.current.mousePosition))
            {
                serializedProperty.isExpanded = !serializedProperty.isExpanded;
                Event.current.Use();
            }

            // Array size change detection
            if (serializedProperty.arraySize != _lastKnownArraySize)
                UpdateFilteredIndices();

            // 7. Draw body when expanded
            if (serializedProperty.isExpanded)
            {
                var bodyY = headRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                var bodyRect = new Rect(rect.x, bodyY, rect.width, rect.yMax - bodyY);

                if (isFiltering)
                {
                    draggable = false;
                    DrawFilteredList(bodyRect);
                    draggable = _originalDraggable;
                }
                else
                {
                    base.DoList(bodyRect);
                }
            }
        }

        /// <summary>
        /// Layout-based drawing. Reserves space via layout system, then delegates to DoList(Rect).
        /// </summary>
        public new void DoLayoutList()
        {
            var rect = EditorGUILayout.GetControlRect(false, GetTotalHeight());
            DoList(rect);
        }


        //MethodInfo _GetContentRectMethod;
        //public Rect GetContentRect(Rect rect)
        //{
        //    if (_GetContentRectMethod == null)
        //        _GetContentRectMethod = typeof(ReorderableList).GetMethod("GetContentRect", BindingFlags.NonPublic | BindingFlags.Instance);

        //    return (Rect)_GetContentRectMethod.Invoke(this, new object[] { rect });
        //}
        public static Rect GetContentRect(Rect rect, bool draggable )
        {
            Rect result = rect;
            if (draggable)
            {
                result.xMin += 20f;
            }
            else
            {
                result.xMin += 6f;
            }

            result.xMax -= 6f;
            return result;
        }
        #region Filter methods
        private void UpdateFilteredIndices()
        {
            _lastKnownArraySize = serializedProperty.arraySize;

            if (!allowFilter)
                return;
            _filteredIndices.Clear();

            if (string.IsNullOrEmpty(_filterText))
            {
                for (int i = 0; i < serializedProperty.arraySize; i++)
                    _filteredIndices.Add(i);
                return;
            }

            for (int i = 0; i < serializedProperty.arraySize; i++)
            {
                var element = serializedProperty.GetArrayElementAtIndex(i);
                var label = GetElementLabel(element, i);

                if (label.MatchesTokens(_filterText))
                    _filteredIndices.Add(i);
            }
        }

        private string GetElementLabel(SerializedProperty element, int index)
        {
            if (elementLabelGetter != null)
                return elementLabelGetter(element, index) ?? "";

            if (element.propertyType == SerializedPropertyType.ObjectReference &&
                element.objectReferenceValue != null)
                return element.objectReferenceValue.name;

            return $"Element {index}";
        }
        public Rect GetContentRect(Rect rect) => GetContentRect(rect, draggable);
        private void DrawFilteredList(Rect rect)
        {
            EditorGUI.indentLevel++;

            if (_filteredIndices.Count == 0)
            {
                EditorGUI.HelpBox(rect, "No matching items", MessageType.Info);
            }
            else
            {
                var r = rect;
                foreach (var realIndex in _filteredIndices)
                {
                    r.height = GetElementHeight(realIndex);
                    DrawElement(r, realIndex, false, false);
                    r.y += r.height + EditorGUIUtility.standardVerticalSpacing;
                }
            }
            EditorGUI.indentLevel--;
        }
        protected virtual void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(GetContentRect(rect), element, true);
        }
        #endregion
    }
}
