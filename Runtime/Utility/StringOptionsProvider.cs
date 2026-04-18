using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// 統一的字串選項來源
    /// 供 Dropdown、AutoFill 等功能共用
    /// </summary>
    public static class StringOptionsProvider
    {
        private static Dictionary<string, Func<object, string, string[]>> _providers
             = new Dictionary<string, Func<object, string, string[]>>();

        // Context Map: InstanceID → ListKey
        public static readonly Dictionary<int, string> _contextMap
            = new Dictionary<int, string>();

        #region Registration

        /// <summary>註冊靜態選項</summary>
        public static void Register(string key, string[] options)
        {
            if (string.IsNullOrEmpty(key)) return;
            _providers[key] = (sender, path) => options;
        }

        public static void Register(string key, Func<object, string, string[]> provider)
        {
            _providers[key] = provider;
        }

        // 保留舊的 overload 向後相容
        public static void Register(string key, Func<object, string[]> provider)
        {
            _providers[key] = (sender, path) => provider(sender);
        }

        public static void Register(string key, Func<string[]> provider)
        {
            _providers[key] = (sender, path) => provider();
        }

        public static void Unregister(string key)
            => _providers.Remove(key);

        public static void Clear()
            => _providers.Clear();

        #endregion

        #region Query

        public static string[] GetOptions(object sender, string key, string propertyPath )
        {
            if (_providers.TryGetValue(key, out var provider))
                return provider?.Invoke(sender, propertyPath) ?? Array.Empty<string>();
            else if (_providers.TryGetValue($"~{key}", out provider))
                return provider?.Invoke(sender, propertyPath) ?? Array.Empty<string>();
            return Array.Empty<string>();
        }

        // 保留舊的 overload
        public static string[] GetOptions(string key)
            => GetOptions(null, key, null);

        public static string[] GetOptions(object sender, string key)
            => GetOptions(sender, key, null);

        /// <summary>嘗試取得選項（不輸出警告）</summary>
        public static bool TryGetOptions(object sender, string key, string propertyPath, out string[] options)
        {
            if (!string.IsNullOrEmpty(key) && _providers.TryGetValue(key, out var provider))
            {
                options = provider?.Invoke(sender, propertyPath) ?? Array.Empty<string>();
                return true;
            }
            options = Array.Empty<string>();
            return false;
        }



        public static bool Contains(string key)
            => !string.IsNullOrEmpty(key) && _providers.ContainsKey(key);

        public static IEnumerable<string> GetAllKeys()
            => _providers.Keys;

        public static int Count => _providers.Count;

        #endregion

        #region Display Formatter

        private static Dictionary<string, Func<string, string>> _displayFormatters
            = new Dictionary<string, Func<string, string>>();

        /// <summary>註冊顯示格式化器（值 → 顯示文字）</summary>
        public static void RegisterDisplayFormatter(string key, Func<string, string> formatter)
        {
            if (string.IsNullOrEmpty(key) || formatter == null) return;
            _displayFormatters[key] = formatter;
        }

        /// <summary>取得顯示格式化器，無則回傳 null</summary>
        public static Func<string, string> GetDisplayFormatter(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_displayFormatters.TryGetValue(key, out var f)) return f;
            if (_displayFormatters.TryGetValue($"~{key}", out f)) return f;
            return null;
        }

        #endregion

        #region Context Management

        /// <summary>
        /// 為物件設定 Context（ListKey）
        /// 用於 ScriptableObject 等跨層級引用的情況
        /// </summary>
        public static void SetContext(int instanceID, string listKey)
        {
            if (string.IsNullOrEmpty(listKey)) return;
            _contextMap[instanceID] = listKey;
        }

        /// <summary>
        /// 嘗試取得物件的 Context
        /// </summary>
        public static bool TryGetContext(int instanceID, out string listKey)
        {
            return _contextMap.TryGetValue(instanceID, out listKey);
        }

        public static void ClearContext(int instanceID)
            => _contextMap.Remove(instanceID);

        public static void ClearAllContexts()
            => _contextMap.Clear();

        #endregion
    }
}
