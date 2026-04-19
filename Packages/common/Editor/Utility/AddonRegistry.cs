using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Editor 端的 Component→Addon 映射表。
    ///
    /// 預設 resolver：
    ///   1. [RequireComponent] — MonoBehaviour 宣告依賴的 Component（1:1）
    ///   2. [AddonFor]         — 顯式宣告支援的 Component（1:N）
    ///
    /// 其他 resolver 透過 <see cref="RegisterResolver"/> 掛入，例如
    /// <c>OpsGenericResolver</c>（由 bootstrap 自動註冊）。
    ///
    /// 採 lazy 建表：首次存取 <see cref="Map"/> 或註冊新 resolver 時才重建，
    /// 避免 InitializeOnLoadMethod 的順序問題。
    /// </summary>
    public static class AddonRegistry
    {
        // targetComponentType → addonType（先註冊優先）
        private static Dictionary<Type, Type> _map;
        private static bool _dirty = true;

        private static HashSet<IAddonResolver> _resolvers = new HashSet<IAddonResolver>
        {
            //new RequireComponentResolver(),
            new AddonForResolver(),
        };

        private static Dictionary<Type, Type> Map
        {
            get
            {
                if (_map == null || _dirty) Build();
                return _map;
            }
        }

        public static void RegisterResolver(IAddonResolver resolver)
        {
            if (resolver == null) return;
            if (_resolvers.Add(resolver))
                _dirty = true;
        }

        private static void Build()
        {
            _map ??= new Dictionary<Type, Type>();
            _map.Clear();

            var monoBehaviourType = typeof(MonoBehaviour);

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract || type.IsInterface) continue;
                    if (!monoBehaviourType.IsAssignableFrom(type)) continue;

                    foreach (var resolver in _resolvers)
                        foreach (var componentType in resolver.GetTargetComponents(type))
                            TryAdd(componentType, type);
                }
            }

            _dirty = false;
        }

        private static void TryAdd(Type targetType, Type addonType)
        {
            if (targetType == null) return;
            if (_map.ContainsKey(targetType)) return; // 先註冊優先
            _map[targetType] = addonType;
        }

        #region Public API

        /// <summary>
        /// 查詢某個 Component 型別對應的 Addon。
        /// 會沿繼承鏈往上找，找到最接近的匹配。
        /// </summary>
        public static bool TryGetAddonType(Type componentType, out Type addonType)
        {
            addonType = null;
            if (componentType == null) return false;

            var map = Map;
            for (var t = componentType; t != null && t != typeof(Component); t = t.BaseType)
            {
                if (map.TryGetValue(t, out addonType))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 對 GameObject 上所有缺少 Addon 的 Component 自動補上。
        /// </summary>
        public static void AddMissingAddons(GameObject go)
        {
            if (go == null) return;

            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                if (!TryGetAddonType(comp.GetType(), out var addonType)) continue;
                if (go.GetComponent(addonType) != null) continue;

                Undo.AddComponent(go, addonType);
            }
        }

        #endregion

        #region Menu

        [MenuItem("Yu5h1Lib/Addon Registry/Print Map")]
        private static void PrintMap()
        {
            var map = Map;
            if (map.Count == 0)
            {
                Debug.LogWarning("[AddonRegistry] Map is empty.");
                return;
            }
            var lines = map.OrderBy(kv => kv.Key.Name)
                           .Select(kv => $"  {kv.Key.Name,-30} -> {kv.Value.FullName}");
            Debug.Log($"[AddonRegistry] {map.Count} entries:\n{string.Join("\n", lines)}");
        }

        [MenuItem("Yu5h1Lib/Addon Registry/Rebuild")]
        private static void RebuildMenu()
        {
            _dirty = true;
            Debug.Log($"[AddonRegistry] Rebuilt, {Map.Count} entries.");
        }

        [MenuItem("CONTEXT/Component/Add Addon")]
        private static void AddAddonContext(MenuCommand cmd)
        {
            if (!(cmd.context is Component target)) return;
            if (!TryGetAddonType(target.GetType(), out var addonType)) return;
            if (target.GetComponent(addonType) != null) return;

            Undo.AddComponent(target.gameObject, addonType);
        }

        [MenuItem("CONTEXT/Component/Add Addon", validate = true)]
        private static bool ValidateAddAddonContext(MenuCommand cmd)
        {
            if (!(cmd.context is Component target)) return false;
            if (!TryGetAddonType(target.GetType(), out var addonType)) return false;
            return target.GetComponent(addonType) == null;
        }

        public static void AddMissingAddons(GameObject[] targets)
        {
            if (targets.IsEmpty()) return;
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Add Missing Addons");
            int group = Undo.GetCurrentGroup();

            int totalBefore = 0, totalAfter = 0;
            foreach (var go in targets)
            {
                totalBefore += go.GetComponents<Component>().Length;
                AddMissingAddons(go);
                totalAfter += go.GetComponents<Component>().Length;
            }

            Undo.CollapseUndoOperations(group);
            //Debug.Log($"[AddonRegistry] Processed {targets.Length} GameObject(s), added {totalAfter - totalBefore} addon(s).");
        }

        public const string baseMenuPath = "GameObject/Add Missing Addons";

        // 對選中的所有 GameObject 批次執行 AddMissingAddons
        [MenuItem(baseMenuPath, false)]
        private static void AddMissingAddonsToSelection() => AddMissingAddons(Selection.gameObjects);
        [MenuItem(baseMenuPath, true)]
        private static bool ValidateAddMissingAddonsToSelection() => Selection.gameObjects != null && Selection.gameObjects.Length > 0;


        #endregion

        #region Resolvers

        public interface IAddonResolver
        {
            /// <summary>
            /// 給定一個候選 addon 型別，回傳它應該掛在哪些 Component 上。
            /// 回傳空序列表示這個 resolver 不處理此型別。
            /// </summary>
            IEnumerable<Type> GetTargetComponents(Type addonType);
        }

        private class RequireComponentResolver : IAddonResolver
        {
            // RequireComponent 常被用作「結構依賴」而非 Addon 映射意圖，
            // 這些型別幾乎所有 UI 元件都會標，納入會造成誤判。
            private static readonly HashSet<Type> _excluded = new HashSet<Type>
            {
                typeof(Transform),
                typeof(RectTransform),
                typeof(CanvasRenderer)                
            };

            public IEnumerable<Type> GetTargetComponents(Type addonType)
            {
                var attrs = addonType.GetCustomAttributes<RequireComponent>(inherit: true);
                foreach (var attr in attrs)
                {
                    if (IsUsable(attr.m_Type0)) yield return attr.m_Type0;
                    if (IsUsable(attr.m_Type1)) yield return attr.m_Type1;
                    if (IsUsable(attr.m_Type2)) yield return attr.m_Type2;
                }
            }

            private static bool IsUsable(Type t) => t != null && !_excluded.Contains(t);
        }

        private class AddonForResolver : IAddonResolver
        {
            public IEnumerable<Type> GetTargetComponents(Type addonType)
            {
                var attrs = addonType.GetCustomAttributes<AddonForAttribute>(inherit: false);
                foreach (var attr in attrs)
                    if (attr.TargetType != null)
                        yield return attr.TargetType;
            }
        }

        #endregion
    }
}
