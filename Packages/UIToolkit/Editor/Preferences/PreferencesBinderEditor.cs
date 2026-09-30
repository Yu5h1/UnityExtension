using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Yu5h1Lib.MVVM;
using Yu5h1Lib.UIToolkit;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Shows every <c>binding-path</c> the binder's UXML produces, outlines the ones that share a field
    /// name with another binding on the same Preferences host (this UXML, another binder, or the host's
    /// uGUI <c>_bindings</c>), and renames a binding by writing the new value back into the UXML file.
    /// </summary>
    [CustomEditor(typeof(PreferencesBinder))]
    public class PreferencesBinderEditor : Editor<PreferencesBinder>
    {
        private const string PreferencesPropertyName = "_preferences";
        private const string BindingsPropertyName = "_bindings";
        private static readonly Color ConflictColor = new Color(1f, 0.76f, 0.03f);

        private string scannedAsset;
        private readonly Dictionary<string, DateTime> scannedStamps = new Dictionary<string, DateTime>();
        private List<UxmlBinding> entries = new List<UxmlBinding>();
        private readonly Dictionary<UxmlBinding, string> conflicts = new Dictionary<UxmlBinding, string>();
        private readonly List<string> sharedTemplateHints = new List<string>();
        private bool conflictsDirty = true;

        private void OnEnable()
        {
            EditorApplication.hierarchyChanged += MarkConflictsDirty;
            Undo.undoRedoPerformed += MarkConflictsDirty;
        }

        protected override void OnDisable()
        {
            EditorApplication.hierarchyChanged -= MarkConflictsDirty;
            Undo.undoRedoPerformed -= MarkConflictsDirty;
            base.OnDisable();
        }

        private void MarkConflictsDirty() => conflictsDirty = true;

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
                conflictsDirty = true;

            if (targets.Length > 1)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UXML Bindings", EditorStyles.boldLabel);

            var asset = targetObject.GetComponent<UIDocument>()?.visualTreeAsset;
            var assetPath = asset == null ? null : AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorGUILayout.HelpBox("The UIDocument has no Source Asset.", MessageType.Info);
                return;
            }

            Refresh(assetPath);
            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox($"{Path.GetFileName(assetPath)} has no element with a binding-path.", MessageType.Info);
                return;
            }

            UxmlBinding renamed = null;
            string newValue = null;
            foreach (var entry in entries)
            {
                conflicts.TryGetValue(entry, out var conflict);
                var tooltip = $"{entry.ElementType} · {entry.DefinedIn}:{entry.Line}" + (entry.FromOverride ? " (AttributeOverrides)" : "")
                    + (conflict == null ? "" : "\n" + conflict);
                var value = EditorGUILayout.DelayedTextField(new GUIContent(entry.DisplayName, tooltip), entry.BindingPath);
                if (conflict != null)
                    DrawOutline(GUILayoutUtility.GetLastRect());
                if (value != entry.BindingPath)
                {
                    renamed = entry;
                    newValue = value;
                }
            }

            if (conflicts.Count > 0)
                EditorGUILayout.HelpBox("Outlined fields share a field name with another binding on the same Preferences; they will overwrite each other at runtime. Hover a field to see what it collides with.", MessageType.Warning);
            foreach (var hint in sharedTemplateHints)
                EditorGUILayout.HelpBox(hint, MessageType.Warning);

            if (renamed != null)
                Rename(renamed, newValue);
        }

        private void Rename(UxmlBinding entry, string newValue)
        {
            if (string.IsNullOrWhiteSpace(newValue))
            {
                "A binding-path cannot be empty.".printWarning();
                return;
            }
            if (!UxmlAttributeWriter.TryReplace(entry.DefinedIn, entry.Line, entry.Column,
                    UxmlBindingScanner.BindingPathAttribute, entry.BindingPath, newValue, out var error))
            {
                error.printWarning();
            }
            scannedAsset = null;
            GUIUtility.ExitGUI();
        }

        private void Refresh(string assetPath)
        {
            if (assetPath != scannedAsset || scannedStamps.Any(pair => ReadStamp(pair.Key) != pair.Value))
            {
                var files = new List<string>();
                entries = UxmlBindingScanner.Scan(assetPath, files);
                scannedStamps.Clear();
                foreach (var file in files)
                    scannedStamps[file] = ReadStamp(file);
                scannedAsset = assetPath;
                conflictsDirty = true;
            }
            if (conflictsDirty)
                CollectConflicts();
        }

        private static DateTime ReadStamp(string assetPath)
        {
            var physical = FileUtil.GetPhysicalPath(assetPath);
            return File.Exists(physical) ? File.GetLastWriteTimeUtc(physical) : DateTime.MinValue;
        }

        private void CollectConflicts()
        {
            conflictsDirty = false;
            conflicts.Clear();
            sharedTemplateHints.Clear();

            var sources = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            void Add(string fieldName, string source)
            {
                if (string.IsNullOrEmpty(fieldName)) return;
                if (!sources.TryGetValue(fieldName, out var list))
                    sources[fieldName] = list = new List<string>();
                list.Add(source);
            }

            foreach (var entry in entries)
                Add(entry.BindingPath, $"this UXML: {entry.DisplayName} ({Path.GetFileName(entry.DefinedIn)}:{entry.Line})");

            var preferences = serializedObject.FindProperty(PreferencesPropertyName)?.objectReferenceValue;
            if (preferences != null)
            {
                foreach (var other in ObjectUtility.FindObjects<PreferencesBinder>())
                {
                    if (other == targetObject || !ReferenceEquals(other.preferences, preferences))
                        continue;
                    var otherAsset = other.GetComponent<UIDocument>()?.visualTreeAsset;
                    var otherPath = otherAsset == null ? null : AssetDatabase.GetAssetPath(otherAsset);
                    if (string.IsNullOrEmpty(otherPath))
                        continue;
                    foreach (var entry in UxmlBindingScanner.Scan(otherPath))
                        Add(entry.BindingPath, $"PreferencesBinder '{other.name}': {entry.DisplayName}");
                }

                var bindings = new SerializedObject(preferences).FindProperty(BindingsPropertyName);
                if (bindings != null)
                    for (int i = 0; i < bindings.arraySize; i++)
                        if (bindings.GetArrayElementAtIndex(i).objectReferenceValue is Component control)
                            Add(control is IValuePort port ? port.GetFieldName() : control.gameObject.name, $"uGUI '{control.name}'");
            }

            foreach (var entry in entries)
                if (sources.TryGetValue(entry.BindingPath, out var list) && list.Count > 1)
                    conflicts[entry] = "Also bound by:\n" + string.Join("\n", list.Distinct());

            foreach (var shared in entries.Where(e => !e.FromOverride).GroupBy(e => e.SourceKey).Where(g => g.Count() > 1))
            {
                var first = shared.First();
                sharedTemplateHints.Add($"'{first.BindingPath}' comes from one element in template {Path.GetFileName(first.DefinedIn)}, instanced {shared.Count()} times. Renaming it renames every instance; to separate them, add <ui:AttributeOverrides element-name=\"…\" binding-path=\"…\" /> to each <ui:Instance> in the parent file (the template element needs a name).");
            }
        }

        private static void DrawOutline(Rect rect)
        {
            var field = new Rect(rect.x + EditorGUIUtility.labelWidth + 2, rect.y, rect.width - EditorGUIUtility.labelWidth - 2, rect.height);
            EditorGUI.DrawRect(new Rect(field.x, field.y, field.width, 1), ConflictColor);
            EditorGUI.DrawRect(new Rect(field.x, field.yMax - 1, field.width, 1), ConflictColor);
            EditorGUI.DrawRect(new Rect(field.x, field.y, 1, field.height), ConflictColor);
            EditorGUI.DrawRect(new Rect(field.xMax - 1, field.y, 1, field.height), ConflictColor);
        }
    }
}
