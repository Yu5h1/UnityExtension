using System.ComponentModel;
using UnityEngine;

namespace UnityEditor
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class EditorGUILayoutEx
    {
        public static bool ChangedCheckField(this Vector3 source,string label, out Vector3 output, params GUILayoutOption[] options)
          => (output = EditorGUILayout.Vector3Field(label, source, options)) != source;
        public static bool ChangedCheckField(this Vector2 source, string label, out Vector2 output, params GUILayoutOption[] options)
            => (output = EditorGUILayout.Vector2Field(label, source, options)) != source;
    }
}
