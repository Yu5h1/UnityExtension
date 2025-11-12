using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace Yu5h1Lib
{
    [Serializable]
    public abstract class PlayerPrefValue
    {
        public string key;

        private static IPlayerPrefsSerializer _serializer;
        public static IPlayerPrefsSerializer Serializer
        {
            get => _serializer ?? (_serializer = new PlayerPrefsSerializer());
            set => _serializer = value;
        }
    }
    [Serializable]
    public class PlayerPrefValue<T> : PlayerPrefValue
    {
        public T defaultValue;

        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        public bool TryGetValue(out T result)
        {
            result = defaultValue;
            if (!PlayerPrefs.HasKey(key))
                return false;
            result = Value;
            return true;
        }

        public string GetStringPref() => PlayerPrefs.GetString(key, "");

        protected virtual T GetValue()
        {
            if (string.IsNullOrEmpty(key) || !PlayerPrefs.HasKey(key))
                return defaultValue;

            var type = typeof(T);

            // 基本類型直接用 PlayerPrefs 原生方法
            if (type == typeof(int))
                return (T)(object)PlayerPrefs.GetInt(key, (int)(object)defaultValue);
            if (type == typeof(float))
                return (T)(object)PlayerPrefs.GetFloat(key, (float)(object)defaultValue);
            if (type == typeof(string))
                return (T)(object)PlayerPrefs.GetString(key, (string)(object)defaultValue);
            if (type == typeof(bool))
                return (T)(object)(PlayerPrefs.GetInt(key, (bool)(object)defaultValue ? 1 : 0) != 0);

            // 複雜類型用序列化器
            if (Serializer.CanHandle(type))
            {
                string data = PlayerPrefs.GetString(key, "");
                return Serializer.Deserialize(data, defaultValue);
            }

            throw new NotSupportedException($"Type {type} is not supported. Please register a custom serializer.");
        }

        protected virtual void SetValue(T value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            var type = typeof(T);

            // 基本類型直接用 PlayerPrefs 原生方法
            if (type == typeof(int))
            {
                PlayerPrefs.SetInt(key, (int)(object)value);
                return;
            }
            if (type == typeof(float))
            {
                PlayerPrefs.SetFloat(key, (float)(object)value);
                return;
            }
            if (type == typeof(string))
            {
                PlayerPrefs.SetString(key, (string)(object)value);
                return;
            }
            if (type == typeof(bool))
            {
                PlayerPrefs.SetInt(key, (bool)(object)value ? 1 : 0);
                return;
            }

            // 複雜類型用序列化器
            if (Serializer.CanHandle(type))
            {
                string data = Serializer.Serialize(value);
                PlayerPrefs.SetString(key, data);
                return;
            }

            throw new NotSupportedException($"Type {type} is not supported. Please register a custom serializer.");
        } 

        public void DeleteKey()
        {
            if (string.IsNullOrEmpty(key))
                return;
            if (PlayerPrefs.HasKey(key))
                PlayerPrefs.DeleteKey(key);
        }
    }

    public interface IPlayerPrefsSerializer
    {
        string Serialize<T>(T value);
        T Deserialize<T>(string data, T defaultValue);
        bool CanHandle(Type type);
    }
    public class PlayerPrefsSerializer : IPlayerPrefsSerializer
    {
        public virtual bool CanHandle(Type type)
        {
            // 基本類型
            if (type == typeof(int) ||
                type == typeof(float) ||
                type == typeof(string) ||
                type == typeof(bool))
                return true;

            // 可序列化的複雜類型
            return type.IsSerializable ||
                   type.GetCustomAttributes(typeof(SerializableAttribute), false).Length > 0;
        }

        public virtual string Serialize<T>(T value)
        {
            if (value == null)
                return string.Empty;

            var type = typeof(T);

            // 基本類型直接轉字串
            if (type == typeof(int) || type == typeof(float) ||
                type == typeof(string) || type == typeof(bool))
            {
                return value.ToString();
            }

            var serializer = new XmlSerializer(typeof(T));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, value);
                return writer.ToString();
            }
        }

        public virtual T Deserialize<T>(string data, T defaultValue)
        {
            if (string.IsNullOrEmpty(data))
                return defaultValue;

            var type = typeof(T);

            try
            {
                // 基本類型解析
                if (type == typeof(int))
                    return (T)(object)int.Parse(data);
                if (type == typeof(float))
                    return (T)(object)float.Parse(data);
                if (type == typeof(string))
                    return (T)(object)data;
                if (type == typeof(bool))
                    return (T)(object)bool.Parse(data);

                // 複雜類型用 XML 反序列化
                var serializer = new XmlSerializer(typeof(T));
                using (var reader = new StringReader(data))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
