// Batchmode entry of the agent translator for general objects: exports every GameObject in the given scenes and prefabs
// with its hierarchy, components and serialized fields, every ScriptableObject asset with its fields, and the inline
// ScriptableObjects they hold. Fields and UnityEvent channels are written as InlineObjectExport writes them, listeners
// come from UnityEventScanner, and every path is UnityEventScanner.DescribePath, which marks repeated names with #n.
// WiringBuildCheck runs the same export over the scenes of a player build.
//
//   Unity.exe -batchmode -projectPath <project> -logFile <log>
//     -executeMethod Yu5h1Lib.EditorExtension.ObjectExport.Run
//     -oeScenes "Assets/Scene/A.unity;Assets/Scene/B.unity"
//     -oeAssets "Assets/Prefabs;Assets/Data/Some.asset"
//     -oeOut "<path>.json"
//
// Exit code: 0 written, 2 the export failed. The output carries no timestamp, so two runs over the same files match.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Field = Yu5h1Lib.EditorExtension.InlineObjectExport.Field;
using EventEntry = Yu5h1Lib.EditorExtension.InlineObjectExport.EventEntry;

namespace Yu5h1Lib.EditorExtension
{
    public static class ObjectExport
    {
        internal const string MissingScript = "(missing script)";

        [Serializable]
        internal class ComponentEntry
        {
            public string id;
            public string type;
            public string enabled;
            public bool added;
            public List<string> overrides = new();
            public List<Field> fields = new();
            public List<EventEntry> events = new();
        }

        [Serializable]
        internal class GameObjectEntry
        {
            public string asset;
            public string path;
            public bool active;
            public string tag;
            public string layer;
            public string prefab;
            public bool added;
            public List<string> overrides = new();
            public List<string> removedComponents = new();
            public List<string> removedGameObjects = new();
            public List<ComponentEntry> components = new();
        }

        [Serializable]
        internal class AssetEntry
        {
            public string asset;
            public string type;
            public string name;
            public List<Field> fields = new();
            public List<EventEntry> events = new();
        }

        [Serializable]
        internal class Document
        {
            public string unityVersion;
            public int gameObjectCount;
            public int componentCount;
            public int missingScripts;
            public int missingReferences;
            public int brokenCalls;
            public List<GameObjectEntry> gameObjects = new();
            public List<AssetEntry> assets = new();
            public List<InlineObjectExport.Entry> inlineObjects = new();
        }

