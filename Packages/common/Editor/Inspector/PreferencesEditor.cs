using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Lists every field in a Preferences save and whether anything still uses it: a binding in the
    /// loaded scenes (no outline), only <c>defaultSetting</c> (yellow: probably bound in a scene that is
    /// not loaded), or neither (red: deletable). <c>Preferences&lt;T&gt;</c> is generic, so each concrete
    /// host declares its own one-line editor deriving from this.
    /// </summary>
    public abstract class PreferencesEditor<T> : Editor<T> where T : Preferences<T>
    {
        private enum Usage { Consumed, DefaultOnly, Unused }

        private static readonly Color DefaultOnlyColor = new Color(1f, 0.76f, 0.03f);
        private static readonly Color UnusedColor = new Color(0.9f, 0.25f, 0.2f);

        private struct Row
        {
            public string Key;
            public string Value;
            public Usage Usage;
            public string Tooltip;
        }

        private List<Row> rows;
        private bool hasSave;
        private bool dirty = true;

        private void OnEnable()
        {
            EditorApplication.hierarchyChanged += MarkDirty;
            EditorApplication.projectChanged += MarkDirty;
            Undo.undoRedoPerformed += MarkDirty;
        }

        protected override void OnDisable()
        {
            EditorApplication.hierarchyChanged -= MarkDirty;
            EditorApplication.projectChanged -= MarkDirty;
            Undo.undoRedoPerformed -= MarkDirty;
            base.OnDisable();
        }

        private void MarkDirty() => dirty = true;

        /// <summary>Writes <paramref name="data"/> back as the whole save. Mirrors the default
        /// <c>SaveToPlayerPrefs</c>; override it for a host that stores its save elsewhere.</summary>
        protected virtual void WriteSave(DataView data)
        {
            PlayerPrefs.SetString(targetObject.KEY, data.ToJson());
            PlayerPrefs.Save();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
                dirty = true;
            if (targets.Length > 1)
                return;

            if (dirty)
                Refresh();
            var shown = rows.Where(r => r.Usage != Usage.Consumed).ToList();
            var unused = shown.Where(r => r.Usage == Usage.Unused).Select(r => r.Key).ToList();

            EditorGUILayout.Space();
            bool expanded;
            bool clear = false;
            using (new EditorGUILayout.HorizontalScope())
            {
                expanded = EditorGUILayout.Foldout(Expanded, new GUIContent($"Unclaimed List ({shown.Count})",
                    $"Fields saved under '{targetObject.KEY}' that no binding in the loaded scenes claims."), true);
                if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(60)))
                    dirty = true;
                using (new EditorGUI.DisabledScope(unused.Count == 0 || EditorApplication.isPlaying))
                    clear = GUILayout.Button(new GUIContent("Clear", "Delete every red field."), EditorStyles.miniButton, GUILayout.Width(50));
            }
            Expanded = expanded;

            string deleteKey = null;
            if (expanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    if (!hasSave)
                        EditorGUILayout.LabelField($"Nothing is saved under '{targetObject.KEY}' yet.", EditorStyles.miniLabel);
                    else if (shown.Count == 0)
                        EditorGUILayout.LabelField("Every saved field is claimed by a binding.", EditorStyles.miniLabel);
                    foreach (var row in shown)
                        if (DrawRow(row))
                            deleteKey = row.Key;
                    if (shown.Count > 0)
                        EditorGUILayout.HelpBox("Only the scenes currently loaded were checked. Yellow: listed in defaultSetting, likely bound in another scene. Red: in neither, deletable.", MessageType.None);
                    if (EditorApplication.isPlaying && shown.Count > 0)
                        EditorGUILayout.HelpBox("Deleting is disabled in Play Mode: the running host would save its in-memory copy back.", MessageType.None);
                }
            }

            if (clear)
                Delete(unused);
            else if (deleteKey != null)
                Delete(new List<string> { deleteKey });
        }

        private string ExpandedStateKey => $"{GetType().FullName}.Expanded";
        private bool Expanded
        {
            get => SessionState.GetBool(ExpandedStateKey, true);
            set => SessionState.SetBool(ExpandedStateKey, value);
        }

        private void Delete(List<string> keys)
        {
            if (!EditorUtility.DisplayDialog("Delete saved fields",
                    $"Remove from '{targetObject.KEY}':\n\n{string.Join("\n", keys)}\n\nThis cannot be undone.", "Delete", "Cancel"))
                return;
            if (!targetObject.TryLoadCurrent(out var saved))
            {
                $"Cannot read the save under '{targetObject.KEY}'.".printWarning();
                return;
            }
            foreach (var key in keys)
                saved.Remove(key);
            WriteSave(saved);
            $"Removed {keys.Count} field(s) from '{targetObject.KEY}': {string.Join(", ", keys)}".print();
            dirty = true;
            GUIUtility.ExitGUI();
        }

        private void Refresh()
        {
            dirty = false;
            rows = new List<Row>();
            hasSave = targetObject.TryLoadCurrent(out var saved) && saved != null;
            if (!hasSave)
                return;

            var consumers = PreferencesConsumerRegistry.Collect(targetObject.KEY)
                .GroupBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(c => c.Source).ToList(), StringComparer.OrdinalIgnoreCase);
            foreach (var pair in saved)
            {
                var row = new Row { Key = pair.Key, Value = pair.Value };
                if (consumers.TryGetValue(pair.Key, out var sources))
                {
                    row.Usage = Usage.Consumed;
                    row.Tooltip = "Used by:\n" + string.Join("\n", sources.Distinct());
                }
                else if (targetObject.TryGetDefault(pair.Key, out _))
                {
                    row.Usage = Usage.DefaultOnly;
                    row.Tooltip = "No binding in the loaded scenes; listed in defaultSetting.";
                }
                else
                {
                    row.Usage = Usage.Unused;
                    row.Tooltip = "No binding in the loaded scenes and not in defaultSetting.";
                }
                rows.Add(row);
            }
        }

        private static GUIStyle lampStyle;

        /// <summary>Draws a lamp, the key aligned to the inspector's label column, the value, and an x
        /// button. Returns true when x is clicked.</summary>
        private static bool DrawRow(Row row)
        {
            const float lampWidth = 14, buttonWidth = 20;
            if (lampStyle == null)
                lampStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };

            var rect = EditorGUILayout.GetControlRect();
            var indented = EditorGUI.IndentedRect(rect);
            var lamp = new Rect(indented.x, rect.y, lampWidth, rect.height);
            var button = new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height);
            var keyWidth = Mathf.Max(0, rect.x + EditorGUIUtility.labelWidth - lamp.xMax);
            var key = new Rect(lamp.xMax, rect.y, keyWidth, rect.height);
            var value = new Rect(key.xMax, rect.y, Mathf.Max(0, button.x - 2 - key.xMax), rect.height);

            var color = GUI.contentColor;
            GUI.contentColor = row.Usage == Usage.Unused ? UnusedColor : DefaultOnlyColor;
            GUI.Label(lamp, new GUIContent("●", row.Tooltip), lampStyle);
            GUI.contentColor = color;
            GUI.Label(key, new GUIContent(row.Key, row.Tooltip), EditorStyles.label);
            GUI.Label(value, new GUIContent(row.Value, row.Tooltip), EditorStyles.label);
            using (new EditorGUI.DisabledScope(row.Usage != Usage.Unused || EditorApplication.isPlaying))
                return GUI.Button(button, new GUIContent("x", row.Usage == Usage.Unused ? "Delete this field." : "Remove it from defaultSetting first."), EditorStyles.miniButton);
        }
    }

    [CustomEditor(typeof(PlayerPreferences))]
    public class PlayerPreferencesEditor : PreferencesEditor<PlayerPreferences> { }
}
