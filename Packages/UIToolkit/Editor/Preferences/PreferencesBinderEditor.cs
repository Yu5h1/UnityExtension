using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Yu5h1Lib.UIToolkit;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Shows every <c>binding-path</c> the binder's UXML produces with whether the save and
    /// <c>defaultSetting</c> hold it, outlines the ones sharing a field name with another consumer of the
    /// same save (<see cref="PreferencesConsumerRegistry"/>), outlines in red the ones whose element value type
    /// does not match the host's source member of that name, and renames a binding by writing the new
    /// value back into the UXML file. Also registers UXML bindings as consumers for the Preferences inspector.
    /// </summary>
    [CustomEditor(typeof(PreferencesBinder))]
    public class PreferencesBinderEditor : Editor<PreferencesBinder>
    {
        private static readonly Color ConflictColor = new Color(1f, 0.76f, 0.03f);
        private static readonly Color MismatchColor = new Color(0.9f, 0.25f, 0.2f);

        private string scannedAsset;
        private readonly Dictionary<string, DateTime> scannedStamps = new Dictionary<string, DateTime>();
        private List<UxmlBinding> entries = new List<UxmlBinding>();
        private readonly Dictionary<UxmlBinding, string> conflicts = new Dictionary<UxmlBinding, string>();
        private readonly Dictionary<UxmlBinding, string> storage = new Dictionary<UxmlBinding, string>();
        private readonly Dictionary<UxmlBinding, string> mismatches = new Dictionary<UxmlBinding, string>();
        private readonly List<string> sharedTemplateHints = new List<string>();
        private bool conflictsDirty = true;

        [InitializeOnLoadMethod]
        private static void RegisterConsumers() => PreferencesConsumerRegistry.Register(CollectUxmlBindings);

        private static IEnumerable<PreferencesConsumer> CollectUxmlBindings(string storageKey)
        {
            foreach (var binder in ObjectUtility.FindObjects<PreferencesBinder>())
            {
                if (binder.preferences == null || binder.preferences.KEY != storageKey)
                    continue;
                var path = GetAssetPath(binder);
                if (string.IsNullOrEmpty(path))
                    continue;
                foreach (var entry in UxmlBindingScanner.Scan(path))
                    yield return new PreferencesConsumer(entry.BindingPath,
                        $"UXML {entry.DisplayName} ({Path.GetFileName(entry.DefinedIn)}:{entry.Line}) via '{binder.name}'");
            }
        }

        private static string GetAssetPath(PreferencesBinder binder)
        {
            var asset = binder.GetComponent<UIDocument>()?.visualTreeAsset;
            return asset == null ? null : AssetDatabase.GetAssetPath(asset);
        }

        private void OnEnable()
        {
            EditorApplication.hierarchyChanged += MarkConflictsDirty;
            EditorApplication.projectChanged += MarkConflictsDirty;
            Undo.undoRedoPerformed += MarkConflictsDirty;
        }

        protected override void OnDisable()
        {
            EditorApplication.hierarchyChanged -= MarkConflictsDirty;
            EditorApplication.projectChanged -= MarkConflictsDirty;
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

            var assetPath = GetAssetPath(targetObject);
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
                storage.TryGetValue(entry, out var stored);
                mismatches.TryGetValue(entry, out var mismatch);
                var tooltip = $"{entry.ElementType} · {entry.DefinedIn}:{entry.Line}" + (entry.FromOverride ? " (AttributeOverrides)" : "")
                    + (mismatch == null ? "" : "\n" + mismatch)
                    + (conflict == null ? "" : "\n" + conflict);
                using (new EditorGUILayout.HorizontalScope())
                {
                    var value = EditorGUILayout.DelayedTextField(new GUIContent(entry.DisplayName, tooltip), entry.BindingPath);
                    if (mismatch != null)
                        DrawOutline(GUILayoutUtility.GetLastRect(), MismatchColor);
                    else if (conflict != null)
                        DrawOutline(GUILayoutUtility.GetLastRect(), ConflictColor);
                    EditorGUILayout.LabelField(stored, EditorStyles.miniLabel, GUILayout.Width(90));
                    if (value != entry.BindingPath)
                    {
                        renamed = entry;
                        newValue = value;
                    }
                }
            }

            if (targetObject.preferences == null)
                EditorGUILayout.HelpBox("Assign Preferences to check these against its save and its other bindings.", MessageType.Info);
            foreach (var pair in mismatches)
                EditorGUILayout.HelpBox($"{pair.Key.DisplayName}: {pair.Value}.", MessageType.Error);
            if (conflicts.Count > 0)
                EditorGUILayout.HelpBox("Yellow-outlined fields share a field name with another binding on the same save; they will overwrite each other at runtime. Hover a field to see what it collides with.", MessageType.Warning);
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
            storage.Clear();
            mismatches.Clear();
            sharedTemplateHints.Clear();

            var preferences = targetObject.preferences;
            // The registry already includes this binder's own UXML when a Preferences is assigned.
            var consumers = preferences != null
                ? PreferencesConsumerRegistry.Collect(preferences.KEY)
                : entries.Select(e => new PreferencesConsumer(e.BindingPath, $"UXML {e.DisplayName} ({Path.GetFileName(e.DefinedIn)}:{e.Line})")).ToList();
            var sources = consumers
                .GroupBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(c => c.Source).ToList(), StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
                if (sources.TryGetValue(entry.BindingPath, out var list) && list.Count > 1)
                    conflicts[entry] = "Also bound by:\n" + string.Join("\n", list.Distinct());

            if (preferences != null)
                foreach (var entry in entries)
                {
                    var portType = VisualElementPortFactory.GetValueType(entry.ElementType);
                    if (portType != null && preferences.TryGetSourceMemberType(entry.BindingPath, out var memberType)
                        && !Preferences.IsCompatible(portType, memberType))
                        mismatches[entry] = Preferences.DescribeMismatch(entry.BindingPath, portType, memberType);
                }

            if (preferences != null)
            {
                var hasSave = preferences.TryLoadCurrent(out var saved) && saved != null;
                foreach (var entry in entries)
                {
                    var isSaved = hasSave && saved.Any(pair => string.Equals(pair.Key, entry.BindingPath, StringComparison.OrdinalIgnoreCase));
                    var isDefault = preferences.TryGetDefault(entry.BindingPath, out _);
                    storage[entry] = isSaved && isDefault ? "saved · default"
                        : isSaved ? "saved"
                        : isDefault ? "default"
                        : "not saved yet";
                }
            }

            foreach (var shared in entries.Where(e => !e.FromOverride).GroupBy(e => e.SourceKey).Where(g => g.Count() > 1))
            {
                var first = shared.First();
                sharedTemplateHints.Add($"'{first.BindingPath}' comes from one element in template {Path.GetFileName(first.DefinedIn)}, instanced {shared.Count()} times. Renaming it renames every instance; to separate them, add <ui:AttributeOverrides element-name=\"…\" binding-path=\"…\" /> to each <ui:Instance> in the parent file (the template element needs a name).");
            }
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            var field = new Rect(rect.x + EditorGUIUtility.labelWidth + 2, rect.y, rect.width - EditorGUIUtility.labelWidth - 2, rect.height);
            EditorGUI.DrawRect(new Rect(field.x, field.y, field.width, 1), color);
            EditorGUI.DrawRect(new Rect(field.x, field.yMax - 1, field.width, 1), color);
            EditorGUI.DrawRect(new Rect(field.x, field.y, 1, field.height), color);
            EditorGUI.DrawRect(new Rect(field.xMax - 1, field.y, 1, field.height), color);
        }
    }
}
