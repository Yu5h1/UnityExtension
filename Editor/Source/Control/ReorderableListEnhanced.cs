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
            add => drawElementCallback += value;
            remove => drawElementCallback -= value;
        }
        private System.Action<Rect> _foldoutCallback = default;
        public event System.Action<Rect> foldoutCallback
        {
            add => _foldoutCallback += value;
            remove => _foldoutCallback -= value;
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


        public ReorderableListEnhanced(SerializedObject serializedObject, SerializedProperty elements
            ,bool draggable,bool displayHeader) : 
            base(serializedObject, elements,draggable,displayHeader,true,true)
        {
            drawElementCallback = DrawElement;
            elementHeightCallback = GetElementHeight;
            //added += OnAdded;
            //removed += OnRemoved;
            //reordered += OnReordered;


        }

        private void OnReordered(ReorderableList list)
        {
            Undo.RecordObject(target, "Reorder Items"); 
            serializedObject.ApplyModifiedProperties();
        }

        private void OnAdded(ReorderableList list)
        {
            Undo.RecordObject(target, "Add Item");
            serializedProperty.arraySize++;
            serializedObject.ApplyModifiedProperties();
        }

        private void OnRemoved(ReorderableList list)
        {
            if (list.index < 0)
                return;
            Undo.RecordObject(target, "Remove Item");
            serializedProperty.DeleteArrayElementAtIndex(list.index);
            serializedObject.ApplyModifiedProperties();
        }
        protected virtual void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = serializedProperty.GetArrayElementAtIndex(index);
            if (element.propertyType == SerializedPropertyType.Generic)
            {
                rect.x += 8;
                rect.width -= 8;
            }
            EditorGUI.PropertyField(rect, element, true);
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
        public new void DoLayoutList()
        {

            Rect foldoutRect = EditorGUILayout.GetControlRect();

            
            serializedProperty.isExpanded = EditorGUI.Foldout(new Rect(foldoutRect.x, foldoutRect.y, 15, foldoutRect.height), serializedProperty.isExpanded , GUIContent.none);
            _foldoutCallback?.Invoke(foldoutRect);

            EditorGUI.PropertyField(new Rect(foldoutRect.x, foldoutRect.y, foldoutRect.width - 15, foldoutRect.height),
                serializedProperty, false);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && foldoutRect.Contains(Event.current.mousePosition))
            {
                serializedProperty.isExpanded = !serializedProperty.isExpanded;
                Event.current.Use(); 
            }
            if (serializedProperty.isExpanded)
                base.DoLayoutList();
        }

    }
}
