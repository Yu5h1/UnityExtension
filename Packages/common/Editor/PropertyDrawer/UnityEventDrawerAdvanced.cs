//using UnityEngine;
//using UnityEngine.Events;
//using UnityEditor;
//using UnityEditorInternal;
//using System.Reflection;

//[CustomPropertyDrawer(typeof(UnityEventBase), true)]
//public class UnityEventDrawerAdvanced : UnityEventDrawer
//{
//    // Cache for reflection access
//    private static FieldInfo s_ReorderableListField;

//    static UnityEventDrawerAdvanced()
//    {
//        // Get the internal ReorderableList field from base UnityEventDrawer
//        s_ReorderableListField = typeof(UnityEventDrawer).GetField("m_ReorderableList",
//            BindingFlags.NonPublic | BindingFlags.Instance);
//    }

//    protected ReorderableList GetReorderableList()
//    {
//        // Access the internal ReorderableList from base class
//        return s_ReorderableListField?.GetValue(this) as ReorderableList;
//    }

//    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//    {
//        Rect eventRect = new Rect(position.x, position.y + 20, position.width, position.height - 20);
//        base.OnGUI(eventRect, property, GUIContent.none);

//        // Now you can access the ReorderableList if needed for additional functionality
//        ReorderableList reorderableList = GetReorderableList();
//        if (reorderableList != null)
//        {
//            // TODO: Add your custom functionality here
//            // For example: modify callbacks, add custom buttons, etc.
//        }
//    }

//    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//    {
//        // Add extra height for our custom header on top of base height
//        return base.GetPropertyHeight(property, label) + 20;
//    }
//}