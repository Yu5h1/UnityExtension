using System;
using UnityEngine;

namespace Yu5h1Lib
{
    public class PlayerPrefJsonSerializer :  IPlayerPrefsSerializer
    {
        public bool CanHandle(Type type)
        {
            // 處理所有可序列化的類型
            return type.IsValueType || 
                   type.IsSerializable ||
                   type.GetCustomAttributes(typeof(SerializableAttribute), false).Length > 0 ;
        }

        public string Serialize<T>(T value)
        {
            return JsonUtility.ToJson(value);
        }

        public T Deserialize<T>(string data, T defaultValue)
        {
            if (string.IsNullOrEmpty(data))
                return defaultValue;
            try
            {
                return JsonUtility.FromJson<T>(data);
            }
            catch
            {
                return defaultValue;
            }
        }
    } 
}