        public static void Run()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                var doc = new Document { unityVersion = Application.unityVersion };
                foreach (var scenePath in UnityEventReport.SplitArgument(args, "-oeScenes"))
                    AppendScene(doc, EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single));
                var assets = UnityEventReport.SplitArgument(args, "-oeAssets");
                if (assets.Length > 0)
                    AppendAssets(doc, assets);
                var output = UnityEventReport.GetArgument(args, "-oeOut") ?? "ObjectExport.json";
                InlineObjectExport.Write(doc, output);
                Debug.Log($"[ObjectExport] {doc.gameObjectCount} game objects, {doc.componentCount} components, {doc.assets.Count} assets, " +
                    $"{doc.inlineObjects.Count} inline objects -> {Path.GetFullPath(output)}");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(2);
            }
        }

        internal static void AppendScene(Document doc, Scene scene)
        {
            var records = new List<UnityEventRecord>();
            var embedded = new List<EmbeddedObject>();
            UnityEventScanner.ScanScene(scene, records, null, embedded);
            var inlinePaths = InlineObjectExport.InlinePathsOf(embedded);
            var callsByOwner = InlineObjectExport.CallsByOwner(records);
            foreach (var root in scene.GetRootGameObjects())
                if (!UnityEventScanner.IsTransient(root))
                    AppendHierarchy(doc, root, scene.path, inlinePaths, callsByOwner, false);
            AppendInline(doc, records, embedded);
        }

        /// <summary>
        /// Adds the prefabs and ScriptableObject assets under the given folders or asset paths, in the order the scanner walks them.
        /// </summary>
        internal static void AppendAssets(Document doc, string[] pathsOrFolders)
        {
            var embedded = new List<EmbeddedObject>();
            var records = UnityEventScanner.ScanAssets(pathsOrFolders, embedded);
            var inlinePaths = InlineObjectExport.InlinePathsOf(embedded);
            var callsByOwner = InlineObjectExport.CallsByOwner(records);
            foreach (var path in UnityEventScanner.ResolveAssetPaths(pathsOrFolders))
            {
                if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                        AppendHierarchy(doc, prefab, path, inlinePaths, callsByOwner, false);
                }
                else if (AssetDatabase.LoadMainAssetAtPath(path) is ScriptableObject asset)
                {
                    var entry = new AssetEntry
                    {
                        asset = path,
                        type = asset.GetType().FullName,
                        name = asset.name,
                        fields = InlineObjectExport.FieldsOf(asset, inlinePaths),
                        events = InlineObjectExport.EventsOf(asset, callsByOwner, inlinePaths),
                    };
                    doc.missingReferences += entry.fields.Count(IsMissing);
                    doc.assets.Add(entry);
                }
            }
            AppendInline(doc, records, embedded);
        }

        private static void AppendInline(Document doc, List<UnityEventRecord> records, List<EmbeddedObject> embedded)
        {
            var inline = new InlineObjectExport.Document();
            InlineObjectExport.Append(inline, records, embedded);
            doc.inlineObjects.AddRange(inline.objects);
            doc.missingReferences += inline.objects.Sum(o => o.fields.Count(IsMissing));
            doc.brokenCalls += records.Count(r => r.IsBroken);
        }

        /// <summary>
        /// Adds the GameObject and its descendants depth-first in sibling order, each with its components in GameObject order.
        /// GameObjects and components Unity does not save (<see cref="UnityEventScanner.IsTransient"/>) are left out.
        /// </summary>
        private static void AppendHierarchy(Document doc, GameObject go, string assetPath, Dictionary<Object, string> inlinePaths,
            Dictionary<Object, List<UnityEventRecord>> callsByOwner, bool parentAdded)
        {
            var entry = new GameObjectEntry
            {
                asset = assetPath,
                path = UnityEventScanner.DescribePath(go),
                active = go.activeSelf,
                tag = go.tag,
                layer = LayerMask.LayerToName(go.layer) is { Length: > 0 } layerName ? layerName : go.layer.ToString(),
                prefab = PrefabSourceOf(go),
                added = parentAdded || IsAdded(go),
                overrides = OverridesOf(go, GameObjectFields),
            };
            if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                entry.removedComponents = PrefabUtility.GetRemovedComponents(go)
                    .Where(r => r.assetComponent != null).Select(r => UnityEventScanner.DescribePath(r.assetComponent))
                    .OrderBy(p => p, StringComparer.Ordinal).ToList();
                entry.removedGameObjects = PrefabUtility.GetRemovedGameObjects(go)
                    .Where(r => r.assetGameObject != null).Select(r => UnityEventScanner.DescribePath(r.assetGameObject))
                    .OrderBy(p => p, StringComparer.Ordinal).ToList();
            }
            var missingOrdinal = 0;
            foreach (var component in go.GetComponents<Component>())
            {
                if (component == null)
                {
                    entry.components.Add(new ComponentEntry { id = UnityEventScanner.WithOrdinal(MissingScript, ++missingOrdinal), type = MissingScript });
                    doc.missingScripts++;
                    continue;
                }
                if (UnityEventScanner.IsTransient(component))
                    continue;
                var fields = InlineObjectExport.FieldsOf(component, inlinePaths);
                doc.missingReferences += fields.Count(IsMissing);
                entry.components.Add(new ComponentEntry
                {
                    id = UnityEventScanner.ComponentId(component),
                    type = component.GetType().FullName,
                    enabled = EditorUtility.GetObjectEnabled(component) switch { 0 => "false", 1 => "true", _ => string.Empty },
                    added = entry.added || PrefabUtility.IsAddedComponentOverride(component) || (!HasSource(component) && HasSource(go)),
                    overrides = OverridesOf(component, fields.Select(f => f.path).Prepend(EnabledField).Concat(EventPathsOf(component))),
                    fields = fields,
                    events = InlineObjectExport.EventsOf(component, callsByOwner, inlinePaths),
                });
                doc.componentCount++;
            }
            doc.gameObjects.Add(entry);
            doc.gameObjectCount++;
            foreach (Transform child in go.transform)
                if (!UnityEventScanner.IsTransient(child.gameObject))
                    AppendHierarchy(doc, child.gameObject, assetPath, inlinePaths, callsByOwner, entry.added);
        }

        /// <summary>
        /// Asset path of the prefab this GameObject is the instance root of, "missing" when that asset is gone, empty otherwise.
        /// Inside a prefab asset the nearest instance root is the asset itself, so there the source comes from the corresponding
        /// object: the base of a variant, the nested prefab of a nested instance.
        /// </summary>
        private static string PrefabSourceOf(GameObject go)
        {
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(go))
                return string.Empty;
            if (PrefabUtility.IsPrefabAssetMissing(go))
                return InlineObjectExport.Missing;
            return EditorUtility.IsPersistent(go)
                ? AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(go))
                : PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
        }

        private static readonly string[] GameObjectFields = { "m_Name", "m_IsActive", "m_TagString", "m_Layer" };

        private static bool HasSource(Object obj) => PrefabUtility.GetCorrespondingObjectFromSource(obj) != null;

        private const string EnabledField = "m_Enabled";

        /// <summary>
        /// Whether the GameObject was added inside a prefab instance or variant: Unity reports it as an added GameObject
        /// (a nested prefab instance added to an instance included), or it has no source object while an ancestor has one,
        /// as inside a variant. The caller marks everything under an added GameObject as added too.
        /// </summary>
        private static bool IsAdded(GameObject go)
        {
            if (PrefabUtility.IsAddedGameObjectOverride(go))
                return true;
            if (HasSource(go))
                return false;
            for (var t = go.transform.parent; t != null; t = t.parent)
                if (HasSource(t.gameObject))
                    return true;
            return false;
        }

        /// <summary>
        /// Paths of the UnityEvent channels on the object, as <c>events[].path</c> writes them, including channels with no
        /// listener left, so a channel whose listeners override the source can be listed.
        /// </summary>
        private static IEnumerable<string> EventPathsOf(Object obj)
        {
            var paths = new List<string>();
            using var so = new SerializedObject(obj);
            var it = so.GetIterator();
            while (it.Next(true))
                if (it.name == "m_PersistentCalls" && it.propertyType == SerializedPropertyType.Generic)
                    paths.Add(it.propertyPath.Substring(0, Math.Max(0, it.propertyPath.Length - "m_PersistentCalls".Length - 1)));
            return paths;
        }

        /// <summary>
        /// The given property paths whose value overrides the prefab source, as the Inspector shows them in bold. An array
        /// counts as overridden when its size is; its elements are listed by their own paths. A UnityEvent channel counts
        /// when any of its listeners does, and <c>m_Enabled</c> stands for the component's <c>enabled</c>.
        /// </summary>
        private static List<string> OverridesOf(Object obj, IEnumerable<string> paths)
        {
            var overrides = new List<string>();
            if (!HasSource(obj))
                return overrides;
            using var so = new SerializedObject(obj);
            foreach (var path in paths)
            {
                var p = so.FindProperty(path);
                if (p == null)
                    continue;
                var calls = p.FindPropertyRelative("m_PersistentCalls");
                var overridden = calls != null ? calls.prefabOverride
                    : p.isArray && p.propertyType == SerializedPropertyType.Generic ? p.FindPropertyRelative("Array.size")?.prefabOverride == true
                    : p.prefabOverride;
                if (overridden)
                    overrides.Add(path);
            }
            return overrides;
        }

        internal static bool IsReference(Field field) => field.type.StartsWith("PPtr<", StringComparison.Ordinal);

        private static bool IsMissing(Field field) => IsReference(field) && field.value == InlineObjectExport.Missing;
    }
}
