using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Draws <see cref="ValuePort"/>'s stored string as a field of its <see cref="ValuePort.kind"/> (Vector3 field,
    /// color picker, ...), converting through the shared codec. In Play Mode an edit goes through
    /// <c>SetValue</c> so the bound DataView and <c>changed</c> hear it. Below the field, it names the source
    /// members this port is bound to on hosts in the loaded scenes, flagging a kind that does not match.
    /// </summary>
    [CustomEditor(typeof(ValuePort), true)]
    public class ValuePortEditor : Editor<ValuePort>
    {
        private List<(string host, string key, System.Type memberType)> boundMembers;

        private void OnEnable()
        {
            EditorApplication.hierarchyChanged += MarkDirty;
            Undo.undoRedoPerformed += MarkDirty;
        }

        protected override void OnDisable()
        {
            EditorApplication.hierarchyChanged -= MarkDirty;
            Undo.undoRedoPerformed -= MarkDirty;
            base.OnDisable();
        }

        private void MarkDirty() => boundMembers = null;

        public override void DrawProperty(SerializedProperty property)
        {
            if (property.name != "_value" || targets.Length > 1)
            {
                base.DrawProperty(property);
                return;
            }
            var kind = (ValuePort.ValueKind)serializedObject.FindProperty("_kind").enumValueIndex;
            var type = ValuePort.TypeOf(kind);
            var text = property.stringValue;
            bool parsed = PlayerPrefsSerializer.Default.TryDeserialize(text ?? string.Empty, type, out var value);
            if (!parsed)
                value = type == typeof(string) ? string.Empty : System.Activator.CreateInstance(type);

            EditorGUI.BeginChangeCheck();
            var edited = DrawField(new GUIContent(property.displayName, $"Stored as \"{text}\""), type, value);
            if (EditorGUI.EndChangeCheck())
            {
                var newText = PlayerPrefsSerializer.Default.Serialize(edited, type);
                if (Application.isPlaying)
                    targetObject.SetValue(newText);
                else
                    property.stringValue = newText;
            }
            if (!parsed && !string.IsNullOrEmpty(text))
                EditorGUILayout.HelpBox($"Stored value \"{text}\" is not a {type.Name}; editing replaces it.", MessageType.Warning);

            DrawBoundMembers(type);
        }

        private static object DrawField(GUIContent label, System.Type type, object value)
        {
            if (type == typeof(bool)) return EditorGUILayout.Toggle(label, (bool)value);
            if (type == typeof(int)) return EditorGUILayout.IntField(label, (int)value);
            if (type == typeof(float)) return EditorGUILayout.FloatField(label, (float)value);
            if (type == typeof(double)) return EditorGUILayout.DoubleField(label, (double)value);
            if (type == typeof(Vector2)) return EditorGUILayout.Vector2Field(label, (Vector2)value);
            if (type == typeof(Vector3)) return EditorGUILayout.Vector3Field(label, (Vector3)value);
            if (type == typeof(Vector4)) return EditorGUILayout.Vector4Field(label, (Vector4)value);
            if (type == typeof(Vector2Int)) return EditorGUILayout.Vector2IntField(label, (Vector2Int)value);
            if (type == typeof(Vector3Int)) return EditorGUILayout.Vector3IntField(label, (Vector3Int)value);
            if (type == typeof(Quaternion))
            {
                var q = (Quaternion)value;
                var v = EditorGUILayout.Vector4Field(label, new Vector4(q.x, q.y, q.z, q.w));
                return new Quaternion(v.x, v.y, v.z, v.w);
            }
            if (type == typeof(Color)) return EditorGUILayout.ColorField(label, (Color)value);
            if (type == typeof(Rect)) return EditorGUILayout.RectField(label, (Rect)value);
            return EditorGUILayout.TextField(label, (string)value);
        }

        private void DrawBoundMembers(System.Type kindType)
        {
            if (boundMembers == null)
                boundMembers = CollectBoundMembers();
            foreach (var (host, key, memberType) in boundMembers)
            {
                if (Preferences.IsCompatible(kindType, memberType))
                    EditorGUILayout.LabelField(" ", $"{host}: source member '{key}' ({memberType.Name})", EditorStyles.miniLabel);
                else
                    EditorGUILayout.HelpBox($"{host}: {Preferences.DescribeMismatch(key, kindType, memberType)}.", MessageType.Error);
            }
        }

        private List<(string, string, System.Type)> CollectBoundMembers()
        {
            var result = new List<(string, string, System.Type)>();
            var key = targetObject.GetFieldName();
            foreach (var behaviour in ObjectUtility.FindObjects<MonoBehaviour>())
            {
                if (!(behaviour is IPreferences host) || host.SourceType == null)
                    continue;
                var bindings = behaviour.GetType().GetProperty("bindings")?.GetValue(behaviour) as IReadOnlyList<Object>;
                if (bindings == null || !Contains(bindings, targetObject))
                    continue;
                if (host.TryGetSourceMemberType(key, out var memberType))
                    result.Add((behaviour.name, key, memberType));
            }
            return result;
        }

        private static bool Contains(IReadOnlyList<Object> list, Object item)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == item)
                    return true;
            return false;
        }
    }
}
