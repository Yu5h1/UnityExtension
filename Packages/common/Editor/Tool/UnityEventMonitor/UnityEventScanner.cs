// Collects every persistent call from loaded scenes, prefab assets and ScriptableObject assets,
// and judges each one with UnityEventBase.GetValidMethodInfo, the rule Unity itself uses.
// Shared core of UnityEventViewer (interactive), UnityEventReport (batchmode), InlineObjectExport and ObjectExport,
// which also take the inline ScriptableObjects the walk reaches, and of the hierarchy paths all of them print.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// A ScriptableObject stored inside a scene or asset file (an inline instance or a sub-asset), with where it is held.
    /// </summary>
    public class EmbeddedObject
    {
        public ScriptableObject obj;
        public Object holder;
        public string holderPath;
        public string fieldPath;
        public string assetPath;
    }

    public static class UnityEventScanner
    {
        private const string PersistentCalls = "m_PersistentCalls";

        /// <summary>
        /// Scans every loaded scene and, when one is open, the prefab being edited in Prefab Mode.
        /// </summary>
        public static List<UnityEventRecord> ScanOpenScenes()
        {
            ClearReferenceCache();
            var records = new List<UnityEventRecord>();
            var visited = new HashSet<Object>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    ScanScene(scene, records, visited);
            }
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
                ScanHierarchy(stage.prefabContentsRoot, stage.assetPath, records, visited);
            return records;
        }

        public static void ScanScene(Scene scene, List<UnityEventRecord> records, HashSet<Object> visited = null,
            List<EmbeddedObject> embeddedOut = null)
        {
            ClearReferenceCache();
            visited ??= new HashSet<Object>();
            foreach (var root in scene.GetRootGameObjects())
                ScanHierarchy(root, scene.path, records, visited, embeddedOut);
        }

        /// <summary>
        /// Scans prefab and ScriptableObject assets under the given folders or asset paths without opening them in an editor view.
        /// </summary>
        public static List<UnityEventRecord> ScanAssets(params string[] pathsOrFolders) => ScanAssets(pathsOrFolders, null);

        /// <summary>
        /// Same as <see cref="ScanAssets(string[])"/>, also collecting the inline ScriptableObjects the walk reaches.
        /// </summary>
        public static List<UnityEventRecord> ScanAssets(string[] pathsOrFolders, List<EmbeddedObject> embeddedOut)
        {
            ClearReferenceCache();
            var records = new List<UnityEventRecord>();
            var visited = new HashSet<Object>();
            foreach (var path in ResolveAssetPaths(pathsOrFolders))
            {
                if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                        ScanHierarchy(prefab, path, records, visited, embeddedOut);
                }
                else
                {
                    foreach (var asset in InWalkOrder(AssetDatabase.LoadAllAssetsAtPath(path).OfType<ScriptableObject>()))
                        ScanObject(asset, DescribePath(asset), path, records, visited, embeddedOut);
                }
            }
            return records;
        }

        /// <summary>
        /// The main asset first, then sub-assets by local file id. LoadAllAssetsAtPath returns a freshly created asset's
        /// sub-assets before the main asset and a loaded one's after it; a fixed order makes both walks reach a sub-asset
        /// through the same holder, so the output does not depend on how the file was loaded.
        /// </summary>
        private static IEnumerable<ScriptableObject> InWalkOrder(IEnumerable<ScriptableObject> assets) =>
            assets.OrderBy(a => AssetDatabase.IsMainAsset(a) ? 0 : 1).ThenBy(LocalFileId);

        private static long LocalFileId(Object obj) =>
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out _, out long id) ? id : 0;

        internal static IEnumerable<string> ResolveAssetPaths(string[] pathsOrFolders)
        {
            var folders = pathsOrFolders.Where(AssetDatabase.IsValidFolder).ToArray();
            var files = pathsOrFolders.Where(p => !AssetDatabase.IsValidFolder(p));
            var found = folders.Length == 0 ? Enumerable.Empty<string>() :
                AssetDatabase.FindAssets("t:Prefab t:ScriptableObject", folders).Select(AssetDatabase.GUIDToAssetPath);
            return files.Concat(found).Where(p => p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)).Distinct();
        }

        public static void ScanHierarchy(GameObject root, string assetPath, List<UnityEventRecord> records, HashSet<Object> visited = null,
            List<EmbeddedObject> embeddedOut = null)
        {
            visited ??= new HashSet<Object>();
            foreach (var component in root.GetComponentsInChildren<Component>(true))
                if (component != null)
                    ScanObject(component, DescribePath(component), assetPath, records, visited, embeddedOut);
        }

        /// <summary>
        /// Scans one object and follows its references into ScriptableObjects stored in the same file
        /// (inline instances in a scene or sub-assets of the same prefab/asset), which no hierarchy walk reaches.
        /// </summary>
        public static void ScanObject(Object obj, string assetPath, List<UnityEventRecord> records, HashSet<Object> visited = null) =>
            ScanObject(obj, DescribePath(obj), assetPath, records, visited ?? new HashSet<Object>(), null);

        private static void ScanObject(Object obj, string ownerPath, string assetPath, List<UnityEventRecord> records, HashSet<Object> visited,
            List<EmbeddedObject> embeddedOut)
        {
            if (!visited.Add(obj))
                return;
            var embedded = new List<(ScriptableObject so, string field)>();
            using (var so = new SerializedObject(obj))
                CollectCalls(so, obj, ownerPath, assetPath, records, embedded);
            foreach (var (inner, field) in embedded)
            {
                // Judged by what is already recorded, not by visited: LoadAllAssetsAtPath may return a sub-asset before
                // the main asset that holds it, and that sub-asset is then visited as a top-level object first.
                if (embeddedOut != null && !AssetDatabase.IsMainAsset(inner) && !embeddedOut.Exists(e => e.obj == inner))
                    embeddedOut.Add(new EmbeddedObject { obj = inner, holder = obj, holderPath = ownerPath, fieldPath = field, assetPath = assetPath });
                ScanObject(inner, $"{ownerPath} > {inner.GetType().Name}", assetPath, records, visited, embeddedOut);
            }
        }

        private static bool IsEmbedded(ScriptableObject so, string assetPath) =>
            !EditorUtility.IsPersistent(so) || AssetDatabase.GetAssetPath(so) == assetPath;

        private static void CollectCalls(SerializedObject so, Object obj, string ownerPath, string assetPath, List<UnityEventRecord> records,
            List<(ScriptableObject so, string field)> embedded)
        {
            var it = so.GetIterator();
            var enter = true;
            while (it.Next(enter))
            {
                enter = ShouldEnter(it);
                if (it.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (it.objectReferenceValue is ScriptableObject referenced && IsEmbedded(referenced, assetPath))
                        embedded.Add((referenced, it.propertyPath));
                    continue;
                }
                if (it.name != PersistentCalls)
                    continue;
                var calls = it.FindPropertyRelative("m_Calls");
                if (calls == null || !calls.isArray)
                    continue;
                var eventPath = it.propertyPath.Substring(0, Math.Max(0, it.propertyPath.Length - PersistentCalls.Length - 1));
                var eventArgs = GetEventArgumentTypes(so.FindProperty(eventPath));
                for (int i = 0; i < calls.arraySize; i++)
                {
                    var call = calls.GetArrayElementAtIndex(i);
                    records.Add(Evaluate(obj, ownerPath, assetPath, eventPath, i, call, eventArgs));
                    var argumentProperty = call.FindPropertyRelative("m_Arguments.m_ObjectArgument");
                    if (argumentProperty.objectReferenceValue is ScriptableObject argument && IsEmbedded(argument, assetPath))
                        embedded.Add((argument, argumentProperty.propertyPath));
                }
                enter = false;
            }
        }

        private static bool ShouldEnter(SerializedProperty p)
        {
            if (p.propertyType == SerializedPropertyType.ManagedReference)
                return true;
            if (p.propertyType != SerializedPropertyType.Generic)
                return false;
            if (!p.isArray)
                return true;
            return p.arraySize > 0 && p.GetArrayElementAtIndex(0).propertyType is
                SerializedPropertyType.Generic or SerializedPropertyType.ManagedReference or SerializedPropertyType.ObjectReference;
        }

        /// <summary>
        /// Generic arguments of the UnityEvent&lt;T…&gt; the property holds; null when they cannot be read,
        /// in which case EventDefined calls are checked by method name only.
        /// </summary>
        private static Type[] GetEventArgumentTypes(SerializedProperty eventProperty)
        {
            if (eventProperty == null)
                return null;
            object value;
            try { value = eventProperty.boxedValue; }
            catch { return null; }
            for (var t = value?.GetType(); t != null && t != typeof(UnityEventBase); t = t.BaseType)
                if (t.IsGenericType && t.Namespace == typeof(UnityEvent).Namespace && t.Name.StartsWith(nameof(UnityEvent)))
                    return t.GetGenericArguments();
            return value is UnityEvent ? Type.EmptyTypes : null;
        }

        private static UnityEventRecord Evaluate(Object owner, string ownerPath, string assetPath, string eventPath, int index, SerializedProperty call, Type[] eventArgs)
        {
            var targetProp = call.FindPropertyRelative("m_Target");
            var args = call.FindPropertyRelative("m_Arguments");
            var target = targetProp.objectReferenceValue;
            var storedTypeName = call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue;
            var argTypeName = args.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue;
            var record = new UnityEventRecord
            {
                owner = owner,
                assetPath = assetPath,
                ownerPath = ownerPath,
                ownerType = owner.GetType().Name,
                eventPath = eventPath,
                index = index,
                target = target,
                targetPath = target != null ? DescribePath(target) : string.Empty,
                methodName = call.FindPropertyRelative("m_MethodName").stringValue,
                mode = (PersistentListenerMode)call.FindPropertyRelative("m_Mode").intValue,
                callState = (UnityEventCallState)call.FindPropertyRelative("m_CallState").intValue,
                argumentObject = args.FindPropertyRelative("m_ObjectArgument").objectReferenceValue,
            };
            record.argument = DescribeArgument(record.mode, args);

            var targetType = target != null ? target.GetType() : ResolveType(storedTypeName);
            record.targetType = targetType != null ? targetType.FullName : TypeNameOf(storedTypeName);

            if (target == null)
                record.status = HoldsReference(targetProp) ? PersistentCallStatus.MissingTarget : PersistentCallStatus.NullTarget;
            else if (target is MonoBehaviour mb && MonoScript.FromMonoBehaviour(mb) == null)
                record.status = PersistentCallStatus.MissingTarget;
            else if (record.methodName.IsEmpty())
                record.status = PersistentCallStatus.NoFunction;
            else
                record.status = Judge(targetType, record.methodName, record.mode, argTypeName, eventArgs, out record.detail);

            if (record.status == PersistentCallStatus.Valid && !storedTypeName.IsEmpty() && ResolveType(storedTypeName) == null)
            {
                record.status = PersistentCallStatus.StaleTargetType;
                record.detail = $"Stored target type '{storedTypeName}' no longer resolves.";
            }
            return record;
        }

        private static PersistentCallStatus Judge(Type type, string method, PersistentListenerMode mode, string argTypeName, Type[] eventArgs, out string detail)
        {
            detail = null;
            Type[] argTypes;
            switch (mode)
            {
                case PersistentListenerMode.Void: argTypes = Type.EmptyTypes; break;
                case PersistentListenerMode.Object:
                    var argType = argTypeName.IsEmpty() ? null : ResolveType(argTypeName);
                    if (!argTypeName.IsEmpty() && argType == null)
                    {
                        detail = $"Argument type '{argTypeName}' no longer resolves.";
                        return HasMethodNamed(type, method) ? PersistentCallStatus.StaleArgumentType : PersistentCallStatus.MethodNotFound;
                    }
                    argTypes = new[] { argType ?? typeof(Object) };
                    break;
                case PersistentListenerMode.Int: argTypes = new[] { typeof(int) }; break;
                case PersistentListenerMode.Float: argTypes = new[] { typeof(float) }; break;
                case PersistentListenerMode.String: argTypes = new[] { typeof(string) }; break;
                case PersistentListenerMode.Bool: argTypes = new[] { typeof(bool) }; break;
                default: argTypes = eventArgs; break;
            }
            if (argTypes == null)
                return HasMethodNamed(type, method) ? PersistentCallStatus.Valid : PersistentCallStatus.MethodNotFound;
            if (UnityEventBase.GetValidMethodInfo(type, method, argTypes) != null)
                return PersistentCallStatus.Valid;
            if (!HasMethodNamed(type, method))
                return PersistentCallStatus.MethodNotFound;
            detail = $"No overload of {method} accepts ({string.Join(", ", argTypes.Select(t => t.Name))}).";
            return PersistentCallStatus.SignatureMismatch;
        }

        private static readonly PropertyInfo ReferenceIdProperty =
            typeof(SerializedProperty).GetProperty("objectReferenceEntityIdValue") ??
            typeof(SerializedProperty).GetProperty("objectReferenceInstanceIDValue");

        /// <summary>
        /// Whether an object-reference property still stores an object id although the object cannot be loaded,
        /// which is what the Inspector draws as "Missing". Reads the entity id on Unity versions that have it and the
        /// instance id on older ones; returns false when neither member exists.
        /// </summary>
        internal static bool HoldsReference(SerializedProperty property)
        {
            var id = ReferenceIdProperty?.GetValue(property);
            return id != null && !id.Equals(Activator.CreateInstance(id.GetType()));
        }

        private const BindingFlags AnyMember = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static bool HasMethodNamed(Type type, string method)
        {
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
                if (t.GetMethods(AnyMember).Any(m => m.Name == method))
                    return true;
            return false;
        }

        /// <summary>
        /// Type part of an assembly-qualified name, keeping commas inside generic argument brackets.
        /// </summary>
        private static string TypeNameOf(string assemblyQualifiedName)
        {
            if (assemblyQualifiedName.IsEmpty())
                return string.Empty;
            var depth = 0;
            for (int i = 0; i < assemblyQualifiedName.Length; i++)
            {
                var ch = assemblyQualifiedName[i];
                if (ch == '[') depth++;
                else if (ch == ']') depth--;
                else if (ch == ',' && depth == 0) return assemblyQualifiedName.Substring(0, i);
            }
            return assemblyQualifiedName;
        }

        private static Type ResolveType(string assemblyQualifiedName) =>
            assemblyQualifiedName.IsEmpty() ? null : Type.GetType(assemblyQualifiedName, false);

        private static string DescribeArgument(PersistentListenerMode mode, SerializedProperty args) => mode switch
        {
            PersistentListenerMode.Object => DescribeReference(args.FindPropertyRelative("m_ObjectArgument").objectReferenceValue),
            PersistentListenerMode.Int => args.FindPropertyRelative("m_IntArgument").intValue.ToString(CultureInfo.InvariantCulture),
            PersistentListenerMode.Float => args.FindPropertyRelative("m_FloatArgument").floatValue.ToString("R", CultureInfo.InvariantCulture),
            PersistentListenerMode.String => $"\"{args.FindPropertyRelative("m_StringArgument").stringValue}\"",
            PersistentListenerMode.Bool => args.FindPropertyRelative("m_BoolArgument").boolValue.ToString(),
            PersistentListenerMode.EventDefined => "(dynamic)",
            _ => string.Empty,
        };

        /// <summary>
        /// A reference as the exports write it: "null"; "&lt;path&gt; &lt;Type&gt;" for a scene object;
        /// "&lt;asset&gt; &lt;Type&gt;" for a main asset; "&lt;asset&gt; &gt; &lt;path&gt; &lt;Type&gt;" for a GameObject or component
        /// inside a prefab or model; "&lt;asset&gt; &gt; &lt;name&gt; &lt;Type&gt;" for another sub-asset, with #2, #3… when
        /// sub-assets of that asset share its name and type, numbered by local file id; "transient: &lt;path or name&gt;
        /// &lt;Type&gt;" for an object Unity does not save (<see cref="IsTransient"/>), which the saved file holds as null.
        /// </summary>
        public static string DescribeReference(Object obj)
        {
            if (obj == null)
                return "null";
            var type = obj.GetType().Name;
            if (IsTransient(obj))
                return $"transient: {(obj is GameObject or Component ? DescribePath(obj) : Escape(obj.name))} <{type}>";
            if (!EditorUtility.IsPersistent(obj))
                return $"{DescribePath(obj)} <{type}>";
            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsMainAsset(obj))
                return $"{assetPath} <{type}>";
            var inFile = obj is GameObject or Component ? DescribePath(obj) : SubAssetName(obj, assetPath);
            return $"{assetPath} > {inFile} <{type}>";
        }

        private static readonly Dictionary<string, Object[]> SubAssets = new();

        /// <summary>
        /// Forgets the sub-asset lists <see cref="DescribeReference"/> caches; every scan starts with it, so a long editor
        /// session never numbers sub-assets from a stale list.
        /// </summary>
        private static void ClearReferenceCache() => SubAssets.Clear();

        private static string SubAssetName(Object obj, string assetPath)
        {
            if (!SubAssets.TryGetValue(assetPath, out var all))
                SubAssets[assetPath] = all = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                    .Where(a => a != null && a is not GameObject && a is not Component && !AssetDatabase.IsMainAsset(a))
                    .OrderBy(LocalFileId).ToArray();
            // Compared by short type name, the name the reference writes, so two types of one name are numbered apart.
            var type = obj.GetType().Name;
            var ordinal = 1;
            foreach (var other in all)
            {
                if (other == obj)
                    break;
                if (other.name == obj.name && other.GetType().Name == type)
                    ordinal++;
            }
            return WithOrdinal(Escape(obj.name), ordinal);
        }

        /// <summary>
        /// Whether the object lives only in memory and Unity leaves it out of the file it belongs to: not an asset, and
        /// flagged <see cref="HideFlags.DontSaveInEditor"/>, such as the mesh and MeshFilter TextMeshPro creates at edit time.
        /// Built-in resources (the Cube mesh, LegacyRuntime font) carry that flag too but are assets, so they are not
        /// transient. The exports skip transient GameObjects and components and do not count them for #n, so their paths are
        /// the same whether or not those objects exist at the moment.
        /// </summary>
        internal static bool IsTransient(Object obj)
            => (obj.hideFlags & HideFlags.DontSaveInEditor) != 0 && !EditorUtility.IsPersistent(obj);

        /// <summary>
        /// A name as it appears in a path: backslash, slash and # are written \\, \/ and \#, so a name is never read as
        /// two path segments or as a #n ordinal.
        /// </summary>
        internal static string Escape(string name) => name.Replace("\\", "\\\\").Replace("/", "\\/").Replace("#", "\\#");

        /// <summary>
        /// Hierarchy path for scene and prefab objects, asset name otherwise. A GameObject named like an earlier sibling
        /// gets #2, #3… after its name, and so does a component whose type repeats on its GameObject, so no two objects
        /// in one file share a path. Names are escaped as <see cref="Escape"/> says.
        /// </summary>
        public static string DescribePath(Object obj)
        {
            var go = obj switch { GameObject g => g, Component c => c.gameObject, _ => null };
            if (go == null)
                return obj.name;
            var path = NameAmongSiblings(go.transform);
            for (var t = go.transform.parent; t != null; t = t.parent)
                path = $"{NameAmongSiblings(t)}/{path}";
            return obj is Component component ? $"{path} ({ComponentId(component)})" : path;
        }

        /// <summary>
        /// The component's type name, followed by #n when an earlier component on the same GameObject has the same type.
        /// </summary>
        internal static string ComponentId(Component component)
        {
            var type = component.GetType();
            var ordinal = 1;
            foreach (var other in component.GetComponents<Component>())
            {
                if (other == component)
                    break;
                if (other != null && other.GetType() == type && !IsTransient(other))
                    ordinal++;
            }
            return WithOrdinal(type.Name, ordinal);
        }

        internal static string WithOrdinal(string name, int ordinal) => ordinal == 1 ? name : $"{name}#{ordinal}";

        private static string NameAmongSiblings(Transform transform)
        {
            var name = transform.name;
            var ordinal = 1;
            var parent = transform.parent;
            if (parent != null)
            {
                for (int i = 0, end = transform.GetSiblingIndex(); i < end; i++)
                    if (parent.GetChild(i) is var sibling && sibling.name == name && !IsTransient(sibling.gameObject))
                        ordinal++;
            }
            else if (transform.gameObject.scene.IsValid())
            {
                foreach (var root in transform.gameObject.scene.GetRootGameObjects())
                {
                    if (root.transform == transform)
                        break;
                    if (root.name == name && !IsTransient(root))
                        ordinal++;
                }
            }
            return WithOrdinal(Escape(name), ordinal);
        }
    }
}
