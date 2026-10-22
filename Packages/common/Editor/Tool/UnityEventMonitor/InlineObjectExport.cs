// Batchmode entry of the agent translator: exports every inline ScriptableObject in the given scenes and assets
// with its serialized fields and UnityEvent calls, plus the UnityEvents of the objects that hold them.
// References are written as names, hierarchy paths, asset paths or inline paths, never as fileID or GUID.
// Shares UnityEventScanner's walk and records with UnityEventReport.
//
//   Unity.exe -batchmode -projectPath <project> -logFile <log>
//     -executeMethod Yu5h1Lib.EditorExtension.InlineObjectExport.Run
//     -ioScenes "Assets/Scene/A.unity;Assets/Scene/B.unity"
//     -ioAssets "Assets/Prefabs;Assets/Data/Some.asset"
//     -ioOut "<path>.json"
//
// Exit code: 0 written, 2 the export failed. The output carries no timestamp, so two runs over the same files match.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.EditorExtension
{
    public static class InlineObjectExport
    {
        [Serializable]
        private class Field
        {
            public string path;
            public string type;
            public string value;
        }

        [Serializable]
        private class Call
        {
            public int index;
            public string target;
            public string targetType;
            public string method;
            public string mode;
            public string argument;
            public string status;
        }

        [Serializable]
        private class EventEntry
        {
            public string path;
            public int count;
            public int dynamicCount;
            public List<Call> calls = new();
        }

        [Serializable]
        private class Entry
        {
            public string asset;
            public string holder;
            public string field;
            public string type;
            public string name;
            public List<Field> fields = new();
            public List<EventEntry> events = new();
        }

        [Serializable]
        private class Holder
        {
            public string asset;
            public string path;
            public string type;
            public List<EventEntry> events = new();
        }

        [Serializable]
        private class ChannelTotal
        {
            public string scope;
            public string eventPath;
            public int count;
            public int dynamicCount;
        }

        [Serializable]
        private class Document
        {
            public string unityVersion;
            public int inlineObjects;
            public List<ChannelTotal> totals = new();
            public List<Holder> holders = new();
            public List<Entry> objects = new();
        }

        private static readonly HashSet<string> EngineFields = new()
        {
            "m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset", "m_GameObject",
            "m_Enabled", "m_EditorHideFlags", "m_Script", "m_Name", "m_EditorClassIdentifier",
        };

        public static void Run()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                var doc = new Document { unityVersion = Application.unityVersion };
                foreach (var scenePath in UnityEventReport.SplitArgument(args, "-ioScenes"))
                {
                    var records = new List<UnityEventRecord>();
                    var embedded = new List<EmbeddedObject>();
                    UnityEventScanner.ScanScene(EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single), records, null, embedded);
                    Append(doc, records, embedded);
                }
                var assets = UnityEventReport.SplitArgument(args, "-ioAssets");
                if (assets.Length > 0)
                {
                    var embedded = new List<EmbeddedObject>();
                    var records = UnityEventScanner.ScanAssets(assets, embedded);
                    Append(doc, records, embedded);
                }
                var output = UnityEventReport.GetArgument(args, "-ioOut") ?? "InlineObjectExport.json";
                Write(doc, output);
                Debug.Log($"[InlineObjectExport] {doc.inlineObjects} inline objects, {doc.holders.Count} holders -> {Path.GetFullPath(output)}");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(2);
            }
        }

        /// <summary>
        /// Adds one scan's inline objects and holders while the scanned objects are still loaded.
        /// </summary>
        private static void Append(Document doc, List<UnityEventRecord> records, List<EmbeddedObject> embedded)
        {
            var inlinePaths = embedded.ToDictionary(e => (Object)e.obj, e => $"{e.holderPath} > {e.fieldPath}");
            var callsByOwner = records.Where(r => r.owner != null).GroupBy(r => r.owner).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var holder in embedded.Where(e => !inlinePaths.ContainsKey(e.holder)).GroupBy(e => e.holder))
            {
                var first = holder.First();
                doc.holders.Add(new Holder
                {
                    asset = first.assetPath,
                    path = first.holderPath,
                    type = holder.Key.GetType().FullName,
                    events = EventsOf(holder.Key, callsByOwner, inlinePaths),
                });
            }
            foreach (var e in embedded)
            {
                doc.objects.Add(new Entry
                {
                    asset = e.assetPath,
                    holder = e.holderPath,
                    field = e.fieldPath,
                    type = e.obj.GetType().FullName,
                    name = e.obj.name,
                    fields = FieldsOf(e.obj, inlinePaths),
                    events = EventsOf(e.obj, callsByOwner, inlinePaths),
                });
            }
            doc.inlineObjects = doc.objects.Count;
            doc.totals = Totals(doc);
        }

        private static List<ChannelTotal> Totals(Document doc)
        {
            var inline = doc.objects.SelectMany(o => o.events).Select(e => ("inline", e));
            var held = doc.holders.SelectMany(h => h.events).Select(e => ("holder", e));
            return inline.Concat(held)
                .GroupBy(x => (x.Item1, x.e.path))
                .Select(g => new ChannelTotal
                {
                    scope = g.Key.Item1,
                    eventPath = g.Key.path,
                    count = g.Sum(x => x.e.count),
                    dynamicCount = g.Sum(x => x.e.dynamicCount),
                })
                .OrderBy(t => t.scope, StringComparer.Ordinal).ThenBy(t => t.eventPath, StringComparer.Ordinal)
                .ToList();
        }

        private static List<EventEntry> EventsOf(Object owner, Dictionary<Object, List<UnityEventRecord>> callsByOwner,
            Dictionary<Object, string> inlinePaths)
        {
            if (!callsByOwner.TryGetValue(owner, out var calls))
                return new List<EventEntry>();
            return calls.GroupBy(r => r.eventPath).Select(g => new EventEntry
            {
                path = g.Key,
                count = g.Count(),
                dynamicCount = g.Count(r => r.mode == PersistentListenerMode.EventDefined),
                calls = g.OrderBy(r => r.index).Select(r => new Call
                {
                    index = r.index,
                    target = r.target != null && inlinePaths.TryGetValue(r.target, out var inline) ? $"inline: {inline}" : r.targetPath,
                    targetType = r.targetType,
                    method = r.methodName,
                    mode = r.mode.ToString(),
                    argument = r.argument,
                    status = r.status.ToString(),
                }).ToList(),
            }).ToList();
        }

        private static List<Field> FieldsOf(Object obj, Dictionary<Object, string> inlinePaths)
        {
            var fields = new List<Field>();
            using var so = new SerializedObject(obj);
            var it = so.GetIterator();
            var enter = true;
            while (it.Next(enter))
            {
                enter = false;
                if (it.depth == 0 && EngineFields.Contains(it.name))
                    continue;
                if (it.propertyType == SerializedPropertyType.ArraySize)
                    continue;
                if (it.propertyType == SerializedPropertyType.Generic)
                {
                    if (it.FindPropertyRelative("m_PersistentCalls") != null)
                        continue;
                    if (it.isArray && it.name != "Array")
                        fields.Add(new Field { path = it.propertyPath, type = $"{it.arrayElementType}[]", value = it.arraySize.ToString(CultureInfo.InvariantCulture) });
                    enter = true;
                    continue;
                }
                if (it.propertyType == SerializedPropertyType.ManagedReference)
                {
                    fields.Add(new Field { path = it.propertyPath, type = "managedReference", value = it.managedReferenceFullTypename });
                    enter = true;
                    continue;
                }
                fields.Add(new Field { path = it.propertyPath, type = it.type, value = ValueOf(it, inlinePaths) });
            }
            return fields;
        }

        private static string ValueOf(SerializedProperty p, Dictionary<Object, string> inlinePaths)
        {
            var invariant = CultureInfo.InvariantCulture;
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer: return p.longValue.ToString(invariant);
                case SerializedPropertyType.Boolean: return p.boolValue ? "true" : "false";
                case SerializedPropertyType.Float: return p.doubleValue.ToString("R", invariant);
                case SerializedPropertyType.String: return p.stringValue;
                case SerializedPropertyType.Enum:
                    return p.enumValueIndex >= 0 && p.enumValueIndex < p.enumNames.Length ? p.enumNames[p.enumValueIndex] : p.intValue.ToString(invariant);
                case SerializedPropertyType.LayerMask: return p.intValue.ToString(invariant);
                case SerializedPropertyType.ObjectReference: return Describe(p.objectReferenceValue, inlinePaths);
                case SerializedPropertyType.AnimationCurve: return $"AnimationCurve({p.animationCurveValue?.length ?? 0} keys)";
                default:
                    try { return Convert.ToString(p.boxedValue, invariant) ?? string.Empty; }
                    catch { return $"({p.propertyType})"; }
            }
        }

        private static string Describe(Object obj, Dictionary<Object, string> inlinePaths)
        {
            if (obj == null)
                return "null";
            var type = obj.GetType().Name;
            if (inlinePaths.TryGetValue(obj, out var inline))
                return $"inline: {inline} <{type}>";
            if (EditorUtility.IsPersistent(obj))
            {
                var path = AssetDatabase.GetAssetPath(obj);
                return AssetDatabase.IsMainAsset(obj) ? $"{path} <{type}>" : $"{path} > {obj.name} <{type}>";
            }
            return $"{UnityEventScanner.DescribePath(obj)} <{type}>";
        }

        private static void Write(Document doc, string path)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!directory.IsEmpty())
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonUtility.ToJson(doc, true), new System.Text.UTF8Encoding(false));
        }
    }
}
