using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Component 設定預設集（統一版本）
    /// - 預設：記憶體模式（快速）
    /// - 需要時：序列化成 JSON（可存檔）
    /// </summary>
    public class ComponentJsonPreset
    {
        private Type componentType;
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();

        // ========== 建構 ==========

        private ComponentJsonPreset(Component source, string[] memberNames)
        {
            componentType = source.GetType();

            if (memberNames == null || memberNames.Length == 0)
                CopyAllMembers(source);
            else
                CopySpecificMembers(source, memberNames);
        }

        // ========== 靜態工廠方法 ==========

        public static ComponentJsonPreset CopyFrom(Component source, params string[] memberNames)
        {
            if (source == null)
            {
                Debug.LogError("❌ Source component is null");
                return null;
            }

            return new ComponentJsonPreset(source, memberNames);
        }

        // ========== 記憶體操作（主要用法）==========

        /// <summary>
        /// 還原到 Component（記憶體模式，最快）
        /// </summary>
        public bool RestoreTo(Component target)
        {
            if (target == null)
            {
                Debug.LogError("❌ Target component is null");
                return false;
            }

            if (target.GetType() != componentType)
            {
                Debug.LogError($"❌ Type mismatch: expected {componentType.Name}, got {target.GetType().Name}");
                return false;
            }

            foreach (var kvp in values)
            {
                try
                {
                    SetMemberValue(target, kvp.Key, kvp.Value);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"⚠️ Failed to restore {kvp.Key}: {ex.Message}");
                }
            }

            return true;
        }

        public bool PasteTo(Component target) => RestoreTo(target);

        // ========== 序列化（需要時才用）==========

        /// <summary>
        /// 轉成 JSON（需要存檔時才用）
        /// </summary>
        public string ToJson(bool prettyPrint = true)
        {
            var data = new SerializedData
            {
                componentType = componentType.AssemblyQualifiedName,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                fields = new List<FieldData>()
            };

            foreach (var kvp in values)
            {
                data.fields.Add(new FieldData
                {
                    name = kvp.Key,
                    typeName = kvp.Value?.GetType().AssemblyQualifiedName ?? "null",
                    value = SerializeValue(kvp.Value)
                });
            }

            return JsonUtility.ToJson(data, prettyPrint);
        }

        /// <summary>
        /// 從 JSON 載入
        /// </summary>
        public static ComponentJsonPreset FromJson(string json)
        {
            try
            {
                var data = JsonUtility.FromJson<SerializedData>(json);
                var preset = new ComponentJsonPreset();
                preset.componentType = Type.GetType(data.componentType);

                foreach (var field in data.fields)
                {
                    var type = Type.GetType(field.typeName);
                    preset.values[field.name] = DeserializeValue(field.value, type);
                }

                return preset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to load from JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 直接存成檔案
        /// </summary>
        public void SaveToFile(string filePath, bool prettyPrint = true)
        {
            try
            {
                string json = ToJson(prettyPrint);
                File.WriteAllText(filePath, json);
                Debug.Log($"💾 Saved to: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 從檔案載入
        /// </summary>
        public static ComponentJsonPreset LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"❌ File not found: {filePath}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                return FromJson(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Load failed: {ex.Message}");
                return null;
            }
        }

        // ========== 查詢方法 ==========

        public string[] GetMemberNames()
        {
            var names = new string[values.Count];
            values.Keys.CopyTo(names, 0);
            return names;
        }

        public T GetValue<T>(string memberName)
        {
            if (values.TryGetValue(memberName, out var value))
                return (T)value;
            return default(T);
        }

        public bool Contains(string memberName)
        {
            return values.ContainsKey(memberName);
        }

        // ========== 內部實作 ==========

        private ComponentJsonPreset() { }  // 私有建構子（供 FromJson 用）

        private void CopyAllMembers(Component source)
        {
            var type = source.GetType();

            // 複製 public 屬性
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                if (prop.GetIndexParameters().Length > 0) continue;

                try
                {
                    values[prop.Name] = prop.GetValue(source);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"⚠️ Cannot copy property {prop.Name}: {ex.Message}");
                }
            }

            // 複製 public 欄位
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                try
                {
                    values[field.Name] = field.GetValue(source);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"⚠️ Cannot copy field {field.Name}: {ex.Message}");
                }
            }
        }

        private void CopySpecificMembers(Component source, string[] memberNames)
        {
            var type = source.GetType();

            foreach (var memberName in memberNames)
            {
                var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanRead)
                {
                    try
                    {
                        values[memberName] = prop.GetValue(source);
                        continue;
                    }
                    catch { }
                }

                var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    try
                    {
                        values[memberName] = field.GetValue(source);
                        continue;
                    }
                    catch { }
                }

                Debug.LogWarning($"⚠️ Member not found: {memberName}");
            }
        }

        private void SetMemberValue(Component target, string memberName, object value)
        {
            var type = target.GetType();

            var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(target, value);
                return;
            }

            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }
        }

        // ========== 序列化輔助 ==========

        private string SerializeValue(object value)
        {
            if (value == null) return null;

            var type = value.GetType();

            if (type.IsPrimitive || type == typeof(string))
                return value.ToString();

            if (type.IsEnum)
                return ((int)value).ToString();

            if (type == typeof(Vector2) || type == typeof(Vector3) ||
                type == typeof(Vector4) || type == typeof(Color) ||
                type == typeof(Quaternion) || type == typeof(Rect))
            {
                return JsonUtility.ToJson(value);
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                var obj = value as UnityEngine.Object;
                return obj != null ? obj.GetInstanceID().ToString() : null;
            }

            if (type.IsSerializable)
                return JsonUtility.ToJson(value);

            return null;
        }

        private static object DeserializeValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value) || targetType == null)
                return GetDefaultValue(targetType);

            if (targetType == typeof(int)) return int.Parse(value);
            if (targetType == typeof(float)) return float.Parse(value);
            if (targetType == typeof(bool)) return bool.Parse(value);
            if (targetType == typeof(string)) return value;
            if (targetType == typeof(double)) return double.Parse(value);

            if (targetType.IsEnum)
                return Enum.ToObject(targetType, int.Parse(value));

            if (targetType == typeof(Vector2)) return JsonUtility.FromJson<Vector2>(value);
            if (targetType == typeof(Vector3)) return JsonUtility.FromJson<Vector3>(value);
            if (targetType == typeof(Vector4)) return JsonUtility.FromJson<Vector4>(value);
            if (targetType == typeof(Color)) return JsonUtility.FromJson<Color>(value);
            if (targetType == typeof(Quaternion)) return JsonUtility.FromJson<Quaternion>(value);
            if (targetType == typeof(Rect)) return JsonUtility.FromJson<Rect>(value);

            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
            {
                int instanceID = int.Parse(value);
                return UnityEngine.Object.FindFirstObjectByType(targetType);
            }

            if (targetType.IsClass)
                return JsonUtility.FromJson(value, targetType);

            return GetDefaultValue(targetType);
        }

        private static object GetDefaultValue(Type type)
        {
            return type != null && type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        // ========== 資料結構 ==========

        [Serializable]
        private class SerializedData
        {
            public string componentType;
            public string timestamp;
            public List<FieldData> fields = new List<FieldData>();
        }

        [Serializable]
        private class FieldData
        {
            public string name;
            public string typeName;
            public string value;
        }
    }

    /// <summary>
    /// Extension Methods
    /// </summary>
    public static class ComponentPresetExtensions
    {
        public static ComponentJsonPreset SavePreset(this Component component, params string[] memberNames)
        {
            return ComponentJsonPreset.CopyFrom(component, memberNames);
        }

        public static bool LoadPreset(this Component component, ComponentJsonPreset preset)
        {
            return preset?.RestoreTo(component) ?? false;
        }
    }
}